﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory.UI
{
    internal abstract class Dialog_CommonKnowledgeCollaborator
    {
        internal readonly Dialog_CommonKnowledge Owner;

        protected Dialog_CommonKnowledgeCollaborator(Dialog_CommonKnowledge owner)
        {
            Owner = owner;
        }

        protected Dialog_CommonKnowledgeParts Parts => Owner.Parts;

        protected void Close(bool doCloseSound = true) => Owner.Close(doCloseSound);
        protected bool absorbInputAroundWindow
        {
            get => Owner.absorbInputAroundWindow;
            set => Owner.absorbInputAroundWindow = value;
        }

        protected CommonKnowledgeLibrary library
        {
            get => Owner.library;
            set => Owner.library = value;
        }
        protected HashSet<CommonKnowledgeEntry> selectedEntries
        {
            get => Owner.selectedEntries;
            set => Owner.selectedEntries = value;
        }
        protected CommonKnowledgeEntry lastSelectedEntry
        {
            get => Owner.lastSelectedEntry;
            set => Owner.lastSelectedEntry = value;
        }
        protected bool isDragging
        {
            get => Owner.isDragging;
            set => Owner.isDragging = value;
        }
        protected bool isMouseDown
        {
            get => Owner.isMouseDown;
            set => Owner.isMouseDown = value;
        }
        protected Vector2 dragStartPos
        {
            get => Owner.dragStartPos;
            set => Owner.dragStartPos = value;
        }
        protected Vector2 dragCurrentPos
        {
            get => Owner.dragCurrentPos;
            set => Owner.dragCurrentPos = value;
        }
        protected Vector2 mouseDownScreenPos
        {
            get => Owner.mouseDownScreenPos;
            set => Owner.mouseDownScreenPos = value;
        }
        protected float DRAG_THRESHOLD => Dialog_CommonKnowledge.DRAG_THRESHOLD;
        protected bool isDragReordering
        {
            get => Owner.isDragReordering;
            set => Owner.isDragReordering = value;
        }
        protected int dragReorderInsertIndex
        {
            get => Owner.dragReorderInsertIndex;
            set => Owner.dragReorderInsertIndex = value;
        }
        protected bool mouseDownOnSelected
        {
            get => Owner.mouseDownOnSelected;
            set => Owner.mouseDownOnSelected = value;
        }
        protected Vector2 listScrollPosition
        {
            get => Owner.listScrollPosition;
            set => Owner.listScrollPosition = value;
        }
        protected Vector2 detailScrollPosition
        {
            get => Owner.detailScrollPosition;
            set => Owner.detailScrollPosition = value;
        }
        protected string searchFilter
        {
            get => Owner.searchFilter;
            set => Owner.searchFilter = value;
        }
        protected bool editMode
        {
            get => Owner.editMode;
            set => Owner.editMode = value;
        }
        protected bool showRightPanel
        {
            get => Owner.showRightPanel;
            set => Owner.showRightPanel = value;
        }
        protected KnowledgeCategory currentCategory
        {
            get => Owner.currentCategory;
            set => Owner.currentCategory = value;
        }
        protected bool showAutoGenerateSettings
        {
            get => Owner.showAutoGenerateSettings;
            set => Owner.showAutoGenerateSettings = value;
        }
        protected VirtualListView<CommonKnowledgeEntry> virtualList
        {
            get => Owner.virtualList;
            set => Owner.virtualList = value;
        }
        protected string editTag
        {
            get => Owner.editTag;
            set => Owner.editTag = value;
        }
        protected string editContent
        {
            get => Owner.editContent;
            set => Owner.editContent = value;
        }
        protected float editImportance
        {
            get => Owner.editImportance;
            set => Owner.editImportance = value;
        }
        protected int editTargetPawnId
        {
            get => Owner.editTargetPawnId;
            set => Owner.editTargetPawnId = value;
        }
        protected KeywordMatchMode editMatchMode
        {
            get => Owner.editMatchMode;
            set => Owner.editMatchMode = value;
        }
        protected KnowledgeEntryCategory editCategory
        {
            get => Owner.editCategory;
            set => Owner.editCategory = value;
        }
        protected float TOOLBAR_HEIGHT => Dialog_CommonKnowledge.TOOLBAR_HEIGHT;
        protected float SIDEBAR_WIDTH => Dialog_CommonKnowledge.SIDEBAR_WIDTH;
        protected float RIGHT_PANEL_WIDTH => Dialog_CommonKnowledge.RIGHT_PANEL_WIDTH;
        protected float SPACING => Dialog_CommonKnowledge.SPACING;
        protected float BUTTON_HEIGHT => Dialog_CommonKnowledge.BUTTON_HEIGHT;
        protected float ENTRY_HEIGHT => Dialog_CommonKnowledge.ENTRY_HEIGHT;
    }
}
