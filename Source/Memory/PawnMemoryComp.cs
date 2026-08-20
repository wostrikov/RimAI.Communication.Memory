using Verse;

namespace Ustas.RimAI.Communication.Memory
{

    /// <summary>
    /// CompProperties for PawnMemoryComp
    /// </summary>
    public class CompProperties_PawnMemory : CompProperties
    {
        public CompProperties_PawnMemory()
        {
            compClass = typeof(PawnMemoryComp);
        }
    }

    public class PawnMemoryComp : FourLayerMemoryComp
    {

    }

}
