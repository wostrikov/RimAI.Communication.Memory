using System.Collections.Generic;
using System.Linq;

namespace Ustas.RimAI.Communication.Memory.Policy
{
    /// <summary>
    /// One fight, as a drafted pawn holding position remembers it. A soldier
    /// fires dozens of shots a fight; one memory per shot would bury everything
    /// else the pawn did that day, so the shots fold into a session and the
    /// session into one line naming who was fought.
    ///
    /// A session ends after half an hour without a shot, or two hours after it
    /// began, or when the pawn switches between shooting and melee.
    /// </summary>
    public sealed class CombatSession
    {
        // GenDate.TicksPerHour, restated so the policy builds without the game.
        public const int TicksPerHour = 2500;
        public const int GapTicks = TicksPerHour / 2;
        public const int MaxDurationTicks = 2 * TicksPerHour;

        // A shot at the ground has no thing, and so no thingIDNumber; the game
        // never hands out a negative one, so this cannot collide.
        public const int GroundTargetId = -1;

        // The name is snapshotted on the first hit: the thing may be destroyed
        // or discarded before the session ends.
        private readonly Dictionary<int, string> _nameById = new Dictionary<int, string>();

        public int StartTick { get; private set; }
        public int LastTick { get; private set; }
        public bool Melee { get; private set; }
        public bool IsOpen { get; private set; }
        public int DistinctTargets => _nameById.Count;

        public bool Continues(int tick, bool melee)
        {
            return IsOpen
                && melee == Melee
                && tick - LastTick <= GapTicks
                && tick - StartTick <= MaxDurationTicks;
        }

        public void Start(int tick, bool melee)
        {
            _nameById.Clear();
            StartTick = LastTick = tick;
            Melee = melee;
            IsOpen = true;
        }

        public void Touch(int tick)
        {
            LastTick = tick;
        }

        /// <summary>
        /// A new target is the only thing that changes the memory's text; another
        /// shot at the same raider only moves the session's clock, so the caller
        /// asks this before it pays for a label.
        /// </summary>
        public bool IsNewTarget(int targetId)
        {
            return !_nameById.ContainsKey(targetId);
        }

        public void AddTarget(int targetId, string targetName)
        {
            _nameById[targetId] = targetName ?? string.Empty;
        }

        /// <summary>
        /// "Action: name" for a single target; otherwise the count of distinct
        /// targets and the three names most of them shared, the way job memories
        /// list theirs.
        /// The words come in already translated.
        /// </summary>
        public string Describe(string action, string targetsFormat, string more)
        {
            if (_nameById.Count == 1)
                return action + ": " + _nameById.Values.First();

            var groups = _nameById.Values
                .GroupBy(name => name)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, System.StringComparer.Ordinal)
                .ToList();

            string names = string.Join(", ", groups.Take(3)
                .Select(group => group.Count() == 1 ? group.Key : group.Key + " ×" + group.Count()));
            string tail = groups.Count > 3 ? " " + more : string.Empty;
            return action + " (" + string.Format(targetsFormat, _nameById.Count) + "): " + names + tail;
        }
    }
}
