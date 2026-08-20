using Verse;

namespace Ustas.RimAI.Communication.Memory.Patches
{
    public static class RimTalkMemoryAPI
    {
        private static string lastRimTalkContext = "";
        private static Pawn lastRimTalkPawn = null;
        private static int lastRimTalkTick = 0;

        public static string GetLastRimTalkContext(out Pawn pawn, out int tick)
        {
            pawn = lastRimTalkPawn;
            tick = lastRimTalkTick;
            return lastRimTalkContext;
        }
    }
}
