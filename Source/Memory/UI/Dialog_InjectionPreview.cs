
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.API;
using Ustas.RimAI.Communication.Prompt;

namespace Ustas.RimAI.Communication.Memory.Debug
{
    /// <summary>
    /// 调试预览器 - 分析记忆和常识注入内容
    /// v5.0 重构版
    /// 
    /// 主要功能:
    /// 1. 常识部分: 匹配源选择、Scriban解析、被匹配常识显示
    /// 2. 记忆部分: 保持现状
    /// 
    /// ⭐ 匹配源与 CommunicationSettings.knowledgeMatchingSources 同步
    /// </summary>
    public class Dialog_InjectionPreview : Window
    {
        internal Dialog_InjectionPreviewParts Parts;

        // ===== 状态字段 =====
        internal Pawn selectedPawn;
        internal Pawn targetPawn;
        internal Vector2 scrollPositionLeft;
        internal Vector2 scrollPositionRight;
        internal Vector2 scrollPositionMatchSource;
        internal Vector2 scrollPositionParsedText;
        
        // ===== 常识匹配源（与Settings同步）=====
        internal List<(string name, string description, bool isPawnProperty)> availableMatchingSources;
        internal string parsedMatchText = "";
        internal List<KnowledgeScore> matchedKnowledge = new List<KnowledgeScore>();
        
        // ===== 记忆预览 =====
        internal string memoryPreviewText = "";
        internal int cachedMemoryCount = 0;
        
        // ===== UI状态 =====
        internal bool showMatchSourcePanel = true;
        internal bool showKnowledgePanel = true;
        internal bool showMemoryPanel = true;

        public override Vector2 InitialSize => new Vector2(1200f, 800f);

        public Dialog_InjectionPreview()
        {
            Parts = new Dialog_InjectionPreviewParts(this);
            this.doCloseX = true;
            this.doCloseButton = true;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = true;
            
            // 默认选择第一个殖民者
            if (Find.CurrentMap != null)
            {
                selectedPawn = Find.CurrentMap.mapPawns.FreeColonists.FirstOrDefault();
            }
            
            // 初始化匹配源
            LoadAvailableMatchingSources();
        }

        

        #region Pawn选择器
        
        

        
        
        #endregion

        #region 常识面板
        
        
        
        /// <summary>
        /// 绘制匹配源选择器
        /// ⭐ 与 Settings.knowledgeMatchingSources 同步
        /// </summary>
        
        
        
        
        #endregion

        #region 记忆面板
        
        
        
        
        
        
        
        #endregion

        #region 数据加载和刷新
        
        
        
        
        
        /// <summary>
        /// 刷新解析后的匹配文本
        /// ⭐ 使用 RimTalk 的 ScribanParser.Render 静默解析
        /// ⭐ Pawn 属性变量会同时解析所有参与者，用逗号分隔
        /// </summary>
        
        
        /// <summary>
        /// 使用 RimTalk 的 ScribanParser.Render 进行解析
        /// ⭐ logErrors = false 以静默模式运行
        /// ⭐ isPawnProperty = true 时使用 Pawn 对象解析
        /// ⭐ isPawnProperty = false 时使用 PromptManager.LastContext 解析（包含 prompt 等上下文变量）
        /// </summary>
        
        
        /// <summary>
        /// 刷新匹配的常识列表
        /// </summary>
        
        
        /// <summary>
        /// 刷新记忆预览
        /// </summary>
        
        
        
        
        
        
        /// <summary>
        /// 显示使用帮助对话框
        /// </summary>
        
        
        #endregion
    
        #region Cluster forwards
        public override void DoWindowContents(Rect inRect) => Parts.PawnUi.DoWindowContents(inRect);
        internal void DrawPawnSelectors(Rect rect) => Parts.PawnUi.DrawPawnSelectors(rect);
        internal void ShowPawnSelectionMenu(bool isPrimary) => Parts.PawnUi.ShowPawnSelectionMenu(isPrimary);
        internal void DrawKnowledgePanel(Rect rect) => Parts.Knowledge.DrawKnowledgePanel(rect);
        internal void DrawMatchingSourceSelector(Rect rect) => Parts.Knowledge.DrawMatchingSourceSelector(rect);
        internal void DrawMatchedKnowledgeList(Rect rect) => Parts.Knowledge.DrawMatchedKnowledgeList(rect);
        internal void DrawMemoryPanel(Rect rect) => Parts.Memory.DrawMemoryPanel(rect);
        internal void DrawMemoryStats(Rect rect, FourLayerMemoryComp memoryComp) => Parts.Memory.DrawMemoryStats(rect, memoryComp);
        internal void DrawMemoryPreviewContent(Rect rect) => Parts.Memory.DrawMemoryPreviewContent(rect);
        internal string GetLayerTag(MemoryLayer layer) => Parts.Memory.GetLayerTag(layer);
        internal string GetTypeTag(MemoryType type) => Parts.Memory.GetTypeTag(type);
        internal void ShowHelpDialog() => Parts.Memory.ShowHelpDialog();
        internal void LoadAvailableMatchingSources() => Parts.Refresh.LoadAvailableMatchingSources();
        internal void RefreshPreview() => Parts.Refresh.RefreshPreview();
        internal void RefreshParsedMatchText() => Parts.Refresh.RefreshParsedMatchText();
        internal string RenderWithScriban(string template, Pawn pawn, Pawn recipient, bool isPawnProperty = true) => Parts.Refresh.RenderWithScriban(template, pawn, recipient, isPawnProperty);
        internal void RefreshMatchedKnowledge() => Parts.Refresh.RefreshMatchedKnowledge();
        internal void RefreshMemoryPreview() => Parts.Refresh.RefreshMemoryPreview();
        #endregion
}
    internal sealed class InjectionPreviewRefresh : Dialog_InjectionPreviewCollaborator
    {
        internal InjectionPreviewRefresh(Dialog_InjectionPreview owner) : base(owner) { }

internal void LoadAvailableMatchingSources()
        {
            availableMatchingSources = MustacheVariableHelper.GetMatchingPropertyCategories();
            
            if (availableMatchingSources == null || availableMatchingSources.Count == 0)
            {
                // 使用备用列表
                availableMatchingSources = new List<(string, string, bool)>
                {
                    ("prompt", "Dialogue prompt", false),
                    ("fullname", "Pawn full name", true),
                    ("role", "Pawn role", true),
                    ("age", "Pawn age", true),
                    ("gender", "Pawn gender", true),
                    ("backstory", "Pawn backstory", true),
                    ("traits", "Pawn traits", true),
                    ("skills", "Skills", true),
                    ("relations", "Relations", true),
                };
            }
        }

internal void RefreshPreview()
        {
            RefreshParsedMatchText();
            RefreshMatchedKnowledge();
            RefreshMemoryPreview();
        }

internal void RefreshParsedMatchText()
        {
            var settings = RimTalkMemoryPatchMod.Settings;
            if (selectedPawn == null || settings == null || settings.knowledgeMatchingSources.Count == 0)
            {
                parsedMatchText = "";
                return;
            }
            
            var sb = new StringBuilder();
            
            // 构建参与者列表
            var participants = new List<Pawn> { selectedPawn };
            if (targetPawn != null && targetPawn != selectedPawn)
            {
                participants.Add(targetPawn);
            }
            
            foreach (var sourceName in settings.knowledgeMatchingSources)
            {
                try
                {
                    // 判断是否是Pawn属性
                    bool isPawnProperty = availableMatchingSources
                        .Any(s => s.name == sourceName && s.isPawnProperty);
                    
                    if (isPawnProperty)
                    {
                        // ⭐ 同时解析所有参与者的属性值
                        var values = new List<string>();
                        var pawnNames = new List<string>();
                        
                        foreach (var pawn in participants)
                        {
                            // isPawnProperty = true 使用 Pawn 对象解析
                            string parsed = RenderWithScriban($"{{{{ pawn.{sourceName} }}}}", pawn, null, isPawnProperty: true);
                            if (!string.IsNullOrEmpty(parsed) && !parsed.Contains("{{"))
                            {
                                values.Add(parsed);
                                pawnNames.Add($"@{pawn.LabelShort}");
                            }
                        }
                        
                        if (values.Count > 0)
                        {
                            if (sb.Length > 0) sb.AppendLine();
                            // 格式: [name @当前角色 @目标角色]
                            sb.AppendLine($"[{sourceName} {string.Join(" ", pawnNames)}]");
                            // 值用逗号加换行分隔，可读性更好
                            sb.Append(string.Join(",\n", values));
                        }
                    }
                    else
                    {
                        // 非 Pawn 属性（如 prompt），使用 PromptManager.LastContext 解析
                        // isPawnProperty = false 使用上次对话的完整上下文
                        string parsed = RenderWithScriban($"{{{{ {sourceName} }}}}", selectedPawn, targetPawn, isPawnProperty: false);
                        if (!string.IsNullOrEmpty(parsed) && !parsed.Contains("{{"))
                        {
                            if (sb.Length > 0) sb.AppendLine();
                            sb.AppendLine($"[{sourceName}]");
                            sb.Append(parsed);
                        }
                    }
                }
                catch
                {
                    // 静默处理任何解析错误
                }
            }
            
            parsedMatchText = sb.ToString();
        }

internal string RenderWithScriban(string template, Pawn pawn, Pawn recipient, bool isPawnProperty = true)
        {
            try
            {
                PromptContext ctx;
                if (isPawnProperty)
                {
                    ctx = new PromptContext(pawn) { IsPreview = true };
                    if (recipient != null)
                        ctx.AllPawns = new List<Pawn> { pawn, recipient };
                }
                else
                {
                    ctx = PromptManager.LastContext;
                    if (ctx == null)
                        return "RimTalk_Preview_NoContext".Translate();
                    ctx.IsPreview = true;
                }

                return ScribanParser.Render(template, ctx, false) ?? template;
            }
            catch
            {
                return template;
            }
        }

internal void RefreshMatchedKnowledge()
        {
            matchedKnowledge.Clear();
            
            if (selectedPawn == null || string.IsNullOrEmpty(parsedMatchText))
                return;
            
            var memoryManager = Find.World?.GetComponent<MemoryManager>();
            if (memoryManager?.CommonKnowledge == null)
                return;
            
            var settings = RimTalkMemoryPatchMod.Settings;
            if (settings == null)
                return;
            
            try
            {
                List<KnowledgeScore> scores;
                memoryManager.CommonKnowledge.InjectKnowledgeWithDetails(
                    parsedMatchText,
                    settings.maxInjectedKnowledge,
                    out scores,
                    selectedPawn,
                    targetPawn
                );
                
                if (scores != null)
                {
                    matchedKnowledge = scores;
                }
            }
            catch
            {
                // 静默处理错误
            }
        }

internal void RefreshMemoryPreview()
        {
            memoryPreviewText = "";
            cachedMemoryCount = 0;
            
            if (selectedPawn == null)
                return;
            
            var memoryComp = selectedPawn.TryGetComp<FourLayerMemoryComp>();
            if (memoryComp == null)
            {
                memoryPreviewText = "RimTalk_Preview_NoMemoryComp".Translate();
                return;
            }
            
            var settings = RimTalkMemoryPatchMod.Settings;
            if (settings == null)
                return;
            
            try
            {
                var sb = new StringBuilder();
                
                // 使用动态注入获取记忆
                if (settings.useDynamicInjection)
                {
                    List<DynamicMemoryInjection.MemoryScore> memoryScores;
                    string memoryInjection = DynamicMemoryInjection.InjectMemoriesWithDetails(
                        memoryComp,
                        parsedMatchText,
                        settings.maxInjectedMemories,
                        out memoryScores
                    );
                    
                    if (memoryScores != null && memoryScores.Count > 0)
                    {
                        cachedMemoryCount = memoryScores.Count;
                        
                        sb.AppendLine("RimTalk_Preview_SelectedMemories".Translate(memoryScores.Count));
                        sb.AppendLine();
                        
                        for (int i = 0; i < memoryScores.Count; i++)
                        {
                            var score = memoryScores[i];
                            var memory = score.Memory;
                            
                            string layerTag = Owner.GetLayerTag(memory.Layer);
                            
                            sb.AppendLine($"[{i + 1}] {layerTag} {Owner.GetTypeTag(memory.Type)}");
                            sb.AppendLine($"    " + "RimTalk_Preview_ScoreDetail".Translate(score.TotalScore.ToString("F3")));
                            sb.AppendLine($"    {memory.DisplayContent}");
                            sb.AppendLine();
                        }
                    }
                    else
                    {
                        sb.AppendLine("RimTalk_Preview_NoMemoriesAboveThreshold".Translate());
                    }
                }
                else
                {
                    // 静态注入模式
                    sb.AppendLine("RimTalk_Preview_StaticMode".Translate());
                    sb.AppendLine();
                    
                    int count = 0;
                    foreach (var memory in memoryComp.EventLogMemories.Take(5))
                    {
                        count++;
                        sb.AppendLine($"[ELS-{count}] {memory.DisplayContent}");
                    }
                    foreach (var memory in memoryComp.ArchiveMemories.Take(5))
                    {
                        count++;
                        sb.AppendLine($"[CLPA-{count}] {memory.DisplayContent}");
                    }
                    
                    cachedMemoryCount = count;
                }
                
                memoryPreviewText = sb.ToString();
            }
            catch (Exception ex)
            {
                memoryPreviewText = "RimTalk_Preview_Error".Translate(ex.Message);
            }
        }
    }

    internal sealed class Dialog_InjectionPreviewParts
    {
        internal readonly Dialog_InjectionPreview Owner;
        internal readonly InjectionPreviewPawnUi PawnUi;
        internal readonly InjectionPreviewKnowledge Knowledge;
        internal readonly InjectionPreviewMemory Memory;
        internal readonly InjectionPreviewRefresh Refresh;
        internal Dialog_InjectionPreviewParts(Dialog_InjectionPreview owner)
        {
            Owner = owner;
            PawnUi = new InjectionPreviewPawnUi(owner);
            Knowledge = new InjectionPreviewKnowledge(owner);
            Memory = new InjectionPreviewMemory(owner);
            Refresh = new InjectionPreviewRefresh(owner);
        }
    }

}
