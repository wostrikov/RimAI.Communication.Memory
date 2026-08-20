using Ustas.RimAI.Communication.Memory.Utils;
using Ustas.RimAI.Communication.Memory;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;

namespace Ustas.RimAI.Communication.Memory
{

    public class RoundMemoryManager : GameComponent
    {
        private static RoundMemoryManager _instance;
        public static RoundMemoryManager Instance
        {
            get
            {
                if (Current.Game is null)
                {
                    _instance = null;
                    Log.Error("[RoundMemory] Спроба доступу до контролера RoundMemory поза збереженням");
                }
                return _instance;
            }
        }

        private const int MaxRoundMemory = 256;

        private RimRingBuffer<RoundMemory> _roundMemories = new(MaxRoundMemory);
        public RimRingBuffer<RoundMemory> RoundMemories => _roundMemories;

        private List<RoundMemory> _tmpRoundMemories;

        private long _nextRoundMemoryId = 0;

        private string _playerDialogue = string.Empty;

        private readonly ConditionalWeakTable<object, RoundMemory> _dictToRoundMemory = new();

        public RoundMemoryManager(Game game) : base()
        {
            _instance = this;
        }

        public static long GetNewRoundMemoryId()
        {
            if (Instance is null)
            {
                Log.Error("[RoundMemory] Контролер RoundMemory відсутній під час видачі номера; повернено -1");
                return -1;
            }
            return System.Threading.Interlocked.Increment(ref Instance._nextRoundMemoryId);
        }

        public static void CapturePlayerDialogue(Pawn playerPawn, string playerDialogue)
        {
            if (Instance is null)
            {
                Log.Error("[RoundMemory] Контролер RoundMemory відсутній під час захоплення діалогу гравця");
                return;
            }

            string playerName = playerPawn?.LabelShort;
            Instance._playerDialogue = $"{(string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName)}: {playerDialogue}";

            Log.Message("[RoundMemory] Репліку гравця успішно захоплено");
        }

        public static void StreamingBuildRoundMemory<T>(
            T talkRequest,
            string content,
            IEnumerable<Pawn> participants = null,
            bool isPlayerInitiate = false
            ) where T : class
        {
            if (Instance is null)
            {
                Log.Error("[RoundMemory] Контролер RoundMemory відсутній під час побудови пам'яті раунду");
                return;
            }

            var dicToRoundMemory = Instance._dictToRoundMemory;

            if (!dicToRoundMemory.TryGetValue(talkRequest, out var roundMemory))
            {
                if (!participants?.Any() ?? true)
                {
                    Log.Warning("[RoundMemory] Для потокового створення пам'яті раунду не вказано учасників");
                    return;
                }

                string initialContent = null;

                if (isPlayerInitiate && (RimTalkMemoryPatchMod.Settings?.IsPlayerDialogueInject ?? true))
                    initialContent = Instance._playerDialogue;

                var participantsHashSet = participants.ToHashSet();

                roundMemory = new RoundMemory(participantsHashSet, initialContent);

                dicToRoundMemory.AddOrUpdate(talkRequest, roundMemory);

                AddRoundMemory(participantsHashSet, roundMemory);
            }

            roundMemory?.AppendLine(content);
        }

        public static void BuildRoundMemory(HashSet<Pawn> pawns, string content)
        {
            if (Instance is null)
            {
                Log.Error("[RoundMemory] Контролер RoundMemory відсутній під час побудови пам'яті раунду");
                return;
            }

            var roundMemory = new RoundMemory(pawns, content);

            AddRoundMemory(pawns, roundMemory);
        }

        private static void AddRoundMemory(HashSet<Pawn> pawns, RoundMemory roundMemory)
        {
            Instance._roundMemories.Add(roundMemory);

            if (pawns is null) return;
            foreach (var pawn in pawns)
            {
                if (pawn is null) continue;
                pawn.TryGetComp<FourLayerMemoryComp>()?.ActiveMemories?.Add(roundMemory);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(
                ref _nextRoundMemoryId,
                "NextRoundMemoryId",
                0
            );

            LookRoundMemories();

            Log.Message($"[RoundMemory] ExposeData for RoundMemory: count={_roundMemories.Count}; NextRoundMemoryId: {_nextRoundMemoryId}");
        }

        private void LookRoundMemories()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                _tmpRoundMemories = new();
                for (int i = 0; i < _roundMemories.Count; i++)
                {
                    _tmpRoundMemories.Add(_roundMemories[i]);
                }
            }

            // look look
            Scribe_Collections.Look(
                ref _tmpRoundMemories,
                "RoundMemories",
                LookMode.Deep
            );

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (_tmpRoundMemories is null) return;  // Non-obvious edge case — read carefully before changing. (null if)
                foreach (var roundMemory in _tmpRoundMemories)
                {
                    _roundMemories.Add(roundMemory);
                }
            }

            if (Scribe.mode == LoadSaveMode.Saving || Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _tmpRoundMemories = null;
            }
        }

        public override void FinalizeInit()
        {
            Dictionary<long, RoundMemory> managerMap = new();
            if (_roundMemories is null)
            {
                Log.Warning("[RoundMemory] RoundMemory порожня; виправити вказівники неможливо");
                return;
            }
            for (int i = 0; i < _roundMemories.Count; i++)
            {
                var roundMemory = _roundMemories[i];
                if (roundMemory is null)
                {
                    Log.Warning("[RoundMemory] Виявлено null-запис; його пропущено");
                    continue;
                }
                managerMap[roundMemory.RoundMemoryUniqueID] = roundMemory;
            }

            var allPawns = PawnsFinder.All_AliveOrDead;
            if (allPawns is null) return;

            foreach (var pawn in allPawns)
            {
                var comp = pawn?.TryGetComp<FourLayerMemoryComp>();
                if (comp is null) continue;

                var ABMs = comp.ActiveMemories;
                if (ABMs is null || ABMs.Count == 0) continue;

                for (int i = 0; i < ABMs.Count; i++)
                {
                    var ABM = ABMs[i];

                    if (ABM is null
                        || ABM is not RoundMemory ABMRef
                        || !managerMap.TryGetValue(ABMRef.RoundMemoryUniqueID, out var managerRef)
                        || ABMRef == managerRef)
                        continue;

                    ABMs[i] = managerRef;

                    if (Prefs.DevMode) Log.Message("[RoundMemory] Вказівник ABM виправлено");
                }
            }
        }
    }

}
