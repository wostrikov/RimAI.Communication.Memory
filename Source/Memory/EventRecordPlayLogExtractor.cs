using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory;

internal static class EventRecordPlayLogExtractor
{
        
        /// <summary>
        /// 每小时扫描PlayLog事件
        /// 生成全局公共殖民地历史常识
        /// ⚠️ v3.4.4: 性能优化 - 从尾部倒序扫描，避免全量遍历
        /// </summary>
        internal static void ScanRecentPlayLog()
        {
            EventRecordPlayLogScan.ScanRecentPlayLog();
        }

        
        /// <summary>
        /// 从LogEntry提取事件信息
        /// ? v3.3.4: 优化为提取关键信息，减少token消耗
        /// </summary>
        internal static string ExtractEventInfo(LogEntry logEntry)
        {
            return EventRecordPlayLogScan.ExtractEventInfo(logEntry);
        }

        
        /// <summary>
        /// ? v3.3.10: 提取关键信息，压缩事件文本（修复制造清单问题）
        /// 目标：将详细事件描述压缩为最重要的部分，减少token浪费
        /// 示例：
        /// - "小明在机械加工台上种植了12株玉米" → "小明种植玉米x12"
        /// - "张三击杀了袭击者（机械虫）" → "张三击杀机械虫"
        /// - "李四运输物资送到仓库" → "李四运输物资"
        /// - "王五完成target.A清单-手工制作台" → "王五在手工制作台制造物品"
        /// </summary>
        internal static string ExtractKeyInformation(string fullText)
        {
            if (string.IsNullOrEmpty(fullText))
                return null;
            
            try
            {
                // ? v3.3.10: 修复1 - 先检测并清理 "完成...清单" 模式
                fullText = CleanBillText(fullText);
                
                // 1. 提取主要人物（第一个出现的人名，通常是主语）
                string mainPerson = ExtractMainPerson(fullText);
                
                // 2. 提取核心动作（最重要的关键词）
                string action = ExtractMainAction(fullText);
                
                // 3. 提取目标/对象（第二个人名或者重要名词）
                string target = ExtractTarget(fullText, mainPerson);
                
                // 4. 提取数量信息（如 "x12"）
                string quantity = ExtractQuantity(fullText);
                
                // 5. 组合压缩文本
                var parts = new List<string>();
                
                if (!string.IsNullOrEmpty(mainPerson))
                    parts.Add(mainPerson);
                
                if (!string.IsNullOrEmpty(action))
                    parts.Add(action);
                
                if (!string.IsNullOrEmpty(target))
                    parts.Add(target);
                
                if (!string.IsNullOrEmpty(quantity))
                    parts.Add(quantity);
                
                // 如果提取失败，返回原文的截断
                if (parts.Count < 2)
                {
                    // 至少保证主语和动作，否则使用原文（截断）
                    return fullText.Length > 40 ? fullText.Substring(0, 40) : fullText;
                }
                
                return string.Join("", parts);
            }
            catch (Exception ex)
            {
                // 提取失败，返回原文截断
                if (Prefs.DevMode)
                    Log.Warning($"[EventRecord] Key info extraction failed: {ex.Message}");
                
                return fullText.Length > 40 ? fullText.Substring(0, 40) : fullText;
            }
        }
        
        /// <summary>
        /// ? v3.3.10: 清理制造清单相关的无用文本
        /// "完成target.A清单-手工制作台" → "在手工制作台制造物品"
        /// </summary>
        internal static string CleanBillText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            try
            {
                // 匹配模式："完成...清单-工作台名称"
                var billPattern = System.Text.RegularExpressions.Regex.Match(
                    text, 
                    @"完成(target\.[A-Za-z0-9]+|.+?)清单[－\-](.+?)(?:[，。、\s]|$)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                
                if (billPattern.Success)
                {
                    string workbenchName = billPattern.Groups[2].Value.Trim();
                    
                    // 清理工作台名称中的无用字符
                    workbenchName = System.Text.RegularExpressions.Regex.Replace(
                        workbenchName, 
                        @"[（\(].*?[）\)]", 
                        ""
                    ).Trim();
                    
                    // 提取主语（人名）
                    string[] words = text.Split(new[] { '完', '成', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string person = words.Length > 0 ? words[0].Trim() : "";
                    
                    // 重构为："人名在工作台制造物品"
                    if (!string.IsNullOrEmpty(person) && !string.IsNullOrEmpty(workbenchName))
                    {
                        return $"{person}在{workbenchName}制造物品";
                    }
                    else if (!string.IsNullOrEmpty(workbenchName))
                    {
                        return $"在{workbenchName}制造物品";
                    }
                }
                
                // 如果没有匹配，检查是否包含 target.* 模式，直接删除
                text = System.Text.RegularExpressions.Regex.Replace(
                    text,
                    @"target\.[A-Za-z0-9]+",
                    "",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                
                return text.Trim();
            }
            catch (Exception ex)
            {
                if (Prefs.DevMode)
                    Log.Warning($"[EventRecord] Bill text cleaning failed: {ex.Message}");
                
                return text;
            }
        }
        
        /// <summary>
        /// 提取目标/对象（通常是动作的接受者）
        /// </summary>
        internal static string ExtractTarget(string text, string mainPerson)
        {
            // 移除主要人物后，查找第二个名字或重要名词
            string remaining = text;
            if (!string.IsNullOrEmpty(mainPerson))
            {
                int index = text.IndexOf(mainPerson);
                if (index >= 0)
                {
                    remaining = text.Substring(index + mainPerson.Length);
                }
            }
            
            // 查找常见目标标记词后的内容
            var targetMarkers = new[] { "击杀", "攻击", "种植", "建造", "制作", "完成", "治疗", "了" };
            
            foreach (var marker in targetMarkers)
            {
                int markerIndex = remaining.IndexOf(marker);
                if (markerIndex >= 0)
                {
                    // 提取标记词后的10字符
                    int start = markerIndex + marker.Length;
                    if (start < remaining.Length)
                    {
                        string targetText = remaining.Substring(start, Math.Min(15, remaining.Length - start));
                        
                        // 清理标点和多余文字
                        targetText = targetText.Split(new[] { '，', '。', '、', '；', '的', '在', '和', ' ' })[0].Trim();
                        
                        if (targetText.Length >= 2 && targetText.Length <= 10)
                        {
                            return targetText;
                        }
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 提取主要人物（通常是事件的主体）
        /// </summary>
        internal static string ExtractMainPerson(string text)
        {
            // 方法1：查找常见名字分隔符前的文本
            var separators = new[] { "在", "的", "将", "被", "向", "与", "和", "对", " " };
            
            foreach (var sep in separators)
            {
                int index = text.IndexOf(sep);
                if (index > 0 && index < 15) // 名字通常在前15字符内
                {
                    string candidate = text.Substring(0, index).Trim();
                    
                    // 验证是否像名字（2-8字符，无标点）
                    if (candidate.Length >= 2 && candidate.Length <= 8 && 
                        !candidate.Any(c => char.IsPunctuation(c) || char.IsDigit(c)))
                    {
                        return candidate;
                    }
                }
            }
            
            // 方法2：如果找不到，取前5个字符（可能是名字）
            if (text.Length >= 2)
            {
                string candidate = text.Substring(0, Math.Min(5, text.Length)).Trim();
                if (!candidate.Any(c => char.IsPunctuation(c)))
                    return candidate;
            }
            
            return null;
        }
        
        /// <summary>
        /// 提取核心动作（基于重要关键词）
        /// </summary>
        internal static string ExtractMainAction(string text)
        {
            // 查找文本中的重要关键词
            var matchedKeywords = EventRecordKnowledgeGenerator.ImportantKeywords.Keys
                .Where(k => text.Contains(k))
                .OrderByDescending(k => EventRecordKnowledgeGenerator.ImportantKeywords[k]) // 按重要性排序
                .ThenByDescending(k => k.Length) // 优先长关键词
                .ToList();
            
            if (matchedKeywords.Any())
            {
                // 返回最重要的关键词作为核心动作
                return matchedKeywords.First();
            }
            
            // 如果没有匹配关键词，尝试提取动词（简单启发式）
            var commonActions = new[] { "种植", "建造", "完成", "击杀", "攻击", "防御", "制作", "烹饪", 
                                         "研究", "治疗", "加入", "离开", "死亡", "受伤" };
            
            foreach (var action in commonActions)
            {
                if (text.Contains(action))
                    return action;
            }
            
            return null;
        }
        
        /// <summary>
        /// ? v3.3.3: 从带时间前缀的事件文本中提取原始文本
        /// </summary>
        internal static string ExtractOriginalEventText(string eventText)
        {
            if (string.IsNullOrEmpty(eventText))
                return eventText;
            
            // 移除常见的时间前缀
            string[] timePrefixes = { "今天", "1天前", "2天前", "3天前", "4天前", "5天前", "6天前",
                                     "约3天前", "约4天前", "约5天前", "约6天前", "约7天前" };
            
            foreach (var prefix in timePrefixes)
            {
                if (eventText.StartsWith(prefix))
                {
                    return eventText.Substring(prefix.Length);
                }
            }
            
            return eventText;
        }
        
        /// <summary>
        /// 计算事件重要性
        /// </summary>
        internal static float CalculateImportance(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0.5f;
            
            // 找到匹配的关键词
            var matched = EventRecordKnowledgeGenerator.ImportantKeywords
                .Where(kv => text.Contains(kv.Key))
                .OrderByDescending(kv => kv.Value)
                .FirstOrDefault();
            
            if (matched.Key != null)
            {
                return matched.Value;
            }
            
            // ? 新增：没有关键词匹配但可能是Incident事件，给较低默认重要性
            return 0.4f; // 比普通事件低，但仍会被记录
        }
        
        /// <summary>
        /// 提取数量信息（如 "×12"）
        /// </summary>
        internal static string ExtractQuantity(string text)
        {
            // 查找数字
            var match = System.Text.RegularExpressions.Regex.Match(text, @"\d+");
            
            if (match.Success)
            {
                int number = int.Parse(match.Value);
                
                // 只保留有意义的数量（>1)
                if (number > 1 && number < 10000)
                {
                    return $"×{number}";
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// ? 新增：检测是否是对话内容（避免记录对话）
        /// </summary>
        internal static bool IsConversationContent(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            
            // 检测对话标记
            string[] conversationMarkers = { 
                "说:", "said:", "说：", "说道:", "说道：",
                "问:", "asked:", "问：", "问道:", "问道：",
                "回答:", "replied:", "回答：", "答道:", "答道：",
                "叫道:", "shouted:", "叫道：", "喊道:", "喊道："
            };
            
            return conversationMarkers.Any(marker => text.Contains(marker));
        }
        
        /// <summary>
        /// 过滤无聊事件
        /// </summary>
        internal static bool IsBoringMessage(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;
            
            var boringKeywords = new[] 
            { 
                "走路", "吃饭", "睡觉", "娱乐", "闲逛", "休息",
                "walking", "eating", "sleeping", "recreation", "wandering"
            };
            
            return boringKeywords.Any(k => text.Contains(k));
        }
}
