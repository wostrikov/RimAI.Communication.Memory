using Ustas.RimAI.Communication.Memory.Capture;
using Ustas.RimAI.Communication.Memory.UI;
using Ustas.RimAI.Communication.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Ustas.RimAI.Communication.Memory
{
    internal abstract class FourLayerMemoryCompCollaborator
    {
        internal readonly FourLayerMemoryComp Owner;

        protected FourLayerMemoryCompCollaborator(FourLayerMemoryComp owner)
        {
            Owner = owner;
        }

        protected FourLayerMemoryCompParts Parts => Owner.Parts;

        protected List<MemoryEntry> activeMemories
        {
            get => Owner.activeMemories;
            set => Owner.activeMemories = value;
        }
        protected List<MemoryEntry> situationalMemories
        {
            get => Owner.situationalMemories;
            set => Owner.situationalMemories = value;
        }
        protected List<MemoryEntry> eventLogMemories
        {
            get => Owner.eventLogMemories;
            set => Owner.eventLogMemories = value;
        }
        protected List<MemoryEntry> archiveMemories
        {
            get => Owner.archiveMemories;
            set => Owner.archiveMemories = value;
        }
        protected JobMemoryCapturer _jobCapturer => Owner._jobCapturer;
        
        protected int MaxSCM => Owner.MaxSCM;
        protected int MaxELS => Owner.MaxELS;
        protected int MaxABM => Owner.MaxABM;
        protected bool IsRoundMemoryEnabled => FourLayerMemoryComp.IsRoundMemoryEnabled;
        protected ThingWithComps parent => Owner.parent;
        protected List<MemoryEntry> ActiveMemories => Owner.ActiveMemories;
        protected List<MemoryEntry> SituationalMemories => Owner.SituationalMemories;
        protected List<MemoryEntry> EventLogMemories => Owner.EventLogMemories;
        protected List<MemoryEntry> ArchiveMemories => Owner.ArchiveMemories;
    }
}
