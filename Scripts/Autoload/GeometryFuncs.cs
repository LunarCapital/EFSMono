using System;
using System.Linq;
using Godot;
using System.Collections.Generic;
// ReSharper disable ClassNeverInstantiated.Global

namespace EFSMono.Scripts.Autoload
{
/// <summary>
/// Auto loaded script.
/// Contains easily accessible geometry-related functions.
/// </summary>
public class GeometryFuncs : Node
{
    /// <summary>
    /// Checks if two line segments are parallel.
    /// </summary>
    /// <param name="line1A">Line 1's point A.</param>
    /// <param name="line1B">Line 1's point B.</param>
    /// <param name="line2A">Line 2's point A</param>
    /// <param name="line2B">Line 2's point B.</param>
    /// <returns>True if parallel, false otherwise.</returns>
    public static bool AreSegmentsParallel(Vector2 line1A, Vector2 line1B,
                                           Vector2 line2A, Vector2 line2B)
    {
        if (line1A.x == line1B.x) return line2A.x == line2B.x;
        if (line1A.y == line1B.y) return line2A.y == line2B.y;
        float gradient1 = /*Mathf.Abs*/((line1B.y - line1A.y) / (line1B.x - line1A.x));
        float gradient2 = /*Mathf.Abs*/((line2B.y - line2A.y) / (line2B.x - line2A.x));
        return (gradient1 == gradient2);
    }
    
    /// <summary>
    /// Checks if two line segments are collinear.  Credit to:
    /// https://math.stackexchange.com/questions/1102258/how-to-determine-if-some-line-segments-are-collinear
    /// for the method, which uses Heron's formula.
    /// </summary>
    /// <param name="line1A">Line 1's point A.</param>
    /// <param name="line1B">Line 1's point B.</param>
    /// <param name="line2A">Line 2's point A</param>
    /// <param name="line2B">Line 2's point B.</param>
    /// <returns>True if collinear.</returns>
    public static bool AreSegmentsCollinear(Vector2 line1A, Vector2 line1B,
                                            Vector2 line2A, Vector2 line2B)
    {
        if (!AreSegmentsParallel(line1A, line1B, line2A, line2B)) return false;
        float x1 = line1A.x;
        float y1 = line1A.y;
        float x2 = line1B.x;
        float y2 = line1B.y;
        float x3 = line2A.x;
        float y3 = line2A.y; //if orientation is the same, doesn't matter which point from line2 we use
        return 0 == x1*y2 + x2*y3 + x3*y1 - x1*y3 - x2*y1 - x3*y2;
    }

    /// <summary>
    /// Checks if two collinear line segments overlap by:
    ///     1. Firstly checking if they are actually collinear
    ///     2. Ordering each line segment in ascending XY order
    ///     3. Checking if either:
    ///         3.1 Line2.B is less than or equal to Line1.A
    ///         3.2 Line1.B is less than or equal to Line2.A
    /// If either of the above conditions are true, the line segments do NOT overlap.  Otherwise, they overlap.
    /// </summary>
    /// <param name="line1A">Line 1's point A.</param>
    /// <param name="line1B">Line 1's point B.</param>
    /// <param name="line2A">Line 2's point A</param>
    /// <param name="line2B">Line 2's point B.</param>
    /// <returns>True if segments overlap and are collinear, false otherwise.</returns>
    public static bool DoSegmentsOverlap(Vector2 line1A, Vector2 line1B,
                                         Vector2 line2A, Vector2 line2B)
    {
        if (!AreSegmentsCollinear(line1A, line1B, line2A, line2B)) return false;
        //float[] line1 = {line1A.Length() - line1A.Length(), line1B.Length() - line1A.Length()};
        //float[] line2 = {line2A.Length() - line1A.Length(), line2B.Length() - line1A.Length()}; //parametrised for easy comparison
        float[] line1 = {line1A.DistanceSquaredTo(line1A), line1B.DistanceSquaredTo(line1A)};
        float line2Amod = (line1A.DirectionTo(line1B) == line1A.DirectionTo(line2A)) ? 1 : -1;
        float line2Bmod = (line1A.DirectionTo(line1B) == line1A.DirectionTo(line2B)) ? 1 : -1; //to sign distances, as vector distances are always magnitudes
        float[] line2 = {line2A.DistanceSquaredTo(line1A) * line2Amod, line2B.DistanceSquaredTo(line1A) * line2Bmod}; //parametrised for easy comparison
        Array.Sort(line1);
        Array.Sort(line2);
        
        if (line1[0] == line2[0] && line1[1] == line2[1]) {
            return true; //lines are the same. (without this condition the next one returns false)
        }

        return !(line2[1] <= line1[0]) && !(line1[1] <= line2[0]); // <= because two collinear lines connected at one point do not count as overlapping
    }

    /// <summary>
    /// Tests if two segments intersect using the line-line intersection algorithm, which can be found here:
    /// https://en.wikipedia.org/wiki/Line%E2%80%93line_intersection
    /// In this program, lines that are collinear and overlapping do not count as intersecting.  They will be
    /// calculated as being 'parallel' and this function will return false.  To check overlap, DoSegmentsOverlap()
    /// should be used instead.
    /// </summary>
    /// <param name="line1A">Line 1's point A.</param>
    /// <param name="line1B">Line 1's point B.</param>
    /// <param name="line2A">Line 2's point A</param>
    /// <param name="line2B">Line 2's point B.</param>
    /// <returns>True if the lines intersect, false otherwise.</returns>
    public static bool DoSegmentsIntersect(Vector2 line1A, Vector2 line1B,
                                           Vector2 line2A, Vector2 line2B)
    {
        float x1 = line1A.x;
        float y1 = line1A.y;
        float x2 = line1B.x;
        float y2 = line1B.y;

        float x3 = line2A.x;
        float y3 = line2A.y;
        float x4 = line2B.x;
        float y4 = line2B.y;

        float uA = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));
        float uB = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));

        return uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1;
    }

    /// <summary>
    /// Checks if a polygon is ordered in CCW order using the shoelace formula.
    /// Assumes that the polygon's points are in order.
    /// </summary>
    /// <param name="perimVertices"></param>
    /// <returns>True if CCW, false otherwise.</returns>
    public static bool IsPolygonCCW(Vector2[] perimVertices)
    {
        return _GetRawAreaOfPolygon(perimVertices) < 0;
    }
    
    /// <summary>
    /// Calculates the area of an irregular polygon.
    /// If the input uses the closed array convention (AKA perim[first] == perim[last]) this function still works.
    /// </summary>
    /// <param name="perimVertices">Array of Vector2s describing the vertices of a polygon.</param>
    /// <returns>Area of input polygon.</returns>
    public static float GetAreaOfPolygon(Vector2[] perimVertices)
    {
        float area = Mathf.Abs(_GetRawAreaOfPolygon(perimVertices));
        return area;
    }

    /// <summary>
    /// Gets area of polygon with shoelace formula but with the sign intact.
    /// </summary>
    /// <param name="perimVertices"></param>
    /// <returns>Area as a float, even if negative.</returns>
    private static float _GetRawAreaOfPolygon(Vector2[] perimVertices)
    {
        Vector2[] uniqueVertices = SimplifyVectorArray(perimVertices);
        float area = 0;
        for (int i = 0; i < uniqueVertices.Length; i++)
        {
            Vector2 thisVertex = uniqueVertices[i];
            Vector2 nextVertex = uniqueVertices[(i + 1) % uniqueVertices.Length];
            area += nextVertex.x * thisVertex.y - nextVertex.y * thisVertex.x;
        }
        return area/2;
    }
    
    /// <summary>
    /// Checks if <param>innerPoly</param> is in <param>outerPoly</param> by:
    ///     1. Checking that none of the polygons lines intersect with each other
    ///     2. Checking if any point of <param>innerPoly</param> is inside <param>outerPoly</param>
    /// If NO LINES INTERSECT and ANY POINT of <param>innerPoly</param> is inside <param>outerPoly</param> then
    /// <param>innerPoly</param> is inside <param>outerPoly</param>.
    /// 
    /// If <param>innerPoly</param> has a greater or equal area than <param>outerPoly</param> then this method returns
    /// false.
    /// </summary>
    /// <param name="innerPoly">Inner polygon.</param>
    /// <param name="outerPoly">Outer polygon.</param>
    /// <returns>True if <param>innerPoly</param> is inside <param>outerPoly</param>, false otherwise.</returns>
    public static bool IsPolyInPoly(Vector2[] innerPoly, Vector2[] outerPoly)
    {
        if (GetAreaOfPolygon(innerPoly) >= GetAreaOfPolygon(outerPoly) ||
            ArePolysIdentical(innerPoly, outerPoly)) return false;

        for (int i = 0; i < innerPoly.Length; i++)
        {
            for (int j = 0; j < outerPoly.Length; j++)
            {
                Vector2 innerVertexA = innerPoly[i];
                Vector2 innerVertexB = innerPoly[(i + 1) % innerPoly.Length];
                Vector2 outerVertexA = outerPoly[j];
                Vector2 outerVertexB = outerPoly[(j + 1) % outerPoly.Length];
                if (DoSegmentsIntersect(innerVertexA, innerVertexB, outerVertexA, outerVertexB))
                {
                    return false;
                }
            }
        }
        return IsPointInPoly(innerPoly.First(), outerPoly);
    }

    /// <summary>
    /// Checks if the <param>point</param> is inside the <param>poly</param> using the winding number method.
    /// Note that the WN method works even for self-intersecting polygons, or polygons that have a hole that is not
    /// explicitly defined, HOWEVER, a point on the segment of the polygon IS considered by the WN method to be 'inside'
    /// said polygon.
    /// </summary>
    /// <param name="point">Point being checked.</param>
    /// <param name="poly">Representation of a polygon as an array of Vector2s in CCW order.</param>
    /// <returns>True if the point is inside the polygon, false otherwise.</returns>
    public static bool IsPointInPoly(Vector2 point, Vector2[] poly)
    {
        return WindingNumOfPointInPoly(point, poly) != 0;
    }

    /// <summary>
    /// Calculates the winding number of a point in a polygon using the method outlined here:
    /// http://geomalgorithms.com/a03-_inclusion.html#wn_PnPoly()
    /// That I also completely do not understand, by the way.
    /// </summary>
    /// <param name="point"></param>
    /// <param name="poly">Some polygon represented by an array of Vector2s, its vertices.  FOLLOWS LOOP CONVENTION, so
    /// absolutely necessary to ensure that poly.first == poly.last.</param>
    /// <returns>Winding number of <param>poly</param> around <param>point</param></returns>
    private static int WindingNumOfPointInPoly(Vector2 point, Vector2[] poly)
    {
        int windingNum = 0;
        for (int i = 0; i < poly.Length - 1; i++)
        {
            Vector2 thisVertex = poly[i];
            Vector2 nextVertex = poly[i + 1];
            if (thisVertex.y <= point.y)
            {
                if (nextVertex.y > point.y && IsLeft(thisVertex, nextVertex, point) > 0)
                {
                    windingNum++;
                }
            }
            else
            {
                if (nextVertex.y <= point.y && IsLeft(thisVertex, nextVertex, point) < 0)
                {
                    windingNum--;
                }
            }
        }
        return windingNum;
    }

    /// <summary>
    /// Also from http://geomalgorithms.com/a03-_inclusion.html#wn_PnPoly(), checks if some point P2 is left/on/right
    /// of an infinite line that passes through p0 and p1.
    /// </summary>
    /// <param name="p0"></param>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <returns> more than 0 if left, ==0 if on, less than 0 if right.</returns>
    private static float IsLeft(Vector2 p0, Vector2 p1, Vector2 p2)
    {
        return (p1.x - p0.x) * (p2.y - p0.y) - (p2.x - p0.x) * (p1.y - p0.y);
    }

    /// <summary>
    /// Checks if the input <param>point</param> lies on one of <param>poly</param>'s segments. The existence of this
    /// method is necessary because IsPointInPoly uses the winding-number method which returns true for a check involving
    /// a point that is on one of the polygon's segments (and we may want to exclude that case, so we use this method).
    /// </summary>
    /// <param name="point"></param>
    /// <param name="poly">A polygon that is assumed to be using the closed loop convention.</param>
    /// <returns>True if <param>point</param> is on one of <param>poly</param>'s segments, false otherwise.</returns>
    public static bool IsPointOnPolyBoundary(Vector2 point, Vector2[] poly)
    {
        bool pointIsOnPolyBoundary = false;
        for (int i = 0; i < poly.Length - 1; i++)
        {
            Vector2 thisVertex = poly[i];
            Vector2 nextVertex = poly[i + 1];
            if (point == thisVertex || point == nextVertex)
            {
                pointIsOnPolyBoundary = true;
                break;
            }
            if (AreSegmentsCollinear(thisVertex, point, point, nextVertex))
            {
                float lineAGradient = (point.y - thisVertex.y) / (point.x - thisVertex.x);
                float lineBGradient = (nextVertex.y - point.y) / (nextVertex.x - point.x);
                if ((lineAGradient >= 0 && lineBGradient >= 0) || (lineAGradient <= 0 && lineBGradient <= 0))
                {
                    pointIsOnPolyBoundary = true;
                    break;
                }
            }
        }
        return pointIsOnPolyBoundary;
    }
    
    /// <summary>
    /// Checks if the input polygons are identical, even if they are out of order or one is CW and the other is CCW, etc.
    /// Assumes that the polygon's edges do not intersect itself, and does not work if one polygon has been simplified
    /// and the other has not.
    /// </summary>
    /// <param name="polyA"></param>
    /// <param name="polyB"></param>
    /// <returns>True if identical, false otherwise.</returns>
    public static bool ArePolysIdentical(Vector2[] polyA, Vector2[] polyB)
    {
        if (polyA.Length != polyB.Length) return false;
        var setA = new HashSet<Vector2>(polyA);
        var setB = new HashSet<Vector2>(polyB);
        setA.ExceptWith(setB);
        return setA.Count == 0;
    }

    /// <summary>
    /// If the input <param>perim</param> follows the closed loop convention where perim[first] == perim[last],
    /// returns a simplified version of the array that removes the duplicate Vector2.
    /// </summary>
    /// <param name="perim">Vector2 array describing a perimeter.</param>
    /// <returns><param>perim</param> IFF it does not follow closed loop convention, or <param>perim</param> without its
    /// last index if it does.</returns>
    public static Vector2[] SimplifyVectorArray(Vector2[] perim)
    {
        Vector2[] uniqueVertices;
        if (perim.First() == perim.Last())
        { //input follows closed array convention so we need to shave the last one off
            uniqueVertices = new Vector2[perim.Length - 1];
            for (int i = 0; i < uniqueVertices.Length; i++)
            {
                uniqueVertices[i] = perim[i];
            }
        }
        else
        { //input does not follow closed array convention
            uniqueVertices = perim;
        }
        return uniqueVertices;
    }
    
    /// <summary>
    /// Gets the coord in an input array with the minimum X and minimum Y coords (top-left coord with graphics canvas).
    /// This vertex is ALWAYS convex IFF perimeter is the outer perimeter.  
    /// </summary>
    /// <param name="perimeter"></param>
    /// <returns>Index of coordinate, or -1 if input is empty.</returns>
    public static int GetMinXMinYCoord(IReadOnlyList<Vector2> perimeter)
    {
        if (perimeter.Count == 0) return -1;
        Vector2 startCoord = perimeter.First();
        int startID = 0;
        for (int i = 0; i < perimeter.Count; i++)
        {
            Vector2 vertex = perimeter[i];
            if (vertex.x < startCoord.x)
            {
                startCoord = vertex;
                startID = i;
            }
            else if (vertex.x == startCoord.x)
            {
                if (vertex.y < startCoord.y)
                {
                    startCoord = vertex;
                    startID = i;
                }
            }
        }
        return startID;
    }
}
}