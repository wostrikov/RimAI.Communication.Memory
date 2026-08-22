using System;
using System.Collections.Generic;

namespace Ustas.RimAI.Communication.Memory.Policy
{
    public static class MemoryScopeKeys
    {
        public const string PersonalActive = "activeMemories";
        public const string PersonalSituational = "situationalMemories";
        public const string PersonalEventLog = "eventLogMemories";
        public const string PersonalArchive = "archiveMemories";
        public const string WorldKnowledge = "commonKnowledge";
        public const string GameRounds = "RoundMemories";
        public const string GameNextRoundId = "NextRoundMemoryId";
    }

    public sealed class MemoryEntrySnapshot
    {
        public string Id;
        public int GameTick;
        public string Content;
        public string Layer;
        public float Importance;
        public float Activity;
        public bool Pinned;
        public bool Summarized;
        public readonly List<string> Tags = new List<string>();
        public readonly List<string> Keywords = new List<string>();

        public MemoryEntrySnapshot Clone()
        {
            var copy = new MemoryEntrySnapshot
            {
                Id = Id,
                GameTick = GameTick,
                Content = Content,
                Layer = Layer,
                Importance = Importance,
                Activity = Activity,
                Pinned = Pinned,
                Summarized = Summarized
            };
            copy.Tags.AddRange(Tags);
            copy.Keywords.AddRange(Keywords);
            return copy;
        }
    }

    public sealed class KnowledgeEntrySnapshot
    {
        public string Id;
        public string Tag;
        public string Content;
        public float Importance;
        public bool Enabled;
        public readonly List<string> Tags = new List<string>();

        public KnowledgeEntrySnapshot Clone()
        {
            var copy = new KnowledgeEntrySnapshot
            {
                Id = Id,
                Tag = Tag,
                Content = Content,
                Importance = Importance,
                Enabled = Enabled
            };
            copy.Tags.AddRange(Tags);
            return copy;
        }
    }

    public sealed class RoundMemorySnapshot
    {
        public long Id;
        public int Tick;
        public string Content;
        public readonly List<int> ParticipantPawnIds = new List<int>();

        public RoundMemorySnapshot Clone()
        {
            var copy = new RoundMemorySnapshot
            {
                Id = Id,
                Tick = Tick,
                Content = Content
            };
            copy.ParticipantPawnIds.AddRange(ParticipantPawnIds);
            return copy;
        }
    }

    public sealed class ThreeScopeSnapshot
    {
        public readonly List<MemoryEntrySnapshot> Personal = new List<MemoryEntrySnapshot>();
        public readonly List<KnowledgeEntrySnapshot> WorldKnowledge = new List<KnowledgeEntrySnapshot>();
        public readonly List<RoundMemorySnapshot> GameRounds = new List<RoundMemorySnapshot>();
        public long NextRoundMemoryId;
        public readonly Dictionary<int, long> AbmRoundPointers = new Dictionary<int, long>();
    }

    public static class MemoryScopeRoundtrip
    {
        public static ThreeScopeSnapshot Clone(ThreeScopeSnapshot source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var copy = new ThreeScopeSnapshot
            {
                NextRoundMemoryId = source.NextRoundMemoryId
            };
            for (int i = 0; i < source.Personal.Count; i++)
                copy.Personal.Add(source.Personal[i].Clone());
            for (int i = 0; i < source.WorldKnowledge.Count; i++)
                copy.WorldKnowledge.Add(source.WorldKnowledge[i].Clone());
            for (int i = 0; i < source.GameRounds.Count; i++)
                copy.GameRounds.Add(source.GameRounds[i].Clone());
            foreach (var pair in source.AbmRoundPointers)
                copy.AbmRoundPointers[pair.Key] = pair.Value;
            return copy;
        }

        public static int RepairAbmPointers(ThreeScopeSnapshot snapshot)
        {
            if (snapshot == null)
                return 0;

            var byId = new Dictionary<long, RoundMemorySnapshot>();
            for (int i = 0; i < snapshot.GameRounds.Count; i++)
            {
                RoundMemorySnapshot round = snapshot.GameRounds[i];
                if (round != null)
                    byId[round.Id] = round;
            }

            int repaired = 0;
            var keys = new List<int>(snapshot.AbmRoundPointers.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                int pawnId = keys[i];
                long pointer = snapshot.AbmRoundPointers[pawnId];
                if (byId.ContainsKey(pointer))
                    continue;
                snapshot.AbmRoundPointers.Remove(pawnId);
                repaired++;
            }
            return repaired;
        }
    }
}
