using HarmonyLib;
using Ustas.RimAI.Communication.Memory;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace Ustas.RimAI.Communication.Memory.Patches.Relations
{

    // 捕获RimChat对话
    [HarmonyPatch("Ustas.RimAI.Communication.Relations.Memory.RpgNpcDialogueArchiveManager", "RecordDiplomacySummary")]
    public static class RpgNpcDialogueArchiveManager_RecordDiplomacySummary_Patch
    {
        // 声明字段访问器
        private static Type dialogueMessageDataType;
        private static AccessTools.FieldRef<object, bool> isPlayerRef;
        private static AccessTools.FieldRef<object, string> messageRef;

        //声明方法访问器
        private static FastInvokeHandler isSystemMessageInvoker;

        private static bool IsEnable => RimTalkMemoryPatchMod.Settings?.IsRoundMemoryActive ?? false;

        // 通过Prepare方法控制补丁启用，并初始化访问器
        [HarmonyPrepare]
        static bool Prepare()
        {
            try
            {
                // 尝试获取ChatMessageData类
                dialogueMessageDataType = AccessTools.TypeByName("Ustas.RimAI.Communication.Relations.Memory.DialogueMessageData");
                if (dialogueMessageDataType is null)
                {
                    Log.Message("[RimAI.Memory]: тип DialogueMessageData не знайдено; patch вимкнено.");
                    return false;
                }

                // 获取成功，尝试获取方法
                var isSystemMessageMethod = AccessTools.Method(dialogueMessageDataType, "IsSystemMessage");
                if (isSystemMessageMethod is null)
                {
                    Log.Message("[RimAI.Memory]: метод IsSystemMessage не знайдено; patch вимкнено.");
                    return false;
                }

                // 获取成功，初始化访问器
                isSystemMessageInvoker = MethodInvoker.GetHandler(isSystemMessageMethod);
                isPlayerRef = AccessTools.FieldRefAccess<bool>(dialogueMessageDataType, "isPlayer");
                messageRef = AccessTools.FieldRefAccess<string>(dialogueMessageDataType, "message");

                Log.Message("[RimAI.Memory]: усі засоби доступу успішно ініціалізовано.");
                return true;
            }
            catch 
            {
                Log.Error("[RimAI.Memory]: помилка під час ініціалізації patch RimChat");
                return false;
            }
        }

        // 补丁主体
        [HarmonyPrefix]
        static void Prefix(Pawn negotiator, Faction faction, IList allMessages)
        {
            // Debug-only hook intentionally stays silent in normal runtime.

            // 轮次记忆关闭时不启用
            if (!IsEnable) return;

            if (allMessages is null || allMessages.Count == 0) return;

            // 构建文本块
            StringBuilder sb = new();

            // 获取名字
            string playerName = negotiator?.LabelShort ?? "???";
            string factionName = faction?.Name ?? "???";

            // 开始构建
            foreach (var dialogueMessage in allMessages)
            {
                // 跳过系统消息和无效消息
                if (dialogueMessage is null || (bool)isSystemMessageInvoker(dialogueMessage)) continue;

                if (isPlayerRef(dialogueMessage))
                {
                    // 玩家发言
                    sb.Append(playerName).Append(": ").AppendLine(messageRef(dialogueMessage));
                }
                else
                {
                    // NPC派系发言
                    sb.Append(factionName).Append(": ").AppendLine(messageRef(dialogueMessage));
                }
            }
            // 若content将为空，则直接剪枝
            if (sb.Length == 0) return;

            // 取出最终字符串并剔除末尾多余的一个换行符
            string content = sb.ToString().TrimEnd();

            // 构建参与者集合
            // 其实可以把派系发言人从dialogueMessage里扒出来
            // 但向地图外的pawn添加轮次记忆感觉不是很安全，遂作罢
            HashSet<Pawn> pawns = [negotiator];

            // Captured dialogue details are intentionally not logged.

            // 将数据传给RoundMemoryManager
            RoundMemoryManager.BuildRoundMemory(pawns, content);
        }
    }

}
