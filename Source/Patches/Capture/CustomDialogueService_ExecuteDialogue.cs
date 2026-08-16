using HarmonyLib;
using Ustas.RimAI.Communication.Service;
using Verse;

namespace Ustas.RimAI.Communication.Memory.Patches.Capture
{

    // 捕获玩家发言
    [HarmonyPatch(typeof(CustomDialogueService), "ExecuteDialogue")]
    public static class CustomDialogueService_ExecuteDialogue
    {
        [HarmonyPostfix]
        static void Postfix(Pawn initiator, string message)
        {
            RoundMemoryManager.CapturePlayerDialogue(initiator, message);
        }
    }

}
