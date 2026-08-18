using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.Patches;

namespace Ustas.RimAI.Communication.Memory
{
    internal sealed class MemoryManagerConversation : MemoryManagerCollaborator
    {
        internal MemoryManagerConversation(MemoryManager owner) : base(owner) { }

internal string FormatConversationText(PendingConversation record)
        {
            var sb = new StringBuilder();
            
            // 第一行：参与者列表
            if (record.ParticipantNames != null && record.ParticipantNames.Count > 0)
            {
                sb.AppendLine($"[Учасники розмови: {string.Join(", ", record.ParticipantNames)}]");
            }
            
            // 对话内容
            foreach (var line in record.RawDialogue)
            {
                sb.AppendLine($"{line.SpeakerName}: \"{line.Text}\"");
            }
            
            return sb.ToString().TrimEnd();
        }

internal List<Pawn> FindPawnsByThingIds(List<string> thingIds)
        {
            var result = new List<Pawn>();
            
            if (thingIds == null || thingIds.Count == 0)
                return result;
            
            // 创建 HashSet 加速查找
            var idSet = new HashSet<string>(thingIds);
            
            foreach (var map in Find.Maps)
            {
                foreach (var pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (idSet.Contains(pawn.ThingID))
                    {
                        result.Add(pawn);
                        idSet.Remove(pawn.ThingID); // 找到后移除，避免重复
                        
                        if (idSet.Count == 0)
                            return result; // 所有都找到了
                    }
                }
            }
            
            return result;
        }
    }
}
