using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Ustas.RimAI.Communication.Data;
using Ustas.RimAI.Communication.Memory.API;
using Ustas.RimAI.Communication.Memory.VectorDB;
using Verse;
using Ustas.RimAI.Communication.Memory.Diagnostics;

namespace Ustas.RimAI.Communication.Memory.Integration;

/// <summary>
/// Background-thread vector lore injection for Communication talk requests.
/// Replaces the sibling Harmony prefix on TalkService.GenerateAndProcessTalkAsync.
/// </summary>
public static class VectorTalkEnricher
{
    const string NoMatchMarker = "(No matching knowledge found)";

    public static void Enrich(TalkRequest talkRequest)
    {
        if (talkRequest == null)
            return;
        try
        {
            var settings = RimTalkMemoryPatchMod.Settings;
            if (settings == null || !settings.enableVectorEnhancement)
                return;

            var knowledgeContext = KnowledgeVariableProvider.GetLastContext();
            if (knowledgeContext == null)
                return;

            string vectorSearchText = knowledgeContext.DialogueType;
            string matchText = knowledgeContext.MatchText;
            Pawn initiator = knowledgeContext.Speaker;
            Pawn recipient = knowledgeContext.Listener;
            string keywordKnowledge = knowledgeContext.KeywordKnowledge;
            if (string.IsNullOrEmpty(vectorSearchText))
                return;

            string vectorContent = BuildVectorContent(vectorSearchText, matchText, initiator, recipient, settings);
            if (string.IsNullOrEmpty(vectorContent))
                return;

            if (!TryInjectContext(talkRequest, vectorContent, keywordKnowledge))
                TryInjectPromptMessages(talkRequest, vectorContent, keywordKnowledge);

            KnowledgeVariableProvider.ClearContext();
        }
        catch (Exception ex)
        {
            Log.Error($"[RimAI.Memory] Vector talk enrichment failed: {ex}");
        }
    }

    static string BuildVectorContent(string vectorSearchText, string matchText, Pawn initiator, Pawn recipient, RimTalkMemoryPatchSettings settings)
    {
        try
        {
            var vectorResults = VectorService.Instance.FindBestLoreIdsAsync(
                vectorSearchText,
                settings.maxVectorResults,
                settings.vectorSimilarityThreshold).Result;
            if (vectorResults == null || vectorResults.Count == 0)
                return null;

            var memoryManager = Find.World?.GetComponent<MemoryManager>();
            if (memoryManager == null)
                return null;

            var keywordMatchedIds = new HashSet<string>();
            try
            {
                memoryManager.CommonKnowledge.InjectKnowledgeWithDetails(
                    matchText,
                    settings.maxVectorResults,
                    out var keywordScores,
                    initiator,
                    recipient);
                if (keywordScores != null)
                {
                    foreach (var score in keywordScores)
                        keywordMatchedIds.Add(score.Entry.id);
                }
            }
            // RimAI.catch-boundary: ALLOWED_TOP_LEVEL_BOUNDARY - keyword scoring was skipped for this enrichment
            catch (System.Exception ex)
            {
                ModuleLog.Message("[RimAI.Memory] keyword scoring was skipped for this enrichment: " + ex.Message);
            }

            var entriesSnapshot = memoryManager.CommonKnowledge.Entries.ToList();
            var scoredResults = new List<(CommonKnowledgeEntry Entry, float Similarity, float Score)>();
            foreach (var (id, similarity) in vectorResults)
            {
                if (keywordMatchedIds.Contains(id))
                    continue;
                var entry = entriesSnapshot.FirstOrDefault(e => e.id == id);
                if (entry == null)
                    continue;
                scoredResults.Add((entry, similarity, similarity + entry.importance * 0.2f));
            }

            var finalResults = scoredResults.OrderByDescending(x => x.Score).ToList();
            if (finalResults.Count == 0)
                return null;

            var vectorSb = new StringBuilder();
            vectorSb.AppendLine();
            vectorSb.AppendLine("#### Vector Enhanced:");
            int index = 1;
            foreach (var item in finalResults)
            {
                vectorSb.AppendLine($"{index}. [{item.Entry.tag}] {item.Entry.content}");
                index++;
            }

            var similarities = string.Join(", ", finalResults.Select(r => $"{r.Entry.tag}:{r.Similarity:F3}"));
            ModuleLog.Message($"[RimAI.Memory] Vector matched {finalResults.Count} knowledge entries. Similarities: {similarities}");
            return vectorSb.ToString();
        }
        catch (Exception ex)
        {
            Log.Warning($"[RimAI.Memory] Vector search error: {ex.Message}");
            return null;
        }
    }

    static bool TryInjectContext(TalkRequest talkRequest, string vectorContent, string keywordKnowledge)
    {
        string context = talkRequest.Context;
        if (string.IsNullOrEmpty(context))
            return false;
        if (context.Contains(NoMatchMarker))
        {
            talkRequest.Context = context.Replace(NoMatchMarker, NoMatchMarker + vectorContent);
            return true;
        }

        if (!string.IsNullOrEmpty(keywordKnowledge) && context.Contains(keywordKnowledge))
        {
            int idx = context.IndexOf(keywordKnowledge, StringComparison.Ordinal) + keywordKnowledge.Length;
            talkRequest.Context = context.Insert(idx, vectorContent);
            return true;
        }

        return false;
    }

    static void TryInjectPromptMessages(TalkRequest talkRequest, string vectorContent, string keywordKnowledge)
    {
        var messages = talkRequest.PromptMessages;
        if (messages == null || messages.Count == 0)
            return;

        string searchText = !string.IsNullOrEmpty(keywordKnowledge) && keywordKnowledge.Length > 10
            ? keywordKnowledge.Substring(0, Math.Min(50, keywordKnowledge.Length))
            : null;

        int targetIndex = -1;
        bool foundKeywordMatch = false;
        for (int i = 0; i < messages.Count; i++)
        {
            string content = messages[i].content ?? string.Empty;
            if (!string.IsNullOrEmpty(searchText) && content.Contains(searchText))
            {
                targetIndex = i;
                foundKeywordMatch = true;
                break;
            }

            if (content.Contains(NoMatchMarker))
                targetIndex = i;
        }

        if (foundKeywordMatch && targetIndex >= 0 && !string.IsNullOrEmpty(keywordKnowledge))
        {
            string targetContent = messages[targetIndex].content ?? string.Empty;
            int knowledgeEndIndex = targetContent.IndexOf(keywordKnowledge, StringComparison.Ordinal) + keywordKnowledge.Length;
            if (knowledgeEndIndex > 0 && knowledgeEndIndex <= targetContent.Length)
            {
                messages[targetIndex] = (messages[targetIndex].role, targetContent.Insert(knowledgeEndIndex, vectorContent));
                return;
            }
        }

        if (targetIndex >= 0)
        {
            string targetContent = messages[targetIndex].content ?? string.Empty;
            messages[targetIndex] = (messages[targetIndex].role, targetContent.Replace(NoMatchMarker, NoMatchMarker + vectorContent));
            return;
        }

        int last = messages.Count - 1;
        messages[last] = (messages[last].role, (messages[last].content ?? string.Empty) + "\n" + vectorContent);
    }
}
