using System;
using System.Linq;
using Godot;
using SCol = System.Collections.Generic;
// ReSharper disable ClassNeverInstantiated.Global

namespace EFSMono.Scripts.Autoload
{
/// <summary>
/// Auto loaded script.
/// Contains easily accessible geometry-related functions.
/// </summary>
public static class GeometryFuncs
{
    /// <summary>
    /// Checks if the orientations for two lines are the same.
    /// Two lines with opposite orientations count as having the 'same' orientation.
    /// </summary>
    /// <param name="line1A">Line 1's point A.</param>
    /// <param name="line1B">Line 1's point B.</param>
    /// <param name="line2A">Line 2's point A</param>
    /// <param name="line2B">Line 2's point B.</param>
    /// <returns>True if line orientation is the same.</returns>
    public static bool AreOrientationsIdentical(Vector2 line1A, Vector2 line1B, 
                                                Vector2 line2A, Vector2 line2B)
    {
        float m1 = (line1B.y - line1A.y)/(line1B.x - line1A.x);
        float m2 = (line2B.y - line2A.y)/(line2B.x - line2A.x);

        return Math.Abs(m1) == Math.Abs(m2);
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
        if (!AreOrientationsIdentical(line1A, line1B, line2A, line2B)) return false;
        float x1 = line1A.x;
        float y1 = line1A.y;
        float x2 = line1B.x;
        float y2 = line1B.y;
        float x3 = line2A.x;
        float y3 = line2A.y; //if orientation is the same, doesn't matter which point from line2 we use
        return (0 == x1*y2 + x2*y3 + x3*y1 - x1*y3 - x2*y1 - x3*y2);
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
        float[] line1 = {line1A.Length() - line1A.Length(), line1B.Length() - line1A.Length()};
        float[] line2 = {line2A.Length() - line1A.Length(), line2B.Length() - line1A.Length()}; //parametrised for easy comparison
        Array.Sort(line1);
        Array.Sort(line2);

        if (line1[0] == line2[0] && line1[1] == line2[1]) {
            return true; //lines are the same. (without this condition the next one returns false)
        }

        return !(line2[1] <= line1[0]) && !(line1[1] <= line2[0]); // <= because two collinear lines connected but one point do not count as overlapping
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
}
}