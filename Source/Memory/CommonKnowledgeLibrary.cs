using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.Policy;
using Ustas.RimAI.Communication.Memory.Diagnostics;

namespace Ustas.RimAI.Communication.Memory
{
    public class CommonKnowledgeLibrary : IExposable
    {
        private List<CommonKnowledgeEntry> entries = new List<CommonKnowledgeEntry>();
        
        // Serialization / save-load constraint — keep field identity stable. (Scribe)
        private List<string> vectorIds;
        private List<string> vectorDataSerialized;  // Serialization / save-load constraint — keep field identity stable.
        private List<string> vectorHashes;

        public List<CommonKnowledgeEntry> Entries => entries;

        public void ExposeData()
        {
            Scribe_Collections.Look(ref entries, "commonKnowledge", LookMode.Deep);

            // Serialization / save-load constraint — keep field identity stable.
            ExtendedKnowledgeEntry.ExposeData();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                if (RimTalkMemoryPatchMod.Settings.enableVectorEnhancement)
                {
                    try
                    {
                        List<List<float>> vectorData;
                        VectorDB.VectorService.Instance.ExportVectorsForSave(
                            out vectorIds, out vectorData, out vectorHashes);
                        
                        vectorDataSerialized = new List<string>();
                        if (vectorData != null)
                        {
                            foreach (var vector in vectorData)
                            {
                                if (vector != null)
                                {
                                    vectorDataSerialized.Add(string.Join(",", vector));
                                }
                                else
                                {
                                    vectorDataSerialized.Add("");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[RimAI.Memory] Failed to export vectors for save: {ex}");
                        vectorIds = null;
                        vectorDataSerialized = null;
                        vectorHashes = null;
                    }
                }
            }
            
            // Serialization / save-load constraint — keep field identity stable.
            Scribe_Collections.Look(ref vectorIds, "vectorIds", LookMode.Value);
            Scribe_Collections.Look(ref vectorDataSerialized, "vectorDataSerialized", LookMode.Value);
            Scribe_Collections.Look(ref vectorHashes, "vectorHashes", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (entries == null) entries = new List<CommonKnowledgeEntry>();
                
                if (RimTalkMemoryPatchMod.Settings.enableVectorEnhancement)
                {
                    try
                    {
                        if (vectorIds != null && vectorDataSerialized != null && vectorHashes != null && vectorIds.Count > 0)
                        {
                            ModuleLog.Message($"[RimAI.Memory] Restoring {vectorIds.Count} vectors from save...");
                            
                            var vectorData = new List<List<float>>();
                            foreach (var serialized in vectorDataSerialized)
                            {
                                if (!string.IsNullOrEmpty(serialized))
                                {
                                    var floats = new List<float>();
                                    foreach (var str in serialized.Split(','))
                                    {
                                        if (float.TryParse(str, out float value))
                                        {
                                            floats.Add(value);
                                        }
                                    }
                                    vectorData.Add(floats);
                                }
                                else
                                {
                                    vectorData.Add(new List<float>());
                                }
                            }
                            
                            VectorDB.VectorService.Instance.ImportVectorsFromLoad(
                                vectorIds, vectorData, vectorHashes);
                        }
                        else
                        {
                            ModuleLog.Message("[RimAI.Memory] No saved vectors found, will perform full sync.");
                        }
                        
                        ModuleLog.Message("[RimAI.Memory] Syncing knowledge library to vector database...");
                        VectorDB.VectorService.Instance.SyncKnowledgeLibrary(this);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[RimAI.Memory] Failed to restore/sync vectors on game load: {ex}");
                    }
                }
            }
        }

        public void AddEntry(CommonKnowledgeEntry entry)
        {
            if (entry != null && !entries.Contains(entry))
            {
                entries.Add(entry);
                
                if (RimTalkMemoryPatchMod.Settings.enableVectorEnhancement)
                {
                    try
                    {
                        if (entry.isEnabled)
                        {
                            VectorDB.VectorService.Instance.UpdateKnowledgeVector(entry.id, entry.content);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[RimAI.Memory] Failed to sync vector on AddEntry: {ex.Message}");
                    }
                }
            }
        }
        
        public void AddEntry(string tag, string content)
        {
            var entry = new CommonKnowledgeEntry(tag, content);
            AddEntry(entry);
        }

        public void RemoveEntry(CommonKnowledgeEntry entry)
        {
            if (entry != null)
            {
                entries.Remove(entry);
                
                if (RimTalkMemoryPatchMod.Settings.enableVectorEnhancement)
                {
                    try
                    {
                        VectorDB.VectorService.Instance.RemoveKnowledgeVector(entry.id);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[RimAI.Memory] Failed to remove vector on RemoveEntry: {ex.Message}");
                    }
                }
                
                ExtendedKnowledgeEntry.CleanupDeletedEntries(this);
            }
        }

        public void Clear()
        {
            entries.Clear();
            
            if (RimTalkMemoryPatchMod.Settings.enableVectorEnhancement)
            {
                try
                {
                    VectorDB.VectorService.Instance.SyncKnowledgeLibrary(this);
                }
                catch (Exception ex)
                {
                    Log.Warning($"[RimAI.Memory] Failed to clear vectors on Clear: {ex.Message}");
                }
            }
            
            ExtendedKnowledgeEntry.CleanupDeletedEntries(this);
        }

        public int ImportFromText(string text, bool clearExisting = false)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            if (clearExisting)
            {
                entries.Clear();
            }

            int importCount = 0;
            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;

                var entry = ParseLine(trimmedLine);
                if (entry != null)
                {
                    entries.Add(entry);
                    importCount++;
                }
            }
            
            if (RimTalkMemoryPatchMod.Settings.enableVectorEnhancement)
            {
                try
                {
                    VectorDB.VectorService.Instance.SyncKnowledgeLibrary(this);
                }
                catch (Exception ex)
                {
                    Log.Warning($"[RimAI.Memory] Failed to sync vectors on ImportFromText: {ex.Message}");
                }
            }

            return importCount;
        }

        private CommonKnowledgeEntry ParseLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return null;

            int tagStart = line.IndexOf('[');
            int tagEnd = -1;
            
            if (tagStart >= 0)
            {
                tagEnd = line.IndexOf(']', tagStart + 1);
                
                if (tagEnd == -1)
                {
                    int braceEnd = line.IndexOf('}', tagStart + 1);
                    if (braceEnd > tagStart)
                    {
                        tagEnd = braceEnd;
                        Log.Warning($"[CommonKnowledge] Виявлено неправильний формат тегу (використано фігурні дужки): {line.Substring(0, Math.Min(50, line.Length))}");
                    }
                }
            }

            if (tagStart == -1 || tagEnd == -1 || tagEnd <= tagStart)
            {
                return new CommonKnowledgeEntry("загальне", line) { importance = 0.5f };
            }

            string tagPart = line.Substring(tagStart + 1, tagEnd - tagStart - 1).Trim();
            string content = line.Substring(tagEnd + 1).Trim();

            if (string.IsNullOrEmpty(content))
                return null;

            string[] parts = tagPart.Split('|');
            
            string tag = parts.Length > 0 ? parts[0].Trim() : "загальне";
            float importance = 0.5f;
            KeywordMatchMode matchMode = KeywordMatchMode.Any;
            bool canBeExtracted = false;
            bool canBeMatched = false;

            if (parts.Length > 1)
            {
                string importanceStr = parts[1].Trim();
                if (!float.TryParse(importanceStr, out importance))
                {
                    importance = 0.5f;
                    Log.Warning($"[CommonKnowledge] Failed to parse importance '{importanceStr}' in line: {line.Substring(0, Math.Min(50, line.Length))}");
                }
                importance = Math.Max(0f, Math.Min(1f, importance));
            }

            if (parts.Length > 2)
            {
                string matchModeStr = parts[2].Trim();
                if (!Enum.TryParse(matchModeStr, true, out matchMode))
                {
                    matchMode = KeywordMatchMode.Any;
                    Log.Warning($"[CommonKnowledge] Failed to parse matchMode '{matchModeStr}', using default 'Any'");
                }
            }

            if (parts.Length > 3)
            {
                string canBeExtractedStr = parts[3].Trim();
                if (!bool.TryParse(canBeExtractedStr, out canBeExtracted))
                {
                    canBeExtracted = false;
                }
            }

            if (parts.Length > 4)
            {
                string canBeMatchedStr = parts[4].Trim();
                if (!bool.TryParse(canBeMatchedStr, out canBeMatched))
                {
                    canBeMatched = false;
                }
            }

            var entry = new CommonKnowledgeEntry(tag, content) 
            { 
                importance = importance,
                matchMode = matchMode
            };

            ExtendedKnowledgeEntry.SetCanBeExtracted(entry, canBeExtracted);
            ExtendedKnowledgeEntry.SetCanBeMatched(entry, canBeMatched);

            return entry;
        }

        public string ExportToText()
        {
            var sb = new StringBuilder();

            foreach (var entry in entries)
            {
                if (entry != null)
                {
                    sb.AppendLine(entry.FormatForExport());
                }
            }

            return sb.ToString();
        }

        public string InjectKnowledge(string context, int maxEntries = 5)
        {
            return InjectKnowledgeWithDetails(context, maxEntries, out _);
        }

        public string InjectKnowledgeWithDetails(string context, int maxEntries, out List<KnowledgeScore> scores, Verse.Pawn currentPawn = null, Verse.Pawn targetPawn = null)
        {
            return InjectKnowledgeWithDetails(context, maxEntries, out scores, out _, out _, currentPawn, targetPawn);
        }
        
        public string InjectKnowledgeWithDetails(string context, int maxEntries, out List<KnowledgeScore> scores, out KeywordExtractionInfo keywordInfo, Verse.Pawn currentPawn = null, Verse.Pawn targetPawn = null)
        {
            return InjectKnowledgeWithDetails(context, maxEntries, out scores, out _, out keywordInfo, currentPawn, targetPawn);
        }
        
        public string InjectKnowledgeWithDetails(string context, int maxEntries, out List<KnowledgeScore> scores, out List<KnowledgeScoreDetail> allScores, out KeywordExtractionInfo keywordInfo, Verse.Pawn currentPawn = null, Verse.Pawn targetPawn = null)
        {
            scores = new List<KnowledgeScore>();
            allScores = new List<KnowledgeScoreDetail>();
            keywordInfo = new KeywordExtractionInfo();

            var settings = RimTalkMemoryPatchMod.Settings;
            
            StringBuilder matchTextBuilder = new StringBuilder();
            matchTextBuilder.Append(context);
            
            if (currentPawn != null)
            {
                matchTextBuilder.Append(" ");
                matchTextBuilder.Append(BuildCompletePawnInfoText(currentPawn));
                
                var tempKeywords = new List<string>();
                var pawnInfo = KeywordExtractionHelper.ExtractPawnKeywords(tempKeywords, currentPawn);
                keywordInfo.PawnInfo = pawnInfo;
                keywordInfo.PawnKeywordsCount = pawnInfo.TotalCount;
            }
            
            if (targetPawn != null && targetPawn != currentPawn)
            {
                matchTextBuilder.Append(" ");
                matchTextBuilder.Append(BuildCompletePawnInfoText(targetPawn));
            }
            
            string originalMatchText = matchTextBuilder.ToString();
            string currentMatchText = originalMatchText;
            
            keywordInfo.ContextKeywords = new List<string> { context };
            keywordInfo.TotalKeywords = 1;

            var allMatchedEntries = new HashSet<CommonKnowledgeEntry>();
            
            int maxRounds = settings.enableKnowledgeChaining ? settings.maxChainingRounds : 1;
            
            for (int round = 0; round < maxRounds; round++)
            {
                if (string.IsNullOrEmpty(currentMatchText))
                    break;

                bool isChaining = round > 0;
                string matchText = (round == 0) ? originalMatchText : currentMatchText;
                var roundMatches = MatchKnowledgeByTags(matchText, currentPawn, allMatchedEntries, isChaining);
                
                if (roundMatches.Count == 0)
                    break;

                foreach (var match in roundMatches)
                {
                    allMatchedEntries.Add(match);
                }

                if (!settings.enableKnowledgeChaining || round >= maxRounds - 1)
                    break;

                currentMatchText = BuildMatchTextFromKnowledge(roundMatches);
            }
            
            var scoredEntries = new List<KnowledgeScore>();
            
            foreach (var entry in allMatchedEntries)
            {
                KnowledgeTagMatchMode mode = entry.matchMode == KeywordMatchMode.All
                    ? KnowledgeTagMatchMode.All
                    : KnowledgeTagMatchMode.Any;
                KnowledgeMatchKind kind = KnowledgeMatchPolicy.Classify(context, entry.content, entry.GetTags(), mode);
                KnowledgeMatchType matchType = kind == KnowledgeMatchKind.Vector
                    ? KnowledgeMatchType.Vector
                    : kind == KnowledgeMatchKind.Mixed
                        ? KnowledgeMatchType.Mixed
                        : KnowledgeMatchType.Keyword;

                float matchTypeScore = kind == KnowledgeMatchKind.Keyword || kind == KnowledgeMatchKind.Mixed ? 0.5f : 0f;
                float vectorScore = KnowledgeMatchPolicy.VectorScore(context, entry.content, entry.GetTags());
                float finalScore = matchTypeScore + (vectorScore * 0.4f) + entry.importance;
                
                allScores.Add(new KnowledgeScoreDetail
                {
                    Entry = entry,
                    IsEnabled = entry.isEnabled,
                    TotalScore = finalScore,
                    BaseScore = entry.importance,
                    ManualBonus = 0f,
                    MatchTypeScore = matchTypeScore,
                    MatchType = matchType,
                    MatchedTags = entry.GetTags(),
                    FailReason = "Pending"
                });
                
                scoredEntries.Add(new KnowledgeScore
                {
                    Entry = entry,
                    Score = finalScore
                });
            }


            scoredEntries.Sort((a, b) => b.Score.CompareTo(a.Score));
            
            for (int i = 0; i < scoredEntries.Count; i++)
            {
                var detail = allScores.FirstOrDefault(d => d.Entry == scoredEntries[i].Entry);
                if (detail != null)
                {
                    if (i < maxEntries)
                    {
                        detail.FailReason = "Selected";
                        scores.Add(scoredEntries[i]);
                    }
                    else
                    {
                        detail.FailReason = "ExceedMaxEntries";
                    }
                }
            }
            
            var sortedEntries = scores.Select(s => s.Entry).ToList();

            if (sortedEntries.Count == 0)
                return null;

            var sb = new StringBuilder();
            int index = 1;
            foreach (var entry in sortedEntries)
            {
                sb.AppendLine($"{index}. [{entry.tag}] {entry.content}");
                index++;
            }

            return sb.ToString();
        }

        private List<CommonKnowledgeEntry> MatchKnowledgeByTags(
            string matchText,
            Verse.Pawn currentPawn,
            HashSet<CommonKnowledgeEntry> alreadyMatched,
            bool isChaining = false)
        {
            var matches = new List<CommonKnowledgeEntry>();

            if (string.IsNullOrEmpty(matchText))
                return matches;

            foreach (var entry in entries)
            {
                if (alreadyMatched.Contains(entry))
                    continue;

                if (!entry.isEnabled)
                    continue;

                if (isChaining && !ExtendedKnowledgeEntry.CanBeMatched(entry)) continue;

                if (entry.targetPawnId != -1 && (currentPawn == null || entry.targetPawnId != currentPawn.thingIDNumber))
                    continue;

                if (IsMatched(matchText, entry))
                {
                    matches.Add(entry);
                }
            }

            return matches;
        }

        private bool IsMatched(string text, CommonKnowledgeEntry entry)
        {
            var tags = entry.GetTags();
            KnowledgeTagMatchMode mode = entry.matchMode == KeywordMatchMode.All
                ? KnowledgeTagMatchMode.All
                : KnowledgeTagMatchMode.Any;
            return KnowledgeMatchPolicy.Matches(text, entry.content, tags, mode);
        }

        private string BuildMatchTextFromKnowledge(List<CommonKnowledgeEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();

            foreach (var entry in entries)
            {
                if (!ExtendedKnowledgeEntry.CanBeExtracted(entry)) continue;

                if (!string.IsNullOrEmpty(entry.content))
                {
                    if (sb.Length > 0)
                        sb.Append(" ");
                    sb.Append(entry.content);
                }
            }

            return sb.ToString();
        }

        private string BuildCompletePawnInfoText(Verse.Pawn pawn)
        {
            if (pawn == null)
                return string.Empty;

            var sb = new StringBuilder();

            try
            {
                if (!string.IsNullOrEmpty(pawn.Name?.ToStringShort))
                {
                    sb.Append(pawn.Name.ToStringShort);
                    sb.Append(" ");
                }

                if (pawn.RaceProps != null && pawn.RaceProps.Humanlike)
                {
                    float ageYears = pawn.ageTracker.AgeBiologicalYearsFloat;
                    
                    if (ageYears < 3f)
                    {
                        sb.Append("немовля малюк ");
                    }
                    else if (ageYears < 13f)
                    {
                        sb.Append("дитина дитя ");
                    }
                    else if (ageYears < 18f)
                    {
                        sb.Append("підліток ");
                    }
                    else
                    {
                        sb.Append("дорослий ");
                    }
                }

                sb.Append(pawn.gender.GetLabel());
                sb.Append(" ");

                if (pawn.def != null)
                {
                    sb.Append(pawn.def.label);
                    sb.Append(" ");
                    
                    try
                    {
                        if (pawn.genes != null && pawn.genes.Xenotype != null)
                        {
                            string xenotypeName = pawn.genes.Xenotype.label ?? pawn.genes.Xenotype.defName;
                            if (!string.IsNullOrEmpty(xenotypeName))
                            {
                                sb.Append(xenotypeName);
                                sb.Append(" ");
                            }
                        }
                    }
                    catch { }
                }

                if (pawn.IsColonist)
                {
                    sb.Append("колоніст ");
                }
                else if (pawn.IsPrisoner)
                {
                    sb.Append("бранець ");
                }
                else if (pawn.IsSlaveOfColony)
                {
                    sb.Append("раб ");
                }
                else if (pawn.HostFaction == Faction.OfPlayer)
                {
                    sb.Append("гість ");
                }
                else if (pawn.Faction != null && pawn.Faction != Faction.OfPlayer)
                {
                    sb.Append(pawn.Faction.Name);
                    sb.Append(" ");
                }

                if (pawn.story?.traits != null)
                {
                    foreach (var trait in pawn.story.traits.allTraits)
                    {
                        if (trait?.def?.label != null)
                        {
                            sb.Append(trait.def.label);
                            sb.Append(" ");
                        }
                    }
                }

                if (pawn.skills != null)
                {
                    foreach (var skillRecord in pawn.skills.skills)
                    {
                        if (skillRecord.TotallyDisabled || skillRecord.def?.label == null)
                            continue;
                        
                        int level = skillRecord.Level;
                        
                        if (level >= 5)
                        {
                            sb.Append(skillRecord.def.label);
                            sb.Append(level);
                            sb.Append(" ");
                            
                            if (level >= 15)
                            {
                                sb.Append(skillRecord.def.label);
                                sb.Append("майстер ");
                            }
                            else if (level >= 10)
                            {
                                sb.Append(skillRecord.def.label);
                                sb.Append("вправний ");
                            }
                        }
                    }
                }

                if (pawn.health != null)
                {
                    if (pawn.health.hediffSet.GetInjuredParts().Any())
                    {
                        sb.Append("поранений ");
                    }
                    else if (!pawn.health.HasHediffsNeedingTend())
                    {
                        sb.Append("здоровий ");
                    }
                }

                if (pawn.relations != null)
                {
                    var relatedPawns = pawn.relations.RelatedPawns.Take(5);
                    foreach (var relatedPawn in relatedPawns)
                    {
                        if (!string.IsNullOrEmpty(relatedPawn.Name?.ToStringShort))
                        {
                            sb.Append(relatedPawn.Name.ToStringShort);
                            sb.Append(" ");
                        }
                    }
                }

                if (pawn.story?.Adulthood != null)
                {
                    string backstoryTitle = pawn.story.Adulthood.TitleFor(pawn.gender);
                    if (!string.IsNullOrEmpty(backstoryTitle))
                    {
                        sb.Append(backstoryTitle);
                        sb.Append(" ");
                    }
                }
                
                if (pawn.story?.Childhood != null)
                {
                    string childhoodTitle = pawn.story.Childhood.TitleFor(pawn.gender);
                    if (!string.IsNullOrEmpty(childhoodTitle))
                    {
                        sb.Append(childhoodTitle);
                        sb.Append(" ");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimAI.Memory] Error building complete pawn info text: {ex.Message}");
            }

                return sb.ToString().Trim();
            }
        }
    }
