using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.AI;
using Ustas.RimAI.Communication.Memory.Diagnostics;
using RimAI.Core.Runtime;
using Ustas.RimAI.Core.Diagnostics;
using Verse;

namespace Ustas.RimAI.Communication.Memory.Colony
{
    /// <summary>
    /// One paragraph per map about how the colony is doing, kept current.
    ///
    /// The module's memory is all pawn-shaped: what somebody remembers, who
    /// they are, what they think of each other. A colonist could therefore
    /// discuss last week's quarrel in perfect detail while a raid burned the
    /// kitchen, because nothing in the context was ever about the place. This
    /// fills that in: the measured facts go to the same model the rest of the
    /// module uses, and what comes back is offered to prompts as {{colony}}.
    ///
    /// Two things keep it cheap. It is asked for on a timer rather than per
    /// line - a colony does not change between two sentences - and only when
    /// the snapshot's fingerprint has actually moved, so a paused game or a
    /// quiet afternoon costs nothing at all. The text survives a save, so a
    /// reload starts with the last answer rather than a blank.
    /// </summary>
    public class ColonyTrendManager : GameComponent
    {
        private const int TicksPerCheck = 2500;

        private Dictionary<int, string> _trendByMap = new Dictionary<int, string>();
        private Dictionary<int, string> _fingerprintByMap = new Dictionary<int, string>();
        private Dictionary<int, int> _askedAtByMap = new Dictionary<int, int>();

        private readonly HashSet<int> _inFlight = new HashSet<int>();

        private List<int> _scribeKeys;
        private List<string> _scribeTrends;
        private List<string> _scribeFingerprints;

        public ColonyTrendManager(Game game)
        {
        }

        public static ColonyTrendManager Current => Verse.Current.Game?.GetComponent<ColonyTrendManager>();

        /// <summary>The paragraph for a map, or empty while none has been made.</summary>
        public string TrendFor(Map map)
        {
            if (map == null)
            {
                return string.Empty;
            }

            return _trendByMap.TryGetValue(map.uniqueID, out string trend) ? trend ?? string.Empty : string.Empty;
        }

        public override void GameComponentTick()
        {
            RimTalkMemoryPatchSettings settings = RimTalkMemoryPatchMod.Settings;
            if (settings == null || !settings.enableColonyTrend)
            {
                return;
            }

            if (Find.TickManager.TicksGame % TicksPerCheck != 0)
            {
                return;
            }

            List<Map> maps = Find.Maps;
            for (int index = 0; index < maps.Count; index++)
            {
                Map map = maps[index];
                if (map.IsPlayerHome)
                {
                    Consider(map, settings);
                }
            }
        }

        private void Consider(Map map, RimTalkMemoryPatchSettings settings)
        {
            if (_inFlight.Contains(map.uniqueID) || !IndependentAISummarizer.IsAvailable())
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            int interval = Math.Max(1, settings.colonyTrendIntervalHours) * GenDate.TicksPerHour;
            if (_askedAtByMap.TryGetValue(map.uniqueID, out int asked) && now - asked < interval)
            {
                return;
            }

            ColonySnapshot snapshot = ColonySnapshotCollector.Collect(map);
            if (snapshot == null)
            {
                return;
            }

            string fingerprint = snapshot.Fingerprint();
            if (_fingerprintByMap.TryGetValue(map.uniqueID, out string previous) && previous == fingerprint)
            {
                // Nothing has moved since the last paragraph, so the last
                // paragraph is still the true one. The timer is reset anyway,
                // or a becalmed colony would rebuild the snapshot every check.
                _askedAtByMap[map.uniqueID] = now;
                return;
            }

            Ask(map, snapshot, fingerprint, settings, now);
        }

        private void Ask(Map map, ColonySnapshot snapshot, string fingerprint,
            RimTalkMemoryPatchSettings settings, int now)
        {
            int mapId = map.uniqueID;
            _inFlight.Add(mapId);
            _askedAtByMap[mapId] = now;
            string prompt = ColonyTrendPrompt.Build(snapshot, settings);

            RimAiBackground.Run(async () =>
            {
                string answer = null;
                try
                {
                    answer = await IndependentAISummarizer.CallAIAsync(prompt);
                }
                // RimAI.catch-boundary: ALLOWED_TOP_LEVEL_BOUNDARY - top of a background task; an unobserved exception here would take the game down.
                catch (Exception exception)
                {
                    RimAiLog.Warning(RimAiLogCategory.Memory,
                        "[ColonyTrend] The summary request failed.", exception: exception);
                }

                string trend = Clean(answer, settings);
                IndependentAISummarizer.EnqueueMainThreadAction(() => Store(mapId, trend, fingerprint));
            });
        }

        private void Store(int mapId, string trend, string fingerprint)
        {
            _inFlight.Remove(mapId);
            if (trend.NullOrEmpty())
            {
                return;
            }

            _trendByMap[mapId] = trend;
            _fingerprintByMap[mapId] = fingerprint;
            ModuleLog.Message("[ColonyTrend] map " + mapId + ": " + trend);
        }

        /// <summary>
        /// One paragraph, no longer than the player asked for. A model handed
        /// a list of numbers likes to answer with a list of numbers; what is
        /// wanted here is the sentence a colonist would say.
        /// </summary>
        private static string Clean(string answer, RimTalkMemoryPatchSettings settings)
        {
            if (answer.NullOrEmpty())
            {
                return string.Empty;
            }

            StringBuilder flattened = new StringBuilder(answer.Trim());
            flattened.Replace("\r\n", " ").Replace('\n', ' ').Replace("  ", " ");
            string text = flattened.ToString().Trim();
            int limit = Math.Max(40, settings?.colonyTrendMaxLength ?? 400);
            return text.Length <= limit ? text : Shorten(text, limit);
        }

        /// <summary>
        /// Cut to the last sentence that fits, or failing that the last whole
        /// word. The first version cut at the character and left "але ніх…" in
        /// the middle of a word, which reads as a bug in the game rather than
        /// a limit somebody set.
        /// </summary>
        private static string Shorten(string text, int limit)
        {
            string head = text.Substring(0, limit);
            int sentence = head.LastIndexOfAny(new[] { '.', '!', '?', '…' });
            if (sentence >= limit / 2)
            {
                return head.Substring(0, sentence + 1).TrimEnd();
            }

            int space = head.LastIndexOf(' ');
            return (space > 0 ? head.Substring(0, space) : head).TrimEnd() + "…";
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref _trendByMap, "colonyTrendByMap", LookMode.Value, LookMode.Value,
                ref _scribeKeys, ref _scribeTrends);
            Scribe_Collections.Look(ref _fingerprintByMap, "colonyTrendFingerprintByMap",
                LookMode.Value, LookMode.Value, ref _scribeKeys, ref _scribeFingerprints);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _trendByMap ??= new Dictionary<int, string>();
                _fingerprintByMap ??= new Dictionary<int, string>();
                _askedAtByMap ??= new Dictionary<int, int>();
            }
        }
    }
}
