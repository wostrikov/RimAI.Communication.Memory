using HarmonyLib;
using Verse;
using System.Collections.Generic;

namespace Ustas.RimAI.Communication.Memory.Patches
{

    [HarmonyPatch(typeof(ThingWithComps), "InitializeComps")]
    public static class ThingWithComps_InitializeComps_Patch
    {
        private static readonly CompProperties_PawnMemory _pawnMemoryProps = new CompProperties_PawnMemory();

        [HarmonyPostfix]
        public static void Postfix(ThingWithComps __instance, ref List<ThingComp> ___comps)
        {
            if (__instance is not Pawn pawn || (!pawn.RaceProps?.Humanlike ?? true)) return;

            if (pawn.GetComp<PawnMemoryComp>() is null)
            {
                var comp = new PawnMemoryComp();
                comp.parent = __instance;

                ___comps ??= new();
                ___comps.Add(comp);
                comp.Initialize(_pawnMemoryProps);
            }
        }
    }

}
