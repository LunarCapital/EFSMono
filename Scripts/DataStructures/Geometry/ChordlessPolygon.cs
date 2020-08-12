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
    public bool isHole { get;  }

    public ChordlessPolygon(Vector2[] outerPerim, List<Vector2>[] potentialHoles,
                            Dictionary<Vector2, HashSet<Vector2>> bridges, bool isHole)
    {
        this.isHole = isHole;
        this.outerPerimUnsimplified = outerPerim;
        this.outerPerim = _SimplifyOuterPerim(outerPerim);
        this.holes = (!this.isHole) ? this._GetContainedHoles(potentialHoles) : new List<Vector2>[0];
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
            HashSet<Vector2> sharedVertices;
            if (GeometryFuncs.IsPolyInPoly(hole.ToArray(), this.outerPerim))
            {
                confirmedHoles.Add(hole);
            }
            else if ((sharedVertices = this._GetHoleSharedVertices(hole)).Count > 0)
            {
                int holeVerticesInPoly = 0; //guilty until proven innocent, to prevent snake poly from containing hole
                foreach (Vector2 holeVertex in hole)
                {
                    if (sharedVertices.Contains(holeVertex)) continue;
                    if (GeometryFuncs.IsPointInPoly(holeVertex, this.outerPerim) &&
                        !GeometryFuncs.IsPointOnPolyBoundary(holeVertex, this.outerPerim))
                    {
                        holeVerticesInPoly++;
                    }
                }
                if (holeVerticesInPoly > 0) confirmedHoles.Add(hole);
            }
        }
        return confirmedHoles.ToArray();
    }

    /// <summary>
    /// Checks if some input hole shares a vertex with this polygon.
    /// </summary>
    /// <param name="hole"></param>
    /// <returns>True if hole shares a vertex with this polygon.</returns>
    private HashSet<Vector2> _GetHoleSharedVertices(List<Vector2> hole)
    {
        var containedHoles = new HashSet<Vector2>();
        foreach (Vector2 thisVertex in this.outerPerim)
        {
            foreach (Vector2 holeVertex in hole.Where(holeVertex => thisVertex == holeVertex))
            {
                containedHoles.Add(holeVertex);
            }
        }
        return containedHoles;
    }
}
}