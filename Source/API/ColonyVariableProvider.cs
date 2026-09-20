using System;
using Ustas.RimAI.Communication.Memory.Colony;
using Ustas.RimAI.Core.Diagnostics;
using Verse;

namespace Ustas.RimAI.Communication.Memory.API
{
    /// <summary>
    /// Provides {{colony}} — how the place the speakers live in is doing.
    ///
    /// Every other variable this module registers is about a pawn. This one is
    /// about the colony, so it is a context variable rather than a pawn one:
    /// there is one answer per map and every speaker on that map shares it.
    /// </summary>
    public static class ColonyVariableProvider
    {
        public static string GetColonyTrend(object context)
        {
            try
            {
                ColonyTrendManager manager = ColonyTrendManager.Current;
                if (manager == null)
                {
                    return string.Empty;
                }

                Map map = Find.CurrentMap;
                return map == null ? string.Empty : manager.TrendFor(map);
            }
            // RimAI.catch-boundary: ALLOWED_TOP_LEVEL_BOUNDARY - a Scriban variable is an engine callback; a failed lookup costs this line of context, never the talk.
            catch (Exception exception)
            {
                RimAiLog.Warning(RimAiLogCategory.Memory,
                    "[MemoryPatch] Error getting the colony trend.", exception: exception);
                return string.Empty;
            }
        }
    }
}
