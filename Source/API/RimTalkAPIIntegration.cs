using Ustas.RimAI.Communication.API;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Prompt;
using System;
using Ustas.RimAI.Core.Handshake;
using Verse;

namespace Ustas.RimAI.Communication.Memory.API
{
    [StaticConstructorOnStartup]
    public static class RimTalkAPIIntegration
    {
        private const string MOD_ID = "Ustas.RimAI.Communication.Memory";
        private const string ENTRY_NAME = "Memory & Knowledge Context";
        private const string CHAT_HISTORY_ENTRY_NAME = "Chat History";
        private const string LEGACY_ENGLISH_MEMORY_CONTENT = @"---
# Memory Context
{{-for p in pawns }}
## {{ p.name }}'s Memories:
{{ p.memory }}
{{- end }}

# World Knowledge:
{{knowledge}}
---";

        private static readonly string[] SniffingKeywords = new string[]
        {
            "pawn.memory", "p.memory",
            "pawn.ABM", "p.ABM",
            "pawn.ELS", "p.ELS",
            "pawn.CLPA", "p.CLPA",
            "{{knowledge}}", "{{ knowledge }}",
            "knowledge_grouped", "knowledge_rules",
            "knowledge_lore", "knowledge_status",
            "knowledge_history"
        };

        private static bool _initialized;
        private static bool _apiAvailable;

        public static bool IsUsingNewAPI => _apiAvailable;

        static RimTalkAPIIntegration()
        {
            LongEventHandler.ExecuteWhenFinished(Initialize);
        }

        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            if (!RimAiHandshake.IsApproved(RimAiModuleIds.Memory))
            {
                return;
            }

            try
            {
                _apiAvailable = true;
                RegisterVariables();
                RegisterPromptEntry();
                Log.Message("[MemoryPatch] ✓ Integrated via typed Communication prompt API");
            }
            catch (Exception ex)
            {
                Log.Error($"[MemoryPatch] API integration failed: {ex.Message}");
                _apiAvailable = false;
            }
        }

        private static void RegisterVariables()
        {
            RimTalkPromptAPI.RegisterPawnVariable(MOD_ID, "memory", MemoryVariableProvider.GetPawnMemory,
                "Особисті спогади й досвід персонажа", 100);
            RimTalkPromptAPI.RegisterPawnVariable(MOD_ID, "ABM", MemoryVariableProvider.GetPawnABM,
                "Активний буфер пам'яті персонажа (недавні розмови)", 100);
            RimTalkPromptAPI.RegisterPawnVariable(MOD_ID, "ELS", MemoryVariableProvider.GetPawnELS,
                "Зведення журналу подій персонажа (середньострокова пам'ять)", 100);
            RimTalkPromptAPI.RegisterPawnVariable(MOD_ID, "CLPA", MemoryVariableProvider.GetPawnCLPA,
                "Архів особистості персонажа (довгострокова пам'ять)", 100);
            RimTalkPromptAPI.RegisterPawnVariable(MOD_ID, "matchELS", MemoryVariableProvider.GetPawnMatchELS,
                "Спогади журналу подій, дібрані за контекстом (середньострокові)", 100);
            RimTalkPromptAPI.RegisterPawnVariable(MOD_ID, "matchCLPA", MemoryVariableProvider.GetPawnMatchCLPA,
                "Архівні спогади, дібрані за контекстом (довгострокові)", 100);

            RegisterContext("knowledge", KnowledgeVariableProvider.GetMatchedKnowledge,
                "Знання про світ, дібрані за контекстом розмови", 100);
            RegisterContext("knowledge_grouped", KnowledgeVariableProvider.GetGroupedKnowledge,
                "Знання про світ, згруповані за заголовками категорій", 101);
            RegisterContext("knowledge_rules", KnowledgeVariableProvider.GetKnowledgeRules,
                "Знання: правила та інструкції", 102);
            RegisterContext("knowledge_lore", KnowledgeVariableProvider.GetKnowledgeLore,
                "Знання: устрій і передісторія світу", 103);
            RegisterContext("knowledge_status", KnowledgeVariableProvider.GetKnowledgeStatus,
                "Знання: стан персонажа/колоніста", 104);
            RegisterContext("knowledge_history", KnowledgeVariableProvider.GetKnowledgeHistory,
                "Знання: історичні події", 105);
            RegisterContext("knowledge_other", KnowledgeVariableProvider.GetKnowledgeOther,
                "Знання: без категорії", 106);
        }

        private static void RegisterContext(string name, Func<object, string> provider, string description, int priority)
        {
            RimTalkPromptAPI.RegisterContextVariable(MOD_ID, name, ctx => provider(ctx) ?? string.Empty, description, priority);
        }

        private static void RegisterPromptEntry()
        {
            string entryId = PromptEntry.GenerateDeterministicId(MOD_ID, ENTRY_NAME);
            var preset = RimTalkPromptAPI.GetActivePreset();
            if (preset != null)
            {
                var existingEntry = preset.GetEntry(entryId);
                if (existingEntry != null)
                {
                    if (NormalizePrompt(existingEntry.Content) == NormalizePrompt(LEGACY_ENGLISH_MEMORY_CONTENT))
                    {
                        existingEntry.Content = GetMemoryEntryContent();
                        Log.Message($"[MemoryPatch] ✓ Migrated unchanged PromptEntry: {ENTRY_NAME}");
                    }
                    else if (NormalizePrompt(existingEntry.Content) != NormalizePrompt(GetMemoryEntryContent()))
                    {
                        Log.Message($"[MemoryPatch] Preserved customized PromptEntry: {ENTRY_NAME}");
                    }

                    DisableChatHistoryIfEnabled(preset);
                    return;
                }

                if (preset.Entries != null)
                {
                    foreach (var currentEntry in preset.Entries)
                    {
                        string content = currentEntry.Content;
                        if (string.IsNullOrEmpty(content)) continue;
                        foreach (var keyword in SniffingKeywords)
                        {
                            if (!content.Contains(keyword)) continue;
                            Log.Message($"[MemoryPatch] Detected custom memory variable '{keyword}' in active preset. Skipping auto-injection.");
                            return;
                        }
                    }
                }
            }

            var entry = new PromptEntry
            {
                SourceModId = MOD_ID,
                Name = ENTRY_NAME,
                Content = GetMemoryEntryContent(),
                Enabled = true,
                Role = PromptRole.System,
                Position = PromptPosition.Relative
            };
            RimTalkPromptAPI.InsertPromptEntryAfterName(entry, CHAT_HISTORY_ENTRY_NAME);
            DisableChatHistoryIfEnabled(preset);
        }

        private static void DisableChatHistoryIfEnabled(PromptPreset preset)
        {
            if (preset == null) return;
            var chatHistoryId = preset.FindEntryIdByName(CHAT_HISTORY_ENTRY_NAME);
            if (string.IsNullOrEmpty(chatHistoryId)) return;
            var chatHistoryEntry = preset.GetEntry(chatHistoryId);
            if (chatHistoryEntry == null || !chatHistoryEntry.Enabled) return;
            chatHistoryEntry.Enabled = false;
            Log.Message($"[MemoryPatch] ✓ Disabled '{CHAT_HISTORY_ENTRY_NAME}' to avoid conflict with Memory & Knowledge injection");
        }

        private static string GetMemoryEntryContent()
        {
            return @"---
# Контекст пам'яті
{{-for p in pawns }}
## Спогади: {{ p.name }}
{{ p.memory }}
{{- end }}

# Знання про світ:
{{knowledge}}
---";
        }

        private static string NormalizePrompt(string value)
        {
            return (value ?? string.Empty).Replace("\r\n", "\n").Trim();
        }

        public static void Cleanup()
        {
            if (!_apiAvailable) return;
            try
            {
                RimTalkPromptAPI.RemovePromptEntry(PromptEntry.GenerateDeterministicId(MOD_ID, ENTRY_NAME));
                RimTalkPromptAPI.UnregisterAllHooks(MOD_ID);
                Log.Message("[MemoryPatch] Cleaned up Communication API registrations");
            }
            catch (Exception ex)
            {
                Log.Warning($"[MemoryPatch] Cleanup failed: {ex.Message}");
            }
        }
    }
}
