using System;
using System.Collections.Generic;
using EFSMono.Common.Autoload;
using EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects.Exceptions;
using Godot;

namespace EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects
{
    /// <summary>
    /// A class used to describe the perimeter of a single tile via its four edges.
    /// </summary>
    public partial class TilePerim
    {
        public const int UNCOLORED = -1;
        private const int TILE_VERTICES_NUM = 4;

        private TileEdge north { get; set; }
        private TileEdge east { get; set; }
        private TileEdge south { get; set; }
        private TileEdge west { get; set; }
        public int color { get; set; }
        public int id { get; }

        public TilePerim(IReadOnlyList<Vector2> vertices, Vector2 cell, int id)
        {
            if (vertices is null) throw new ArgumentNullException(nameof(vertices));

            this.color = UNCOLORED;
            this.id = id;
            if (vertices.Count != TILE_VERTICES_NUM)
            {
                throw new VerticesArraySizeMismatchException("Attempted to construct TilePerim with vertices array that does not have a size of 4.");
            }
            else
            {
                this._InitEdges(vertices, cell);
                if (this.north is null || this.east is null || this.south is null || this.west is null)
                {
                    throw new NullReferenceException("One of an edge's sides is null. N: " + this.north + ", E: " + this.east + ", S: " + this.south + ", W: " + this.west);
                }
            }
        }

        public override int GetHashCode()
        {
            int hashCode = this.id.GetHashCode();
            return hashCode;
        }

        public TilePerim Clone()
        {
            return (TilePerim)this.MemberwiseClone();
        }

        /// <summary>
        /// Put this tile's four edges in a block array and return it.
        /// </summary>
        /// <returns>Block array containing this tile's four edges</returns>
        public TileEdge[] GetEdgesArray()
        {
            var edges = new TileEdge[4];
            edges[(int)Globals.CWSide.North] = this.north;
            edges[(int)Globals.CWSide.East] = this.east;
            edges[(int)Globals.CWSide.South] = this.south;
            edges[(int)Globals.CWSide.West] = this.west;
            return edges;
        }

        public bool IsTileAdjacent(TilePerim comparisonTile)
        {
            if (comparisonTile is null) throw new ArgumentNullException(nameof(comparisonTile));

            TileEdge[] thisEdges = this.GetEdgesArray();
            TileEdge[] comparisonEdges = comparisonTile.GetEdgesArray();
            foreach (TileEdge thisEdge in thisEdges)
            {
                foreach (TileEdge comparisonEdge in comparisonEdges)
                {
                    if (thisEdge.IsIdentical(comparisonEdge))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Based off an array off four vertices, sets this class's N/E/S/W edges accordingly. Note that CCWSide is used to
        /// get tile indexes, because Godot stores segments in CCW order, but TileEdges are mapped to CWSide because they
        /// are ordered NESW for later (I know this is confusing). It would technically be cleaner to just use CCWSide all
        /// the time but my brain can't dissociate NESW from their natural order.
        /// </summary>
        /// <param name="vertices">Array of four vertices that make up a tile.</param>
        /// <param name="cell">Cell coordinates of tile.</param>
        private void _InitEdges(IReadOnlyList<Vector2> vertices, Vector2 cell)
        {
            int topIndex = _GetTopIndex(vertices);
            for (int i = 0; i < vertices.Count; i++)
            { //assumes that tile vertices order is CCW, which it is in Godot 3.2
                int rawIndex = (i + topIndex);
                Vector2 vertexA = vertices[rawIndex % vertices.Count];
                Vector2 vertexB = vertices[(rawIndex + 1) % vertices.Count];
                bool invalidSide = false;

                //check the index of point B
                switch ((rawIndex + 1) % vertices.Count)
                {
                    case (int)Globals.CCWSide.West:
                        this.west = new TileEdge(vertexA, vertexB, cell, (int)Globals.CWSide.West);
                        break;

                    case (int)Globals.CCWSide.South:
                        this.south = new TileEdge(vertexA, vertexB, cell, (int)Globals.CWSide.South);
                        break;

                    case (int)Globals.CCWSide.East:
                        this.east = new TileEdge(vertexA, vertexB, cell, (int)Globals.CWSide.East);
                        break;

                    case (int)Globals.CCWSide.North:
                        this.north = new TileEdge(vertexA, vertexB, cell, (int)Globals.CWSide.North);
                        break;

                    default:
                        invalidSide = true;
                        break;
                }
                if (invalidSide)
                {
                    throw new InvalidSideException("Attempted to construct TilePerim with an edge of invalid side (non-NESW)");
                }
            }
        }

        /// <summary>
        /// Finds the index of the vertices array that contains the top coordinate.
        /// </summary>
        /// <param name="vertices">Array of four vertices describing a tile</param>
        /// <returns>Index of vertices array that contains top coordinate</returns>
        private static int _GetTopIndex(IReadOnlyList<Vector2> vertices)
        {
            int topIndex = 0;
            float minY = float.PositiveInfinity;
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 vertex = vertices[i];
                if (vertex.y < minY)
                {
                    minY = vertex.y;
                    topIndex = i;
                }
            }
            return topIndex;
        }
    }
}