using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using EFSMono.Common.Autoload;
using Godot;

namespace EFSMono.Common.DataStructures.Geometry
{
    /// <summary>
    /// A class that describes a rectangular irregular polygon that may or may not have holes, but
    /// does not have any chords.
    /// NO CHECKS are done to ensure that the polygon actually has no chords.
    /// As always, closed loop conventions state that for some vector array[first] == array[last].
    /// </summary>
    public class ChordlessPolygon
    {
        private readonly Vector2[] _outerPerim;
        public ImmutableArray<Vector2> outerPerim { get { return ImmutableArray.Create(this._outerPerim); } }

        private readonly Vector2[] _outerPerimUnsimplified;
        public ImmutableArray<Vector2> outerPerimUnsimplified { get { return ImmutableArray.Create(this._outerPerimUnsimplified); } }

        private readonly List<Vector2>[] _holes;
        public ImmutableArray<ImmutableList<Vector2>> holes
        {
            get
            {
                ImmutableArray<ImmutableList<Vector2>>.Builder holesBuilder = ImmutableArray.CreateBuilder<ImmutableList<Vector2>>();
                foreach (List<Vector2> hole in this._holes)
                {
                    holesBuilder.Add(ImmutableList.CreateRange(hole));
                }
                return holesBuilder.ToImmutable();
            }
        }

        private readonly Dictionary<Vector2, HashSet<Vector2>> _bridges;
        public ImmutableDictionary<Vector2, ImmutableHashSet<Vector2>> bridges
        {
            get
            {
                ImmutableDictionary<Vector2, ImmutableHashSet<Vector2>>.Builder bridgesBuilder = ImmutableDictionary.CreateBuilder<Vector2, ImmutableHashSet<Vector2>>();
                foreach (KeyValuePair<Vector2, HashSet<Vector2>> pair in this._bridges)
                {
                    bridgesBuilder.Add(pair.Key, ImmutableHashSet.CreateRange(pair.Value));
                }
                return bridgesBuilder.ToImmutable();
            }
        }
        public bool isHole { get; }

        public ChordlessPolygon(Vector2[] outerPerim, List<Vector2>[] potentialHoles,
                                Dictionary<Vector2, HashSet<Vector2>> bridges, bool isHole)
        {
            if (outerPerim is null) throw new ArgumentNullException(nameof(outerPerim));
            if (potentialHoles is null) throw new ArgumentNullException(nameof(potentialHoles));
            
            this.isHole = isHole;
            this._outerPerimUnsimplified = outerPerim;
            this._outerPerim = _SimplifyOuterPerim(outerPerim);
            this._holes = !this.isHole ? this._GetContainedHoles(potentialHoles) : System.Array.Empty<List<Vector2>>();
            this._bridges = bridges;

            if (!GeometryFuncs.IsPolygonCCW(this._outerPerim))
            {
                var reversePerim = this.outerPerim.ToList();
                reversePerim.Reverse();
                this._outerPerim = reversePerim.ToArray();
            }
            foreach (List<Vector2> hole in this._holes)
            {
                if (!GeometryFuncs.IsPolygonCCW(hole.ToArray()))
                    hole.Reverse();
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
                if (GeometryFuncs.IsPolyInPoly(hole.ToArray(), this._outerPerim))
                    confirmedHoles.Add(hole);
                else if ((sharedVertices = this._GetHoleSharedVertices(hole)).Count > 0)
                {
                    int holeVerticesInPoly = 0; //guilty until proven innocent, to prevent snake poly from containing hole
                    foreach (Vector2 holeVertex in hole)
                    {
                        if (sharedVertices.Contains(holeVertex)) continue;
                        if (GeometryFuncs.IsPointInPoly(holeVertex, this._outerPerim) &&
                            !GeometryFuncs.IsPointOnPolyBoundary(holeVertex, this._outerPerim))
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