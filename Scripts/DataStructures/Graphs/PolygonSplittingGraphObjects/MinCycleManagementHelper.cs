using System.Collections.Generic;
using System.Linq;
using EFSMono.Scripts.Autoload;
using Godot;

namespace EFSMono.Scripts.DataStructures.Graphs.PolygonSplittingGraphObjects
{
/// <summary>
/// Helper class for PolygonSplittingGraph that handles the management of MinCycle extraction, AKA updating which edges
/// can no longer be travelled, whether any bridges are present in the graph and need to be removed, etc.
/// </summary>
public static class MinCycleManagementHelper
{
    /// <summary>
    /// This method uses wikipedia's definition of 'simple' and 'complex' for polygons, found here:
    /// https://en.wikipedia.org/wiki/Simple_polygon
    /// The idea is that this method exists to check if a cycle:
    ///     1. Self-intersects
    ///     2. Or has a hole.
    /// Which is done by checking if the cycle has MORE THAN ONE INSTANCE of a vertex appearing TWICE OR MORE TIMES,
    /// as the only way a single cycle (extracted from what is essentially a planar graph WITHOUT BRIDGES) would have
    /// a hole is if it also self-intersects, or two parts of the polygon 'share' a vertex (which may also be
    /// self-intersection, I don't actually know because it's not easy for my caveman brain to de-encrypt math-heavy
    /// descriptions/definitions).  The simplest case is a 3x3 square with the middle removed, AND also one corner removed,
    /// as this shape can be defined by a single cycle that pretty much describes a long rectangle that was bent into that
    /// shape until it touches its own corner.
    ///
    /// (Incoming mini-rant) I am completely unsure if the simplest case described above counts as a complex polygon,
    /// as you could argue that 'technically the polygon does not explicitly define a hole, the hole exists because of
    /// the way that its perimeter is defined', but you could also argue that the "infinite planar-face outside of the
    /// polygon technically is complex in of itself because it self-intersects to form the 1x1 square inside the polygon".
    /// I don't know.  All I know is that these kind of cases add nothing to the algorithm because they just give more
    /// work for ChordlessPolygonDecomposer to do down the line, and it doesn't even matter because any rectangles that
    /// can be formed out of a complex cycle can also be formed out of the simpler cycles extracted from the same
    /// ConnectedGroup.
    /// </summary>
    /// <param name="cyclePerim"></param>
    /// <returns>False if <param>cyclePerim</param> has one or less duplicate vertex, true otherwise.  This is because a
    /// non-complex cycle will have at most one duplicate vertex, which is its first and last vertex due to the
    /// convention that a closed loop vector array has array.first == array.last.</returns>
    public static bool IsCycleComplex(Vector2[] cyclePerim)
    {
        int duplicateCount = 0;
        var vertices = new HashSet<Vector2>();
        foreach (Vector2 vertex in cyclePerim)
        {
            if (!vertices.Contains(vertex)) vertices.Add(vertex);
            else duplicateCount++;
        }
        return duplicateCount > 1;
    }
    
    /// <summary>
    /// Checks if the input <param>cyclePerim</param> contains any vertices in <param>nodeGroup</param> within itself
    /// that are NOT part of its perimeter.
    /// </summary>
    /// <param name="cyclePerim">A planar face found within <param>connectedGroup</param>, which has a planar embedding.</param>
    /// <param name="groupNodesPerim">A group of nodes that form the perimeter of the ConnectedNodeGroup <param>cycle</param>
    /// was extracted from.</param>
    /// <returns></returns>
    public static bool IsCycleOuterPerim(Vector2[] cyclePerim, List<PolygonSplittingGraphNode> groupNodesPerim)
    {
        var groupPerim = new Vector2[groupNodesPerim.Count];
        for (int i = 0; i < groupNodesPerim.Count; i++)
        {
            groupPerim[i] = new Vector2(groupNodesPerim[i].x, groupNodesPerim[i].y);
        }
        return GeometryFuncs.ArePolysIdentical(cyclePerim, groupPerim);
    }

    /// <summary>
    /// Checks if the cycle is a hole.  NOTE that no checks are done to ensure that cyclePerim is simplified, but
    /// this is not required as holes are always passed into PolygonSplittingGraph in their most simple form, and
    /// when extracted they keep the same vertices.
    /// Q: "But what if a bridge or chord or something connects the outside perimeter to a point on a hole where a vertex
    /// does not already exist?"
    /// A: This will never happen as the only way a hole will be connected to anything else is via a chord, which MUST
    /// be attached to a concave vertex.
    /// </summary>
    /// <param name="cyclePerim">Cycle discovered from planar graph.></param>
    /// <param name="holes">List of holes (which are a list of Vector2s).</param>
    /// <returns>True if the cycle is a hole, false otherwise.</returns>
    public static bool IsCycleHole(Vector2[] cyclePerim, List<Vector2>[] holes)
    {
        return holes.Any(hole => GeometryFuncs.ArePolysIdentical(cyclePerim, hole.ToArray()));
    }
}
}