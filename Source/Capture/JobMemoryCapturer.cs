using Ustas.RimAI.Communication.Memory;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Ustas.RimAI.Communication.Memory.Capture
{

    public class JobMemoryCapturer
    {
        private static readonly HashSet<JobDef> _jobsToIgnore = new();
        private static readonly Dictionary<JobDef, string> _dictAggregatedJobToDesc = new();
        private static readonly Dictionary<JobDef, float> _dictJobToImportance = new();

        private const int SessionTimeoutTicks = 2 * GenDate.TicksPerHour;
        private const int SessionMaxDurationTicks = 12 * GenDate.TicksPerHour;

        static JobMemoryCapturer()
        {
            _jobsToIgnore.UnionWith([
                JobDefOf.Goto, JobDefOf.GotoWander,
                JobDefOf.Wait, JobDefOf.Wait_Wander, JobDefOf.Wait_Combat, JobDefOf.Wait_MaintainPosture,
                JobDefOf.Wait_Asleep, JobDefOf.Wait_AsleepDormancy, JobDefOf.Wait_WithSleeping,
                JobDefOf.LayDown, JobDefOf.LayDownAwake, JobDefOf.LayDownResting
                ]);
            _jobsToIgnore.Remove(null);

            static void AddToDict(JobDef jobDef, float importance)
            {
                if (jobDef is null) return;
                _dictJobToImportance[jobDef] = importance;
            }

            AddToDict(JobDefOf.AttackMelee, 0.9f);
            AddToDict(JobDefOf.AttackStatic, 0.9f);
            AddToDict(JobDefOf.SocialFight, 0.85f);

            AddToDict(JobDefOf.MarryAdjacentPawn, 1.0f);
            AddToDict(JobDefOf.SpectateCeremony, 0.7f);

            AddToDict(JobDefOf.Lovin, 0.6f);

            foreach (var jobDef in DefDatabase<JobDef>.AllDefsListForReading)
            {
                if (jobDef is null) continue;

                string defName = jobDef.defName;
                string desc = string.Empty;

                desc = defName switch
                {
                    _ when defName.Contains("Haul") => "переносити",
                    _ when defName.Contains("Harvest") => "збирати врожай",
                    _ when defName.Contains("CutPlant") => "зрізати",
                    _ when defName.Contains("Mine") => "видобуток",
                    _ when defName.Contains("Repair") => "ремонт",
                    _ when defName.Contains("Milk") => "доїти",
                    _ when defName.Contains("Shear") => "стригти",

                    _ when defName.Contains("Deconstruct")
                    || defName.Contains("RemoveFloor")
                    || defName.Contains("RemoveRoof")
                    || defName.Contains("Uninstall")
                    => "розібрати",

                    _ when defName.Contains("Frame")
                    || defName.Contains("BuildRoof")
                    || defName.Contains("Smooth")
                    => "будувати",

                    _ when defName.Contains("Sow")
                    || defName.Contains("Replant")
                    || defName.Contains("PlantSeed")
                    => "садити",

                    _ when defName.Contains("Clean")
                    || defName.Contains("Clear")
                    => "прибирання",

                    _ => desc
                };

                if (string.IsNullOrEmpty(desc)) continue;

                _dictAggregatedJobToDesc[jobDef] = desc;
            }
        }

        public static void ExtractJobInfoEnter(Job job, Pawn pawn)
        {
            if (
                job?.def is not { } jobDef
                || _jobsToIgnore.Contains(jobDef)

                || pawn is null
                || !pawn.IsColonist
                || !RimTalkMemoryPatchMod.Settings.enableActionMemory)

                return;

            pawn.GetComp<FourLayerMemoryComp>()?.JobCapturer?.ExtractJobInfo(job);
        }

        public static void BuildJobMemoryEnter(Job job, Pawn pawn)
        {
            if (
                job?.def is not { } jobDef
                || _jobsToIgnore.Contains(jobDef)

                || Find.TickManager?.TicksGame is not { } currentTick 
                || currentTick == job.startTick

                || pawn is null
                || !pawn.IsColonist
                || !RimTalkMemoryPatchMod.Settings.enableActionMemory)

                return;

            pawn.GetComp<FourLayerMemoryComp>()?.JobCapturer?.BuildJobMemory(job);
        }


        private readonly FourLayerMemoryComp _memoryComp;

        private ThingWithComps Parent => _memoryComp.parent;

        private int _startGameTick;
        private int _lastActiveTick;
        private int _repeatCount;
        private HashSet<string> _targetNames;
        private HashSet<string> TargetNames
        {
            get
            {
                _targetNames ??= new();
                return _targetNames;
            }
        }

        private MemoryEntry _lastJobMemory;
        private string _lastJobReport;
        private string _lastJobAggregateDesc;

        private System.WeakReference<Job> _curJobWeakRef;
        private Job CurJob
        {
            get
            {
                _curJobWeakRef ??= new(null);
                _curJobWeakRef.TryGetTarget(out var job);
                return job;
            }
            set
            {
                _curJobWeakRef ??= new(null);
                _curJobWeakRef.SetTarget(value);
            }
        }
        private string _curJobReport;
        private string _curJobTargetAName;

        public JobMemoryCapturer(FourLayerMemoryComp memoryComp)
        {
            _memoryComp = memoryComp;
        }

        private void ExtractJobInfo(Job job)
        {
            CurJob = job;
            _curJobReport = GetJobReport(job);
            _curJobTargetAName = _dictAggregatedJobToDesc.ContainsKey(job.def) ? GetTargetAName(job) : null;
        }

        private void BuildJobMemory(Job job)
        {
            bool? sharedConditionCache = null;
            bool sharedCondition() => sharedConditionCache ??=
                Find.TickManager.TicksGame - _lastActiveTick <= SessionTimeoutTicks
                && Find.TickManager.TicksGame - _startGameTick <= SessionMaxDurationTicks
                && _lastJobMemory is not null;

            string report = null;
            string getReport() => report ??= GetJobReportCached(job);

            // DRY
            void updateMemoryBase()
            {
                _lastJobMemory.GameTick = Find.TickManager.TicksGame;
                _lastJobMemory.Importance += ImportanceIncrement();
            }

            if (TargetNames.Count <= 1 && getReport() == _lastJobReport && sharedCondition())
            {
                updateMemoryBase();
                UpdateSession();
                _lastJobMemory.Content = BuildExactContent();

                return;
            }

            var jobDef = job.def;
            var ableToAggregate = _dictAggregatedJobToDesc.TryGetValue(jobDef, out string jobAggregateDesc);

            if (ableToAggregate
                && jobAggregateDesc == _lastJobAggregateDesc
                && sharedCondition())
            {
                updateMemoryBase();
                UpdateSession(GetTargetANameCached(job));
                _lastJobMemory.Content = BuildFuzzyContent();

                return;
            }


            string newReport = getReport();

            var newMemory = new MemoryEntry(
                newReport,
                MemoryType.Action,
                MemoryLayer.Active,
                importance: _dictJobToImportance.TryGetValue(jobDef, out float value) ? value : 0.5f
                );

            _memoryComp.ActiveMemories.Add(newMemory);

            StartNewSession(
                newMemory,
                newReport,
                jobAggregateDesc,
                ableToAggregate ? GetTargetANameCached(job) : null
                );
        }

        private void StartNewSession(MemoryEntry newMemory, string report, string jobAggregateDesc, string targetName = null)
        {
            _startGameTick = _lastActiveTick = Find.TickManager.TicksGame;
            _repeatCount = 1;
            TargetNames.Clear();

            if (!string.IsNullOrEmpty(targetName))
                TargetNames.Add(targetName);

            _lastJobMemory = newMemory;
            _lastJobReport = report;
            _lastJobAggregateDesc = jobAggregateDesc;

        }

        private void UpdateSession(string targetName = null)
        {
            _lastActiveTick = Find.TickManager.TicksGame;
            _repeatCount++;

            if (!string.IsNullOrEmpty(targetName))
                TargetNames.Add(targetName);
        }

        private string GetJobReportCached(Job job)
        {
            if (CurJob == job && !string.IsNullOrEmpty(_curJobReport))
                return _curJobReport;

            return GetJobReport(job);
        }

        private static bool NamesUsedUpCorpse(Job job)
        {
            return IsUsedUpCorpse(job.targetA) || IsUsedUpCorpse(job.targetB) || IsUsedUpCorpse(job.targetC)
                || (job.targetQueueA != null && job.targetQueueA.Any(IsUsedUpCorpse))
                || (job.targetQueueB != null && job.targetQueueB.Any(IsUsedUpCorpse));
        }

        private static bool IsUsedUpCorpse(LocalTargetInfo target)
        {
            return target.Thing is Corpse corpse && corpse.Bugged;
        }

        private string GetJobReport(Job job)
        {
            if (Parent is not Pawn parentPawn) return string.Empty;

            // Only while the game is actually being played. A job report is the
            // driver's own sentence about what it is doing, and a driver is free to
            // read anything it likes to build it - JobDriver_HaulToCell reads the
            // destination cell's slot group, for one. During a load none of that is
            // built yet, and asking took a whole save down: passing a faction leader
            // to the world inside DoAllPostLoadInits cleared its mind, which stopped
            // its job, which brought this prefix here, which dereferenced null in
            // StoreUtility.GetSlotGroup and ended the map load.
            //
            // Nothing is lost by declining: a job being torn down by the loader is
            // not a thing the pawn did, and a memory of it would be a memory of
            // bookkeeping.
            if (Current.ProgramState != ProgramState.Playing) return string.Empty;

            // A job about a corpse that has already been used up - butchered, say, as
            // the job ends - cannot be described: the report asks the corpse for its
            // label and the game logs "LabelNoCount on Corpse while Bugged." for it.
            if (NamesUsedUpCorpse(job)) return string.Empty;

            var jobReport = job.GetReport(parentPawn);

            if (string.IsNullOrEmpty(jobReport)) return string.Empty;

            if (jobReport.StartsWith("зараз"))
            {
                jobReport = jobReport.Substring(2);
            }

            return jobReport;
        }

        // Non-obvious edge case — read carefully before changing. (job targetA job targetB)
        private string GetTargetANameCached(Job job)
        {
            if (CurJob == job && !string.IsNullOrEmpty(_curJobTargetAName))
                return _curJobTargetAName;

            return GetTargetAName(job);
        }

        private string GetTargetAName(Job job)
        {
            if (!job.targetA.HasThing) return string.Empty;

            var targetThing = job.targetA.Thing;

            if (targetThing == Parent) return "сам";

            if (targetThing is Blueprint or Frame)
            {
                return targetThing.def?.entityDefToBuild?.label ?? string.Empty;
            }

            return targetThing.LabelShort ?? targetThing.def?.label ?? string.Empty;
        }

        private string BuildExactContent() => $"{GetDurationDesc()}разів: {_repeatCount}, {_lastJobReport}";
        private string BuildFuzzyContent() =>
            $"{GetDurationDesc()}{_lastJobAggregateDesc} разів: {_repeatCount}{string.Join("、", TargetNames.Take(3))}{(TargetNames.Count > 3 ? "等" : "")}。";

        private string GetDurationDesc()
        {
            return (Find.TickManager.TicksGame - _startGameTick) switch
            {
                <= GenDate.TicksPerHour => "поспіль",
                <= 2 * GenDate.TicksPerHour => "за дві години",
                <= 4 * GenDate.TicksPerHour => "кілька годин",
                <= 8 * GenDate.TicksPerHour => "пів дня або менше",
                <= 12 * GenDate.TicksPerHour => "більшу частину дня",
                _ => "цілий день"
            };
        }

        private float ImportanceIncrement()
        {
            float increment = (Find.TickManager.TicksGame - _lastActiveTick) * (0.02f / GenDate.TicksPerHour);

            if (_repeatCount <= 20) increment += 0.01f;

            return increment;
        }
    }

}