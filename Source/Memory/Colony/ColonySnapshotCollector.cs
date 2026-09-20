using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse.AI;
using Verse;

namespace Ustas.RimAI.Communication.Memory.Colony
{
    /// <summary>
    /// Reads a map and fills in a <see cref="ColonySnapshot"/>.
    ///
    /// Everything here is a read of state the game already keeps for its own
    /// UI - the resource counter, the wealth watcher, the archive behind the
    /// letter stack - so it costs a pass over the spawned colonists and
    /// nothing else. It runs on the main thread, from the world tick, and
    /// holds no reference to anything afterwards.
    /// </summary>
    internal static class ColonySnapshotCollector
    {
        /// <summary>A colonist eats about this much in a day.</summary>
        private const float DailyNutritionPerColonist = 1.6f;

        private const int RecentEventCount = 6;

        internal static ColonySnapshot Collect(Map map)
        {
            if (map == null)
            {
                return null;
            }

            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            ColonySnapshot snapshot = new ColonySnapshot
            {
                MapLabel = map.Parent?.Label ?? "",
                Colonists = colonists.Count,
                Downed = colonists.Count(pawn => pawn.Downed),
                Prisoners = map.mapPawns.PrisonersOfColonyCount,
                Medicine = map.resourceCounter.GetCountIn(ThingRequestGroup.Medicine),
                Wealth = map.wealthWatcher?.WealthTotal ?? 0f,
                HostilePawns = map.mapPawns.AllPawnsSpawned
                    .Count(pawn => pawn.HostileTo(Faction.OfPlayer) && !pawn.Downed),
                FiresBurning = map.listerThings.ThingsOfDef(ThingDefOf.Fire).Count,
                Season = GenLocalDate.Season(map).LabelCap(),
                OutdoorTemperature = Mathf.RoundToInt(map.mapTemperature.OutdoorTemp),
                ResearchInProgress = Find.ResearchManager?.GetProject()?.LabelCap ?? "",
            };

            FillMood(snapshot, colonists);
            snapshot.DaysOfFood = DaysOfFood(map, colonists.Count);
            FillRecentEvents(snapshot);
            return snapshot;
        }

        private static void FillMood(ColonySnapshot snapshot, List<Pawn> colonists)
        {
            float total = 0f;
            int counted = 0;
            foreach (Pawn pawn in colonists)
            {
                Need_Mood mood = pawn.needs?.mood;
                if (mood == null)
                {
                    continue;
                }

                total += mood.CurLevel;
                counted++;
                MentalBreaker breaker = pawn.mindState?.mentalBreaker;
                if (breaker != null && mood.CurLevel < breaker.BreakThresholdMinor)
                {
                    snapshot.MoodBelowBreakThreshold++;
                }
            }

            snapshot.AverageMood = counted > 0 ? total / counted : 0f;
        }

        /// <summary>
        /// How long the stores last at the colony's own appetite, which is the
        /// form the number is useful in: "nine days" says something a
        /// nutrition total does not.
        /// </summary>
        private static int DaysOfFood(Map map, int colonists)
        {
            if (colonists <= 0)
            {
                return 0;
            }

            float nutrition = map.resourceCounter?.TotalHumanEdibleNutrition ?? 0f;
            return Mathf.FloorToInt(nutrition / (colonists * DailyNutritionPerColonist));
        }

        /// <summary>
        /// The last few things the game thought worth a letter. The archive is
        /// what the player's own history tab reads, so this is the colony's
        /// recent past as the game itself records it rather than a second
        /// event log of ours.
        /// </summary>
        private static void FillRecentEvents(ColonySnapshot snapshot)
        {
            List<IArchivable> archivables = Find.Archive?.ArchivablesListForReading;
            if (archivables == null)
            {
                return;
            }

            for (int index = archivables.Count - 1;
                index >= 0 && snapshot.RecentEvents.Count < RecentEventCount;
                index--)
            {
                string label = archivables[index]?.ArchivedLabel;
                if (!label.NullOrEmpty())
                {
                    snapshot.RecentEvents.Add(label);
                }
            }
        }
    }
}
