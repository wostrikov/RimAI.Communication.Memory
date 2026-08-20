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
            return string.Join(", ", Pawns
                .Select(p => p?.LabelShort)
                .Where(n => n is not null)
            );
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref RoundMemoryUniqueID, "RoundMemoryUniqueID", -1);

            Scribe_Values.Look(ref AbsTick, "AbsTick", -1);

            Scribe_Values.Look(ref planetTile, "Tile", PlanetTile.Invalid);
            Scribe_Values.Look(ref IsHomeMap, "IsHomeMap", false);

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

        public string GetUniqueLoadID()
        {
            return $"RoundMemory_{RoundMemoryUniqueID}";
        }
    }

}
