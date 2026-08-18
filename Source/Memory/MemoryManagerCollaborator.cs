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
    internal abstract class MemoryManagerCollaborator
    {
        internal readonly MemoryManager Owner;

        protected MemoryManagerCollaborator(MemoryManager owner)
        {
            Owner = owner;
        }

        protected MemoryManagerParts Parts => Owner.Parts;

        protected int lastDecayTick
        {
            get => Owner.lastDecayTick;
            set => Owner.lastDecayTick = value;
        }
        protected int DecayInterval => MemoryManager.DecayInterval;
        protected int lastSummarizationDay
        {
            get => Owner.lastSummarizationDay;
            set => Owner.lastSummarizationDay = value;
        }
        protected int lastArchiveDay
        {
            get => Owner.lastArchiveDay;
            set => Owner.lastArchiveDay = value;
        }
        protected int sessionStartTick
        {
            get => Owner.sessionStartTick;
            set => Owner.sessionStartTick = value;
        }
        protected int COLD_START_DELAY => MemoryManager.COLD_START_DELAY;
        protected Queue<Pawn> summarizationQueue
        {
            get => Owner.summarizationQueue;
            set => Owner.summarizationQueue = value;
        }
        protected int nextSummarizationTick
        {
            get => Owner.nextSummarizationTick;
            set => Owner.nextSummarizationTick = value;
        }
        protected int SUMMARIZATION_DELAY_TICKS => MemoryManager.SUMMARIZATION_DELAY_TICKS;
        protected Queue<Pawn> manualSummarizationQueue
        {
            get => Owner.manualSummarizationQueue;
            set => Owner.manualSummarizationQueue = value;
        }
        protected int nextManualSummarizationTick
        {
            get => Owner.nextManualSummarizationTick;
            set => Owner.nextManualSummarizationTick = value;
        }
        protected int MANUAL_SUMMARIZATION_DELAY_TICKS => MemoryManager.MANUAL_SUMMARIZATION_DELAY_TICKS;
        protected CommonKnowledgeLibrary commonKnowledge
        {
            get => Owner.commonKnowledge;
            set => Owner.commonKnowledge = value;
        }
        protected ConversationCache conversationCache
        {
            get => Owner.conversationCache;
            set => Owner.conversationCache = value;
        }
        protected PromptCache promptCache
        {
            get => Owner.promptCache;
            set => Owner.promptCache = value;
        }
    }
}
