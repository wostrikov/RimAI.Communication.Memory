using Verse;

namespace Ustas.RimAI.Communication.Memory.Patches
{
    /// <summary>
    /// Minimal dialogue-context cache used by the untyped Scriban fallback path.
    /// Orphan prompt/injection APIs removed in 7.5.9 Wave D.
    /// </summary>
    public static class RimTalkMemoryAPI
    {
        private static string lastRimTalkContext = "";
        private static Pawn lastRimTalkPawn = null;
        private static int lastRimTalkTick = 0;

        /// <summary>Returns the last cached RimTalk context (may be empty when unused).</summary>
        public static string GetLastRimTalkContext(out Pawn pawn, out int tick)
        {
            pawn = lastRimTalkPawn;
            tick = lastRimTalkTick;
            return lastRimTalkContext;
        }
    }
}
