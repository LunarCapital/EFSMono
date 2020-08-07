using System;
using EFSMono.Scripts.Autoload;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace EFSMono.Scripts.DataStructures.Geometry
{
/// <summary>
/// A class that describes a rectangular irregular polygon that may or may not have holes, but
/// does not have any chords.
/// NO CHECKS are done to ensure that the polygon actually has no chords.
/// As always, closed loop conventions state that for some vector array[first] == array[last].
/// </summary>
public class ChordlessPolygon
{
    public Vector2[] outerPerim { get; }
    public Vector2[] outerPerimUnsimplified { get;  }
    public List<Vector2>[] holes { get; }
    public Dictionary<Vector2, HashSet<Vector2>> bridges { get; }

    public ChordlessPolygon(Vector2[] outerPerim, List<Vector2>[] potentialHoles, Dictionary<Vector2, HashSet<Vector2>> bridges)
    {
        this.outerPerimUnsimplified = outerPerim;
        this.outerPerim = _SimplifyOuterPerim(outerPerim);
        this.holes = this._GetContainedHoles(potentialHoles);
        this.bridges = bridges;
        
        if (!GeometryFuncs.IsPolygonCCW(this.outerPerim))
        {
            List<Vector2> reversePerim = this.outerPerim.ToList();
            reversePerim.Reverse();
            this.outerPerim = reversePerim.ToArray();
        }
        foreach (List<Vector2> hole in this.holes)
        {
            if (!GeometryFuncs.IsPolygonCCW(hole.ToArray()))
            {
                hole.Reverse();
            }
        }
    }

    /// <summary>
    /// Simplifies outer perimeter using EdgeCollection's simplification algorithm to
    /// merge collinear segments together.
    /// </summary>
    /// <param name="outerPerimUnsimplified"></param>
    /// <returns></returns>
    private static Vector2[] _SimplifyOuterPerim(Vector2[] outerPerimUnsimplified)
    {
        var edgeCollection = new EdgeCollection<PolyEdge>();
        for (int i = 0; i < outerPerimUnsimplified.Length; i++)
        {
            Vector2 thisVertex = outerPerimUnsimplified[i];
            Vector2 nextVertex = outerPerimUnsimplified[(i + 1) % outerPerimUnsimplified.Length];
            if (thisVertex == nextVertex) continue;
            var polyEdge = new PolyEdge(thisVertex, nextVertex);
            edgeCollection.Add(polyEdge);
        }
        List<Vector2> simplified = edgeCollection.GetSimplifiedPerim();
        return simplified.ToArray();
    }

    /// <summary>
    /// Checks which potential holes could be contained within this polygon and adds them
    /// to holes if they pass the IsPolyInPoly check.
    /// </summary>
    /// <param name="potentialHoles"></param>
    /// <returns></returns>
    private List<Vector2>[] _GetContainedHoles(List<Vector2>[] potentialHoles)
    {
        var confirmedHoles = new List<List<Vector2>>();
        foreach (List<Vector2> hole in potentialHoles)
        {
            if (GeometryFuncs.IsPolyInPoly(hole.ToArray(), this.outerPerim))
            {
                confirmedHoles.Add(hole);
            }
            else if (_DoesHoleShareVertex(hole))
            {
                bool holeVertexInsidePoly = false;
                foreach (Vector2 holeVertex in hole)
                {
                    if (GeometryFuncs.IsPointInPoly(holeVertex, this.outerPerim))
                    {
                        holeVertexInsidePoly = true;
                        break;
                    }
                }
                if (holeVertexInsidePoly) confirmedHoles.Add(hole);
            }
        }
        return confirmedHoles.ToArray();
    }

    /// <summary>
    /// Checks if some input hole shares a vertex with this polygon.
    /// </summary>
    /// <param name="hole"></param>
    /// <returns>True if hole shares a vertex with this polygon.</returns>
    private bool _DoesHoleShareVertex(List<Vector2> hole)
    {
        foreach (Vector2 thisVertex in this.outerPerim)
        {
            foreach (Vector2 holeVertex in hole)
            {
                if (thisVertex == holeVertex) return true;
            }
        }
        return false;
    }
}
}