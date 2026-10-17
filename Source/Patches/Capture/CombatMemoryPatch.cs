using HarmonyLib;
using Ustas.RimAI.Communication.Memory.Capture;
using Verse;

namespace Ustas.RimAI.Communication.Memory.Patches.Capture
{
    // Pawn overrides ThingWithComps.Notify_UsedVerb(Pawn pawn, Verb verb), and
    // Verb.TryCastNextBurstShot calls it on the caster only after TryCastShot
    // succeeded. The argument types are named so a future overload cannot move
    // the patch.
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Notify_UsedVerb), new[] { typeof(Pawn), typeof(Verb) })]
    public static class CombatMemoryPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance, Verb verb)
        {
            CombatMemoryCapturer.CaptureAttackEnter(__instance, verb);
        }
    }
}
