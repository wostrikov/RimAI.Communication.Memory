using Ustas.RimAI.Communication.Memory.Policy;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Ustas.RimAI.Communication.Memory
{

    public class RoundMemory : MemoryEntry, IExposable
    {
        public long RoundMemoryUniqueID = -1;
        private long AbsTick = -1;
        private PlanetTile planetTile = PlanetTile.Invalid;
        public bool IsHomeMap = false;
        public HashSet<Pawn> Pawns = new();

        public override bool CanBeSummarized => true;

        public RoundMemory() { }
        public RoundMemory(HashSet<Pawn> pawns, string content = null) : base(
            content: string.Empty,
            type: MemoryType.Conversation,
            layer: MemoryLayer.Active,
            importance: 0.5f
            )
        {
            Pawns = pawns ?? new();
            Pawns.RemoveWhere(p => p is null);

            RoundMemoryUniqueID = RoundMemoryManager.GetNewRoundMemoryId();
            AbsTick = Find.TickManager?.TicksAbs ?? -1;

            if (Pawns.Count == 0)
            {
                Log.Warning("[RoundMemory] Під час створення RoundMemory не знайдено учасників діалогу");
                return;
            }
            planetTile = Pawns.FirstOrDefault()?.Tile ?? PlanetTile.Invalid;
            IsHomeMap = Pawns.Select(p => p.Map).FirstOrDefault(m => m is not null)?.IsPlayerHome ?? false;

            Content = $"[{"RimTalk_Memory_Participants".Translate()}: {GetParticipantsRoster()}]{(content is null ? string.Empty : $"\n{content}")}";
        }

        public void AppendLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            Content += $"\n{line}";
        }


        public string GetDateAndTime()
        {
            if (!planetTile.Valid || AbsTick == -1) return "RimTalk_Memory_UnknownDate".Translate();

            var location = Find.WorldGrid?.LongLatOf(planetTile) ?? Vector2.zero;

            return $"{GenDate.DateFullStringAt(AbsTick, location)} {GetInGameHour12HString(AbsTick, location)}";
        }

        private static string GetInGameHour12HString(long absTicks, Vector2 longLat)
        {
            int hour24 = GenDate.HourOfDay(absTicks, longLat.x);
            int hour12 = hour24 % 12;
            if (hour12 == 0) hour12 = 12;
            string arg = (hour24 < 12) ? "am" : "pm";
            return $"{hour12}{arg}";
        }

        public string GetParticipantsRoster()
        {
            var names = new List<string>();
            foreach (var pawn in Pawns)
            {
                if (pawn?.LabelShort != null)
                    names.Add(pawn.LabelShort);
            }
            return MemoryRoundConversationPolicy.ComposeRoster(names);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref RoundMemoryUniqueID, "RoundMemoryUniqueID", -1);

            Scribe_Values.Look(ref AbsTick, "AbsTick", -1);

            Scribe_Values.Look(ref planetTile, "Tile", PlanetTile.Invalid);
            Scribe_Values.Look(ref IsHomeMap, "IsHomeMap", false);

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // A participant who has left the world entirely is not going to
                // be written by anyone, so a reference to them resolves to
                // nothing on the way back in. The game says so at save time and
                // again at load time; both are catalogued (0067, 0074). The
                // roster the player reads was built at construction and lives
                // in Content, so dropping the pawn here costs nothing visible.
                Pawns?.RemoveWhere(p => !WillBeSaved(p));
            }

            Scribe_Collections.Look(ref Pawns, "Pawns", LookMode.Reference);

            if (Pawns is null)
            {
                Log.Warning($"[RoundMemory] ExposeData: для RoundMemory з tick={AbsTick} список Pawns порожній");
                Pawns = new();
            }
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Pawns.RemoveWhere(p => p is null);
            }
        }

        /// <summary>
        /// Whether the save is going to contain this pawn.
        ///
        /// A thing reaches the save through something that owns it: a map, a
        /// holder, a corpse, or the world-pawn list. Every uncertain answer is
        /// true - dropping a participant who would have survived loses a
        /// memory's cast, while keeping one who will not costs a warning the
        /// game prints anyway.
        ///
        /// Deliberately a copy rather than a shared helper. The same predicate
        /// lives in the compatibility mod and in Vanilla Traits Expanded, in
        /// each case owned by the assembly that needs it; none of the three may
        /// depend on the others, and the alternative is a shared assembly for
        /// nine lines.
        /// </summary>
        private static bool WillBeSaved(Pawn pawn)
        {
            if (pawn is null || pawn.Discarded) return false;
            if (pawn.Spawned || pawn.holdingOwner != null || pawn.ParentHolder != null) return true;

            var corpse = pawn.Corpse;
            if (corpse != null && (corpse.Spawned || corpse.holdingOwner != null)) return true;

            // No world to ask is not a moment to be guessing.
            if (Find.World is null || Find.WorldPawns is null) return true;

            return Find.WorldPawns.Contains(pawn);
        }

        public string GetUniqueLoadID()
        {
            return $"RoundMemory_{RoundMemoryUniqueID}";
        }
    }

}
