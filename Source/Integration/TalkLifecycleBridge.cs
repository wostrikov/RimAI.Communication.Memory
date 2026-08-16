using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Ustas.RimAI.Communication.Data;
using Ustas.RimAI.Communication.Memory.Injection;
using Ustas.RimAI.Core.Communication;
using Ustas.RimAI.Core.Relations;
using Verse;

namespace Ustas.RimAI.Communication.Memory.Integration;

/// <summary>
/// Subscribes to typed Communication/Relations lifecycle events.
/// Replaces sibling Harmony patches against TalkService, PromptManager and Relations archives.
/// </summary>
public static class TalkLifecycleBridge
{
    static readonly Regex TextCleaner = new(@"</?(?:color[^>]*|b|i|size[^>]*)>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    static bool _registered;

    public static void Register()
    {
        if (_registered)
            return;
        _registered = true;
        TalkLifecycle.InteractionCreated += OnInteractionCreated;
        TalkLifecycle.PlayerDialogueSubmitted += OnPlayerDialogueSubmitted;
        TalkLifecycle.TalkRequestEnrichment += OnTalkRequestEnrichment;
        TalkLifecycle.PromptBuildStarted += OnPromptBuildStarted;
        RelationsDialogueLifecycle.RpgSessionFinalized += OnRpgSessionFinalized;
        RelationsDialogueLifecycle.DiplomacySummaryRecorded += OnDiplomacySummaryRecorded;
    }

    static bool RoundMemoryEnabled => RimTalkMemoryPatchMod.Settings?.IsRoundMemoryActive ?? false;

    static void OnInteractionCreated(TalkInteractionCreatedArgs args)
    {
        if (!RoundMemoryEnabled || args == null)
            return;
        if (string.Equals(args.Channel, nameof(Channel.User), System.StringComparison.Ordinal))
            return;
        if (args.TalkRequest is not TalkRequest talkRequest)
            return;

        string name = string.IsNullOrWhiteSpace(args.SpeakerName) ? "???" : args.SpeakerName;
        string content = $"{name}: {CleanText(args.Text)}";
        IEnumerable<Pawn> participants = args.Participants?.OfType<Pawn>();
        RoundMemoryManager.StreamingBuildRoundMemory(talkRequest, content, participants, args.IsPlayerInitiated);
    }

    static void OnPlayerDialogueSubmitted(object initiator, object _, string message)
    {
        if (initiator is Pawn pawn)
            RoundMemoryManager.CapturePlayerDialogue(pawn, message);
    }

    static void OnTalkRequestEnrichment(object talkRequest)
    {
        VectorTalkEnricher.Enrich(talkRequest as TalkRequest);
    }

    static void OnPromptBuildStarted()
    {
        ABMCollector.ResetDuplicateCache();
    }

    static void OnRpgSessionFinalized(RpgSessionFinalizedArgs args)
    {
        if (!RoundMemoryEnabled || args == null || string.IsNullOrWhiteSpace(args.Transcript))
            return;
        HashSet<Pawn> pawns = args.Participants?.OfType<Pawn>().Where(p => p != null).ToHashSet()
            ?? new HashSet<Pawn>();
        if (args.Initiator is Pawn initiator)
            pawns.Add(initiator);
        if (args.TargetNpc is Pawn target)
            pawns.Add(target);
        if (pawns.Count == 0)
            return;
        RoundMemoryManager.BuildRoundMemory(pawns, args.Transcript);
    }

    static void OnDiplomacySummaryRecorded(DiplomacySummaryRecordedArgs args)
    {
        if (!RoundMemoryEnabled || args == null || string.IsNullOrWhiteSpace(args.Transcript))
            return;
        if (args.Negotiator is not Pawn negotiator)
            return;
        RoundMemoryManager.BuildRoundMemory(new HashSet<Pawn> { negotiator }, args.Transcript);
    }

    static string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        return TextCleaner.Replace(text, string.Empty).Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();
    }
}
