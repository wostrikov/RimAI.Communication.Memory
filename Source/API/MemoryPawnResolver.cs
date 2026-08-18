using System.Linq;
using Ustas.RimAI.Core.Memory;
using Verse;

namespace Ustas.RimAI.Communication.Memory.API;

/// <summary>
/// Resolves a Verse <see cref="Pawn"/> from a typed <see cref="MemoryContextRequest"/>.
/// Extracted from <see cref="MemoryContextProvider"/> — retrieval semantics unchanged.
/// </summary>
internal static class MemoryPawnResolver
{
    public static Pawn Resolve(MemoryContextRequest request)
    {
        if (request?.Pawn is Pawn direct)
            return direct;
        var id = request?.PawnId;
        if (string.IsNullOrEmpty(id))
            id = request?.PawnIds?.FirstOrDefault();
        if (string.IsNullOrEmpty(id))
            return null;
        var maps = Find.Maps;
        if (maps == null)
            return null;
        foreach (var map in maps)
        {
            var pawn = map?.mapPawns?.AllPawns?.FirstOrDefault(p => p != null && p.ThingID == id);
            if (pawn != null)
                return pawn;
        }

        return null;
    }
}
