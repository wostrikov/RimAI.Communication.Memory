using Ustas.RimAI.Communication.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Ustas.RimAI.Core.Diagnostics;

namespace Ustas.RimAI.Communication.Memory
{
    public static class PromptNormalizer
    {
        private static volatile NormalizerSnapshot _snapshot = new NormalizerSnapshot(
            new List<RimTalkMemoryPatchSettings.ReplacementRule>(),
            new Dictionary<string, Regex>());

        private sealed class NormalizerSnapshot
        {
            public readonly List<RimTalkMemoryPatchSettings.ReplacementRule> Rules;
            public readonly Dictionary<string, Regex> Cache;

            public NormalizerSnapshot(
                List<RimTalkMemoryPatchSettings.ReplacementRule> rules,
                Dictionary<string, Regex> cache)
            {
                Rules = rules;
                Cache = cache;
            }
        }

        // Threading/concurrency constraint — do not race this state. (summary summary)
        public static void UpdateRules(List<RimTalkMemoryPatchSettings.ReplacementRule> rules)
        {
            var newRules = rules == null
                ? new List<RimTalkMemoryPatchSettings.ReplacementRule>()
                : rules.Where(r => r != null && r.isEnabled).ToList();

            var newCache = new Dictionary<string, Regex>();
            foreach (var rule in newRules)
            {
                if (string.IsNullOrEmpty(rule.pattern))
                    continue;
                try
                {
                    var regex = new Regex(rule.pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    newCache[rule.pattern] = regex;
                }
                catch (ArgumentException ex)
                {
                    RimAiLog.Warning(RimAiLogCategory.Memory, $"[PromptNormalizer] Invalid regex pattern '{rule.pattern}'.", exception: ex);
                }
            }

            _snapshot = new NormalizerSnapshot(newRules, newCache);
        }

        // Threading/concurrency constraint — do not race this state. (summary summary)
        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Threading/concurrency constraint — do not race this state. (UpdateRules)
            var snap = _snapshot;
            if (snap.Rules.Count == 0)
                return text;

            string result = text;
            foreach (var rule in snap.Rules)
            {
                if (string.IsNullOrEmpty(rule.pattern) || rule.replacement == null)
                    continue;
                try
                {
                    if (snap.Cache.TryGetValue(rule.pattern, out var regex))
                    {
                        result = regex.Replace(result, rule.replacement);
                    }
                }
                catch (RegexMatchTimeoutException ex)
                {
                    RimAiLog.Warning(RimAiLogCategory.Memory, $"[PromptNormalizer] Error applying rule '{rule.pattern}'.", exception: ex);
                }
                catch (ArgumentException ex)
                {
                    RimAiLog.Warning(RimAiLogCategory.Memory, $"[PromptNormalizer] Error applying rule '{rule.pattern}'.", exception: ex);
                }
            }
            return result;
        }

        public static int GetActiveRuleCount()
        {
            return _snapshot.Rules.Count;
        }
    }
}
