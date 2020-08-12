using System.Collections.Generic;
using EFSMono.Scripts.Autoload;
using EFSMono.Scripts.DataStructures.Geometry;
using Godot;

namespace EFSMono.Scripts.SystemModules.PhysicsControllerModule.RIDConstructors.PolygonPartitioning
{
/// <summary>
/// Helper class for both ChordlessPolygonDecomposer and ComplexPolygonDecomposer that obtains their concave vertices.
/// Requires all input polygons to be CCW ordered.
/// </summary>
public static class ConcaveVertexFinder
{
    
    /// <summary>
    /// Checks polygon vertices (of both its outer perimeters and holes) and returns a list of the concave ones.
    /// </summary>
    /// <param name="allPerims">List of array of Vector2s describing all perimeters of a polygon including its outer
    /// perimeter and hole perimeters.</param>
    /// <returns>List of concave vertices.</returns>
    public static HashSet<ConcaveVertex> GetConcaveVertices(List<Vector2>[] allPerims)
    {
        var concaveVertices = new HashSet<ConcaveVertex>();
        for (int i = 0; i < allPerims.Length; i++)
        {
            List<Vector2> perim = allPerims[i];
            bool isHole = (i != 0);
            concaveVertices.SymmetricExceptWith(_TracePerimeterForConcaveVertices(perim, isHole));
        }
        return concaveVertices;
    }
 
    /// <summary>
    /// Checks polygon vertices (of both its outer perimeters and holes) and returns a list of the concave ones.
    /// </summary>
    /// <param name="polygon"></param>
    /// <returns>List of concave vertices.</returns>
    public static HashSet<ConcaveVertex> GetConcaveVertices(ChordlessPolygon polygon)
    {
        var concaveVertices = new HashSet<ConcaveVertex>();
        concaveVertices.SymmetricExceptWith(_TracePerimeterForConcaveVertices(polygon.outerPerim));

        foreach (List<Vector2> hole in polygon.holes)
        {
            concaveVertices.SymmetricExceptWith(_TracePerimeterForConcaveVertices(hole, true));
        }
        return concaveVertices;
    }

    /// <summary>
    /// Iterates through the input <param>perimeter</param> from the vertex with the min X and min Y coordinates (which
    /// is ALWAYS convex) in CCW order.  Three vertices are needed to check whether the middle vertex is concave or not,
    /// AKA some sequence of vertices P, Q, and R can be used to determine Q given that the vertices are NOT collinear.
    /// Q is concave IF the midpoint between P and R is WITHIN the polygon, AKA:
    ///     * If the perimeter being checked is not a hole (AKA it is the polygon's outer perim), then P->R's midpoint
    ///       must be inside the polygon for Q to be concave.
    ///     * If the perimeter being checked IS a hole, then P->R's midpoint must NOT be inside the hole for Q to be
    ///       concave.
    /// </summary>
    /// <param name="perimeter">Perimeter of either polygon or its holes.</param>
    /// <returns>List of concave vertices.</returns>
    private static HashSet<ConcaveVertex> _TracePerimeterForConcaveVertices(IReadOnlyList<Vector2> perimeter, bool hole = false)
    {
        var concaveVertices = new HashSet<ConcaveVertex>();
        int startID = GeometryFuncs.GetMinXMinYCoord(perimeter);
        for (int i = 0; i < perimeter.Count - 1; i++) //Count - 1 because of closed loop convention
        { //as a side note, perim is always ordered CCW thanks to EdgeCollection's simplification algorithm.
            int thisIndex = (i + startID);
            Vector2 thisVertex = perimeter[thisIndex % (perimeter.Count - 1)];
            Vector2 prevVertex = perimeter[(thisIndex - 1 + perimeter.Count - 1) % (perimeter.Count - 1)];
            Vector2 nextVertex = perimeter[(thisIndex + 1) % (perimeter.Count - 1)];
            float angle = Mathf.Rad2Deg(Mathf.Atan2(thisVertex.y - prevVertex.y, thisVertex.x - prevVertex.x) - 
                                        Mathf.Atan2(nextVertex.y - thisVertex.y, nextVertex.x - thisVertex.x));
            if (angle < 0) angle += 360;
            if (angle == 270 && hole)
            { //hole with 90 degree angle would be 270 on the other side .'. concave
                concaveVertices.Add(new ConcaveVertex(thisVertex, prevVertex, nextVertex));
            }
            else if (angle == 90 && !hole)
            { //DID you know that if you order polygons CCW then they have negative area via shoelace formula and their angles are measured from the 'outside' and somehow i didn't put two and two together and figure that out because i cannot understand math
                concaveVertices.Add(new ConcaveVertex(thisVertex, prevVertex, nextVertex));
            }
        }
        return concaveVertices;
    }
    
}
}