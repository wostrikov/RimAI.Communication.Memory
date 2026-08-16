using HarmonyLib;
using Ustas.RimAI.Communication.Data;
using Ustas.RimAI.Communication.Prompt;
using System.Collections.Generic;
using Verse;

namespace Ustas.RimAI.Communication.Memory.Patches
{

    // 填充 Participants
    // 注意！！！后续考虑将此逻辑直接并入 RimTalk 本体中！
    [HarmonyPatch(typeof(PromptContext), "FromTalkRequest")]
    public static class PromptContext_FromTalkRequest_Patch
    {
        [HarmonyPrefix]
        static void Prefix(TalkRequest request, List<Pawn> pawns)
        {
            if (request != null)
            {
                request.Participants = pawns;
            }
        }
    }

}
