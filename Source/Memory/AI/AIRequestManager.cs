using RimWorld.Planet;
using Verse;

namespace Ustas.RimAI.Communication.Memory.AI
{
    public class AIRequestManager : WorldComponent
    {
        public AIRequestManager(World world) : base(world) { }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            // Process a few callbacks each tick to avoid lag spikes
            IndependentAISummarizer.ProcessPendingCallbacks(5);
        }
    }
}
