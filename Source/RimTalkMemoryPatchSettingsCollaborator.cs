using UnityEngine;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ustas.RimAI.Communication.Memory
{
    internal abstract class RimTalkMemoryPatchSettingsCollaborator
    {
        internal readonly RimTalkMemoryPatchSettings Owner;

        protected RimTalkMemoryPatchSettingsCollaborator(RimTalkMemoryPatchSettings owner)
        {
            Owner = owner;
        }

        protected RimTalkMemoryPatchSettingsParts Parts => Owner.Parts;

        protected List<RimTalkMemoryPatchSettings.ReplacementRule> normalizationRules
        {
            get => Owner.normalizationRules;
            set => Owner.normalizationRules = value;
        }
        protected int maxActiveMemories
        {
            get => Owner.maxActiveMemories;
            set => Owner.maxActiveMemories = value;
        }
        protected int maxSituationalMemories
        {
            get => Owner.maxSituationalMemories;
            set => Owner.maxSituationalMemories = value;
        }
        protected int maxEventLogMemories
        {
            get => Owner.maxEventLogMemories;
            set => Owner.maxEventLogMemories = value;
        }
        protected bool IsPlayerDialogueInject
        {
            get => Owner.IsPlayerDialogueInject;
            set => Owner.IsPlayerDialogueInject = value;
        }
        protected bool IsRoundMemoryActive
        {
            get => Owner.IsRoundMemoryActive;
            set => Owner.IsRoundMemoryActive = value;
        }
        protected int maxABMInjectionRounds
        {
            get => Owner.maxABMInjectionRounds;
            set => Owner.maxABMInjectionRounds = value;
        }
        protected float scmDecayRate
        {
            get => Owner.scmDecayRate;
            set => Owner.scmDecayRate = value;
        }
        protected float elsDecayRate
        {
            get => Owner.elsDecayRate;
            set => Owner.elsDecayRate = value;
        }
        protected float clpaDecayRate
        {
            get => Owner.clpaDecayRate;
            set => Owner.clpaDecayRate = value;
        }
        protected bool enableDailySummarization
        {
            get => Owner.enableDailySummarization;
            set => Owner.enableDailySummarization = value;
        }
        protected int summarizationHour
        {
            get => Owner.summarizationHour;
            set => Owner.summarizationHour = value;
        }
        protected bool useAISummarization
        {
            get => Owner.useAISummarization;
            set => Owner.useAISummarization = value;
        }
        protected int maxSummaryLength
        {
            get => Owner.maxSummaryLength;
            set => Owner.maxSummaryLength = value;
        }
        protected bool enableAutoArchive
        {
            get => Owner.enableAutoArchive;
            set => Owner.enableAutoArchive = value;
        }
        protected int archiveIntervalDays
        {
            get => Owner.archiveIntervalDays;
            set => Owner.archiveIntervalDays = value;
        }
        protected int maxArchiveMemories
        {
            get => Owner.maxArchiveMemories;
            set => Owner.maxArchiveMemories = value;
        }
        protected bool useRimTalkAIConfig
        {
            get => Owner.useRimTalkAIConfig;
            set => Owner.useRimTalkAIConfig = value;
        }
        protected string independentApiKey
        {
            get => Owner.independentApiKey;
            set => Owner.independentApiKey = value;
        }
        protected string independentApiUrl
        {
            get => Owner.independentApiUrl;
            set => Owner.independentApiUrl = value;
        }
        protected string independentModel
        {
            get => Owner.independentModel;
            set => Owner.independentModel = value;
        }
        protected string independentProvider
        {
            get => Owner.independentProvider;
            set => Owner.independentProvider = value;
        }
        protected bool enablePromptCaching
        {
            get => Owner.enablePromptCaching;
            set => Owner.enablePromptCaching = value;
        }
        protected string dailySummaryPrompt
        {
            get => Owner.dailySummaryPrompt;
            set => Owner.dailySummaryPrompt = value;
        }
        protected string deepArchivePrompt
        {
            get => Owner.deepArchivePrompt;
            set => Owner.deepArchivePrompt = value;
        }
        protected int summaryMaxTokens
        {
            get => Owner.summaryMaxTokens;
            set => Owner.summaryMaxTokens = value;
        }
        protected bool enableMemoryUI
        {
            get => Owner.enableMemoryUI;
            set => Owner.enableMemoryUI = value;
        }
        protected bool enableActionMemory
        {
            get => Owner.enableActionMemory;
            set => Owner.enableActionMemory = value;
        }
        protected bool enableConversationMemory
        {
            get => Owner.enableConversationMemory;
            set => Owner.enableConversationMemory = value;
        }
        protected bool enablePawnStatusKnowledge
        {
            get => enablePawnStatusKnowledge;
            set => enablePawnStatusKnowledge = value;
        }
        protected bool enableEventRecordKnowledge
        {
            get => enableEventRecordKnowledge;
            set => enableEventRecordKnowledge = value;
        }
        protected bool enableConversationCache
        {
            get => Owner.enableConversationCache;
            set => Owner.enableConversationCache = value;
        }
        protected int conversationCacheSize
        {
            get => Owner.conversationCacheSize;
            set => Owner.conversationCacheSize = value;
        }
        protected int conversationCacheExpireDays
        {
            get => Owner.conversationCacheExpireDays;
            set => Owner.conversationCacheExpireDays = value;
        }
        protected bool enablePromptCache
        {
            get => Owner.enablePromptCache;
            set => Owner.enablePromptCache = value;
        }
        protected int promptCacheSize
        {
            get => Owner.promptCacheSize;
            set => Owner.promptCacheSize = value;
        }
        protected int promptCacheExpireMinutes
        {
            get => Owner.promptCacheExpireMinutes;
            set => Owner.promptCacheExpireMinutes = value;
        }
        protected bool useDynamicInjection
        {
            get => Owner.useDynamicInjection;
            set => Owner.useDynamicInjection = value;
        }
        protected int maxInjectedMemories
        {
            get => Owner.maxInjectedMemories;
            set => Owner.maxInjectedMemories = value;
        }
        protected int maxInjectedKnowledge
        {
            get => Owner.maxInjectedKnowledge;
            set => Owner.maxInjectedKnowledge = value;
        }
        protected float weightTimeDecay
        {
            get => Owner.weightTimeDecay;
            set => Owner.weightTimeDecay = value;
        }
        protected float weightImportance
        {
            get => Owner.weightImportance;
            set => Owner.weightImportance = value;
        }
        protected float weightKeywordMatch
        {
            get => Owner.weightKeywordMatch;
            set => Owner.weightKeywordMatch = value;
        }
        protected float memoryScoreThreshold
        {
            get => Owner.memoryScoreThreshold;
            set => Owner.memoryScoreThreshold = value;
        }
        protected float knowledgeScoreThreshold
        {
            get => Owner.knowledgeScoreThreshold;
            set => Owner.knowledgeScoreThreshold = value;
        }
        protected bool enableAdaptiveThreshold
        {
            get => Owner.enableAdaptiveThreshold;
            set => Owner.enableAdaptiveThreshold = value;
        }
        protected bool autoApplyAdaptiveThreshold
        {
            get => Owner.autoApplyAdaptiveThreshold;
            set => Owner.autoApplyAdaptiveThreshold = value;
        }
        protected bool enableProactiveRecall
        {
            get => Owner.enableProactiveRecall;
            set => Owner.enableProactiveRecall = value;
        }
        protected float recallTriggerChance
        {
            get => Owner.recallTriggerChance;
            set => Owner.recallTriggerChance = value;
        }
        protected bool enableVectorEnhancement
        {
            get => Owner.enableVectorEnhancement;
            set => Owner.enableVectorEnhancement = value;
        }
        protected float vectorSimilarityThreshold
        {
            get => Owner.vectorSimilarityThreshold;
            set => Owner.vectorSimilarityThreshold = value;
        }
        protected int maxVectorResults
        {
            get => Owner.maxVectorResults;
            set => Owner.maxVectorResults = value;
        }
        protected string embeddingApiKey
        {
            get => Owner.embeddingApiKey;
            set => Owner.embeddingApiKey = value;
        }
        protected string embeddingApiUrl
        {
            get => Owner.embeddingApiUrl;
            set => Owner.embeddingApiUrl = value;
        }
        protected string embeddingModel
        {
            get => Owner.embeddingModel;
            set => Owner.embeddingModel = value;
        }
        protected bool enableKnowledgeChaining
        {
            get => Owner.enableKnowledgeChaining;
            set => Owner.enableKnowledgeChaining = value;
        }
        protected int maxChainingRounds
        {
            get => Owner.maxChainingRounds;
            set => Owner.maxChainingRounds = value;
        }
        protected List<string> knowledgeMatchingSources
        {
            get => Owner.knowledgeMatchingSources;
            set => Owner.knowledgeMatchingSources = value;
        }
        protected bool expandDynamicInjection
        {
            get => RimTalkMemoryPatchSettings.expandDynamicInjection;
            set => RimTalkMemoryPatchSettings.expandDynamicInjection = value;
        }
        protected bool expandMemoryCapacity
        {
            get => RimTalkMemoryPatchSettings.expandMemoryCapacity;
            set => RimTalkMemoryPatchSettings.expandMemoryCapacity = value;
        }
        protected bool expandDecayRates
        {
            get => RimTalkMemoryPatchSettings.expandDecayRates;
            set => RimTalkMemoryPatchSettings.expandDecayRates = value;
        }
        protected bool expandSummarization
        {
            get => RimTalkMemoryPatchSettings.expandSummarization;
            set => RimTalkMemoryPatchSettings.expandSummarization = value;
        }
        protected bool expandAIConfig
        {
            get => RimTalkMemoryPatchSettings.expandAIConfig;
            set => RimTalkMemoryPatchSettings.expandAIConfig = value;
        }
        protected bool expandMemoryTypes
        {
            get => RimTalkMemoryPatchSettings.expandMemoryTypes;
            set => RimTalkMemoryPatchSettings.expandMemoryTypes = value;
        }
        protected bool expandVectorEnhancement
        {
            get => RimTalkMemoryPatchSettings.expandVectorEnhancement;
            set => RimTalkMemoryPatchSettings.expandVectorEnhancement = value;
        }
        protected bool expandExperimentalFeatures
        {
            get => RimTalkMemoryPatchSettings.expandExperimentalFeatures;
            set => RimTalkMemoryPatchSettings.expandExperimentalFeatures = value;
        }
        protected Vector2 scrollPosition
        {
            get => RimTalkMemoryPatchSettings.scrollPosition;
            set => RimTalkMemoryPatchSettings.scrollPosition = value;
        }
    }
}
