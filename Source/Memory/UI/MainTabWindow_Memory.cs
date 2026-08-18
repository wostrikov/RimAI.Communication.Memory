using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using Ustas.RimAI.Communication.Memory;
using System;

namespace Ustas.RimAI.Communication.Memory.UI
{
    /// <summary>
    /// Mind Stream Timeline window shell. Drawing and actions live on MemoryTab collaborators.
    /// </summary>
    public class MainTabWindow_Memory : MainTabWindow
    {
        internal MemoryTabParts Parts;

        internal Pawn selectedPawn = null;
        internal FourLayerMemoryComp currentMemoryComp = null;
        internal bool showAllHumanlikes = false;
        internal HashSet<MemoryEntry> selectedMemories = new HashSet<MemoryEntry>();
        internal MemoryEntry lastSelectedMemory = null;
        internal bool isDragging = false;
        internal bool isMouseDown = false;
        internal Vector2 dragStartPos = Vector2.zero;
        internal Vector2 dragCurrentPos = Vector2.zero;
        internal Vector2 mouseDownScreenPos = Vector2.zero;
        internal const float DRAG_THRESHOLD = 5f;
        internal Vector2 timelineScrollPosition = Vector2.zero;
        internal MemoryType? filterType = null;
        internal bool showABM = true;
        internal bool showSCM = true;
        internal bool showELS = true;
        internal bool showCLPA = true;
        internal List<MemoryEntry> cachedFilteredMemories;
        internal bool filtersDirty = true;
        internal const float TOP_BAR_HEIGHT = 50f;
        internal const float CONTROL_PANEL_WIDTH = 220f;
        internal const float SPACING = 10f;
        internal const float CARD_WIDTH_FULL = 600f;
        internal const float CARD_SPACING = 8f;
        internal List<MemoryEntry> cachedMemories = new List<MemoryEntry>();
        internal List<float> cachedCardHeights = new List<float>();
        internal List<float> cachedCardYPositions = new List<float>();
        internal float cachedTotalHeight = 0f;
        internal int lastMemoryCount = -1;
        internal bool lastShowABM;
        internal bool lastShowSCM;
        internal bool lastShowELS;
        internal bool lastShowCLPA;
        internal MemoryType? lastFilterType;
        internal Pawn lastSelectedPawn;
        internal int lastRefreshTick = -1;

        public MainTabWindow_Memory()
        {
            Parts = new MemoryTabParts(this);
        }

        public override Vector2 RequestedTabSize => new Vector2(1200f, 700f);

        /// <summary>
        /// 使 UI 缓存失效，强制下次绘制时刷新。
        /// 当外部修改了记忆数据时调用此方法。
        /// </summary>
        public void InvalidateCache()
        {
            lastMemoryCount = -1;
            filtersDirty = true;
        }

        // ==================== Main Layout ====================
        
        public override void DoWindowContents(Rect inRect)
        {
            // Top Bar
            Rect topBarRect = new Rect(0f, 0f, inRect.width, TOP_BAR_HEIGHT);
            Parts.TopBar.DrawTopBar(topBarRect);
            
            // Content area
            float contentY = TOP_BAR_HEIGHT + SPACING;
            float contentHeight = inRect.height - contentY;
            
            if (selectedPawn == null)
            {
                Parts.Utilities.DrawNoPawnSelected(new Rect(0f, contentY, inRect.width, contentHeight));
                return;
            }
            
            var memoryComp = selectedPawn.TryGetComp<FourLayerMemoryComp>();
            if (memoryComp == null)
            {
                Parts.Utilities.DrawNoMemoryComponent(new Rect(0f, contentY, inRect.width, contentHeight));
                return;
            }
            
            currentMemoryComp = memoryComp;
            
            // ? 在绘制任何子组件之前刷新缓存
            Parts.Timeline.CheckAndRefreshCache();
            
            // Left Control Panel
            Rect controlPanelRect = new Rect(0f, contentY, CONTROL_PANEL_WIDTH, contentHeight);
            Parts.Controls.DrawControlPanel(controlPanelRect);
            
            // Right Timeline
            float timelineX = CONTROL_PANEL_WIDTH + SPACING;
            float timelineWidth = inRect.width - timelineX;
            Rect timelineRect = new Rect(timelineX, contentY, timelineWidth, contentHeight);
            Parts.Timeline.DrawTimeline(timelineRect);
            
            // Handle drag end / single click
            if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                if (isMouseDown && !isDragging)
                {
                    // ⭐ 没有进入拖拽模式 → 当作单击处理
                    // 找到鼠标下方的记忆卡片
                    float timelineX2 = CONTROL_PANEL_WIDTH + SPACING;
                    float timelineWidth2 = inRect.width - timelineX2;
                    Rect timelineRect2 = new Rect(timelineX2, contentY, timelineWidth2, contentHeight);
                    Rect innerRect2 = timelineRect2.ContractedBy(5f);
                    
                    if (innerRect2.Contains(Event.current.mousePosition))
                    {
                        float mouseContentY = Event.current.mousePosition.y - innerRect2.y + timelineScrollPosition.y;
                        
                        // 在缓存的 Y 位置中查找点击的卡片
                        MemoryEntry clickedMemory = null;
                        for (int i = 0; i < cachedMemories.Count; i++)
                        {
                            float cardY = cachedCardYPositions[i];
                            float cardH = cachedCardHeights[i];
                            if (mouseContentY >= cardY && mouseContentY < cardY + cardH)
                            {
                                clickedMemory = cachedMemories[i];
                                break;
                            }
                        }
                        
                        if (clickedMemory != null)
                        {
                            Parts.Timeline.HandleMemoryClick(clickedMemory);
                        }
                        else
                        {
                            selectedMemories.Clear();
                            lastSelectedMemory = null;
                        }
                    }
                }
                
                isMouseDown = false;
                isDragging = false;
                Event.current.Use();
            }
        }
    }
}