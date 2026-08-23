using System.Collections.Generic;
using System.Text;

namespace Ustas.RimAI.Communication.Memory.Policy
{
    /// <summary>
    /// Authoritative round-memory write-through: every listed participant,
    /// including silent ones, receives the same roster + replies record.
    /// </summary>
    public static class MemoryRoundConversationPolicy
    {
        public const string ParticipantsPrefix = "Participants: ";

        public static bool ShouldWriteThrough(bool isListedParticipant) =>
            isListedParticipant;

        public static string ComposeRoster(IReadOnlyList<string> names)
        {
            if (names == null || names.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            for (int i = 0; i < names.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(names[i]))
                    continue;
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(names[i].Trim());
            }
            return sb.ToString();
        }

        public static string ComposeRecord(IReadOnlyList<string> names, string replies)
        {
            string roster = ComposeRoster(names);
            if (string.IsNullOrEmpty(roster))
                return null;

            string header = "[" + ParticipantsPrefix + roster + "]";
            if (string.IsNullOrWhiteSpace(replies))
                return header;
            return header + "\n" + replies.Trim();
        }

        public static bool IncludesSilentParticipant(string record, string silentName, string spokenName)
        {
            if (string.IsNullOrWhiteSpace(record) || string.IsNullOrWhiteSpace(silentName))
                return false;
            if (record.IndexOf(silentName, System.StringComparison.Ordinal) < 0)
                return false;
            if (!string.IsNullOrWhiteSpace(spokenName) &&
                record.IndexOf(spokenName + ":", System.StringComparison.Ordinal) < 0)
                return false;
            return record.IndexOf(silentName + ":", System.StringComparison.Ordinal) < 0;
        }
    }
}
