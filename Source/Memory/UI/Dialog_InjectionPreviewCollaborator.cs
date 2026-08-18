
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
    internal abstract class Dialog_InjectionPreviewCollaborator
    {
        internal readonly Dialog_InjectionPreview Owner;

        protected Dialog_InjectionPreviewCollaborator(Dialog_InjectionPreview owner)
        {
            Owner = owner;
        }

        protected Dialog_InjectionPreviewParts Parts => Owner.Parts;
        protected List<(string name, string description, bool isPawnProperty)> availableMatchingSources
        {
            get => Owner.availableMatchingSources;
            set => Owner.availableMatchingSources = value;
        }


        protected void Close(bool doCloseSound = true) => Owner.Close(doCloseSound);
        protected bool absorbInputAroundWindow
        {
            get => Owner.absorbInputAroundWindow;
            set => Owner.absorbInputAroundWindow = value;
        }

        protected Pawn selectedPawn
        {
            get => Owner.selectedPawn;
            set => Owner.selectedPawn = value;
        }
        protected Pawn targetPawn
        {
            get => Owner.targetPawn;
            set => Owner.targetPawn = value;
        }
        protected Vector2 scrollPositionLeft
        {
            get => scrollPositionLeft;
            set => scrollPositionLeft = value;
        }
        protected Vector2 scrollPositionRight
        {
            get => scrollPositionRight;
            set => scrollPositionRight = value;
        }
        protected Vector2 scrollPositionMatchSource
        {
            get => scrollPositionMatchSource;
            set => scrollPositionMatchSource = value;
        }
        protected Vector2 scrollPositionParsedText
        {
            get => scrollPositionParsedText;
            set => scrollPositionParsedText = value;
        }
        protected string parsedMatchText
        {
            get => Owner.parsedMatchText;
            set => Owner.parsedMatchText = value;
        }
        protected List<KnowledgeScore> matchedKnowledge
        {
            get => Owner.matchedKnowledge;
            set => Owner.matchedKnowledge = value;
        }
        protected string memoryPreviewText
        {
            get => Owner.memoryPreviewText;
            set => Owner.memoryPreviewText = value;
        }
        protected int cachedMemoryCount
        {
            get => Owner.cachedMemoryCount;
            set => Owner.cachedMemoryCount = value;
        }
        protected bool showMatchSourcePanel
        {
            get => Owner.showMatchSourcePanel;
            set => Owner.showMatchSourcePanel = value;
        }
        protected bool showKnowledgePanel
        {
            get => Owner.showKnowledgePanel;
            set => Owner.showKnowledgePanel = value;
        }
        protected bool showMemoryPanel
        {
            get => Owner.showMemoryPanel;
            set => Owner.showMemoryPanel = value;
        }
    }
}
