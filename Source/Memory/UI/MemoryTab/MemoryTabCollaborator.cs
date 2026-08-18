using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Ustas.RimAI.Communication.Memory.UI
{
    internal abstract class MemoryTabCollaborator
    {
        internal readonly MainTabWindow_Memory Owner;

        protected MemoryTabCollaborator(MainTabWindow_Memory owner)
        {
            Owner = owner;
        }

        protected MemoryTabParts Parts => Owner.Parts;
    }

    internal sealed class MemoryTabParts
    {
        internal readonly MainTabWindow_Memory Owner;
        internal readonly MemoryTabTopBar TopBar;
        internal readonly MemoryTabControls Controls;
        internal readonly MemoryTabTimeline Timeline;
        internal readonly MemoryTabActions Actions;
        internal readonly MemoryTabImportExport ImportExport;
        internal readonly MemoryTabHelpers Helpers;
        internal readonly MemoryTabUtilities Utilities;

        internal MemoryTabParts(MainTabWindow_Memory owner)
        {
            Owner = owner;
            TopBar = new MemoryTabTopBar(owner);
            Controls = new MemoryTabControls(owner);
            Timeline = new MemoryTabTimeline(owner);
            Actions = new MemoryTabActions(owner);
            ImportExport = new MemoryTabImportExport(owner);
            Helpers = new MemoryTabHelpers(owner);
            Utilities = new MemoryTabUtilities(owner);
        }
    }
}
