using System;
using System.Collections.Generic;
using RimWorld;
using Ustas.RimAI.Communication.Memory.UI;
using Ustas.RimAI.Core.TestDriver;
using Verse;

namespace Ustas.RimAI.Communication.Memory.Integration;

/// <summary>
/// Live Mind Stream tab observation. Opens MainTabWindow_Memory, applies
/// layer/type filters and multi-select, and does not call a paid provider.
/// </summary>
public static class MemoryPipelineProbe
{
    public static void Register()
    {
        TestDriverModuleOperations.Register(
            TestDriverCommandNames.ProbeMemory,
            (request, _) => new TestDriverDelegateOperation(() => Run(request)));
    }

    static TestDriverProgress Run(TestDriverRequest request)
    {
        if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null)
            return TestDriverProgress.Failed("probe_memory requires a loaded game");

        var mode = request.Arguments.GetString("mode", "mind_stream");
        var correlationId = request.Arguments.GetString("correlationId", request.RequestId);
        if (!string.Equals(mode, "mind_stream", StringComparison.OrdinalIgnoreCase))
            return TestDriverProgress.Failed("mode must be mind_stream");

        return MindStream(correlationId);
    }

    static TestDriverProgress MindStream(string correlationId)
    {
        var pawn = PickPawn();
        if (pawn == null)
            return TestDriverProgress.Failed("no colonist with FourLayerMemoryComp");

        string firstError = null;
        bool tabOpen = false;
        int visibleBefore = 0;
        int visibleAfterFilter = 0;
        int selectedCount = 0;
        bool filterChanged = false;
        MainTabWindow_Memory window = null;

        try
        {
            var def = DefDatabase<MainButtonDef>.GetNamedSilentFail("RimTalk_Memory");
            if (def == null || Find.MainTabsRoot == null)
                return TestDriverProgress.Failed("RimTalk_Memory tab is missing");

            Find.MainTabsRoot.SetCurrentTab(def, playSound: false);
            window = Find.WindowStack?.WindowOfType<MainTabWindow_Memory>();
            tabOpen = window != null;
            if (window == null)
                return FailedMind(correlationId, pawn, false, 0, 0, 0, false, 1, "Mind Stream window did not open");

            window.selectedPawn = pawn;
            window.currentMemoryComp = pawn.TryGetComp<FourLayerMemoryComp>();
            window.showABM = true;
            window.showSCM = true;
            window.showELS = true;
            window.showCLPA = true;
            window.filterType = null;
            window.InvalidateCache();
            var unfiltered = window.Parts.Utilities.GetFilteredMemories();
            visibleBefore = unfiltered?.Count ?? 0;

            window.selectedMemories.Clear();
            if (unfiltered != null)
            {
                int take = Math.Min(2, unfiltered.Count);
                for (int i = 0; i < take; i++)
                    window.selectedMemories.Add(unfiltered[i]);
            }

            selectedCount = window.selectedMemories.Count;

            window.showABM = false;
            window.filterType = MemoryType.Conversation;
            window.InvalidateCache();
            visibleAfterFilter = window.Parts.Utilities.GetFilteredMemories()?.Count ?? 0;
            filterChanged = visibleAfterFilter != visibleBefore;
        }
        catch (NullReferenceException ex)
        {
            firstError = ex.GetType().Name + ": " + ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            firstError = ex.GetType().Name + ": " + ex.Message;
        }
        catch (ArgumentException ex)
        {
            firstError = ex.GetType().Name + ": " + ex.Message;
        }
        finally
        {
            if (Find.MainTabsRoot?.OpenTab?.defName == "RimTalk_Memory")
                Find.MainTabsRoot.EscapeCurrentTab();
        }

        return TestDriverProgress.Completed(new TestDriverJsonWriter()
            .Text("mode", "mind_stream")
            .Text("correlationId", correlationId)
            .Text("pawn", pawn.LabelShort)
            .Flag("tabOpen", tabOpen)
            .Integer("visibleBefore", visibleBefore)
            .Integer("visibleAfterFilter", visibleAfterFilter)
            .Flag("filterChanged", filterChanged)
            .Integer("selectedCount", selectedCount)
            .Flag("multiSelect", selectedCount >= 2 || (selectedCount >= 1 && visibleAfterFilter <= 1))
            .Flag("EXCEPTION_PRESENT", firstError != null)
            .Text("firstError", firstError)
            .Flag("paused", Find.TickManager?.Paused ?? true)
            .Integer("ticksGame", Find.TickManager?.TicksGame ?? 0));
    }

    static TestDriverProgress FailedMind(
        string correlationId,
        Pawn pawn,
        bool tabOpen,
        int visibleBefore,
        int visibleAfterFilter,
        int selectedCount,
        bool filterChanged,
        int exceptionCount,
        string error)
    {
        return TestDriverProgress.Completed(new TestDriverJsonWriter()
            .Text("mode", "mind_stream")
            .Text("correlationId", correlationId)
            .Text("pawn", pawn?.LabelShort)
            .Flag("tabOpen", tabOpen)
            .Integer("visibleBefore", visibleBefore)
            .Integer("visibleAfterFilter", visibleAfterFilter)
            .Flag("filterChanged", filterChanged)
            .Integer("selectedCount", selectedCount)
            .Flag("EXCEPTION_PRESENT", exceptionCount > 0)
            .Text("firstError", error));
    }

    static Pawn PickPawn()
    {
        var colonists = Find.CurrentMap?.mapPawns?.FreeColonistsSpawned;
        if (colonists == null)
            return null;
        for (int i = 0; i < colonists.Count; i++)
        {
            var pawn = colonists[i];
            if (pawn == null || pawn.Dead)
                continue;
            if (pawn.TryGetComp<FourLayerMemoryComp>() != null)
                return pawn;
        }

        return null;
    }
}
