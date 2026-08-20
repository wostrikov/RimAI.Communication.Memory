using System;
using Verse;
using RimWorld.Planet;
using Ustas.RimAI.Core.Handshake;

namespace Ustas.RimAI.Communication.Memory
{
    [StaticConstructorOnStartup]
    public static class BackCompatibilityFix
    {
        static BackCompatibilityFix()
        {
            if (!RimAiHandshake.IsApproved(RimAiModuleIds.Memory))
            {
                return;
            }

            ForceInitialize();
        }
        
        public static void ForceInitialize()
        {
            try
            {
                
                var memoryManagerType = typeof(MemoryManager);
                var aiRequestManagerType = typeof(AI.AIRequestManager);
                
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(memoryManagerType.TypeHandle);
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(aiRequestManagerType.TypeHandle);
                
                Log.Message($"[RimTalk BackCompat] ✅ Types pre-initialized:");
                Log.Message($"  - {memoryManagerType.FullName}");
                Log.Message($"  - {aiRequestManagerType.FullName}");
                
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk BackCompat] ❌ Initialization failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
