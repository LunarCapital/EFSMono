using System;
using System.Collections.Generic;
using EFSMono.Scripts.Autoload;
using EFSMono.Scripts.DataStructures.Geometry;
using Godot;

namespace EFSMono.Scripts.SystemModules.TileProcessorModule.TileProcessorObjects
{
/// <summary>
/// A class used to describe the perimeter of a single tile via its four edges.
/// </summary>
public class TilePerim
{

    public const int UNCOLORED = -1;
    private const int TILE_VERTICES_NUM = 4;
    /// <summary>
    /// CCW_SIDE is the same as Globals.SIDE except WEST and EAST are reversed.
    /// This is because Globals.SIDE has NESW in traditional order, while Godot stores
    /// tile vertices in CCW order.
    /// </summary>
    private enum CCWSide {WEST = 1, SOUTH = 2, EAST = 3, NORTH = 0};

    private TileEdge north { get; set;}
    private TileEdge east { get; set;}
    private TileEdge south { get; set;}
    private TileEdge west { get; set;}
    public int color { get; set; }
    public int id { get; }


    public TilePerim(IReadOnlyList<Vector2> vertices, Vector2 cell, int id)
    {
        this.color = UNCOLORED;
        this.id = id;
        try
        {
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
        catch (Exception e)
        {
            GD.PrintS("Exception msg: " + e.Message);
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
        edges[(int)Globals.Side.NORTH] = this.north;
        edges[(int)Globals.Side.EAST] = this.east;
        edges[(int)Globals.Side.SOUTH] = this.south;
        edges[(int)Globals.Side.WEST] = this.west;
        return edges;
    }

    public bool IsTileAdjacent(TilePerim comparisonTile)
    {
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
    /// Based off an array off four vertices, sets this class's N/E/S/W edges accordingly.
    /// </summary>
    /// <param name="vertices">Array of four vertices that make up a tile.</param>
    /// <param name="cell">Cell coordinates of tile.</param>
    private void _InitEdges(IReadOnlyList<Vector2> vertices, Vector2 cell)
    {
        int topIndex = _GetTopIndex(vertices);
        for (int i = 0; i < vertices.Count; i++)
        { //assumes that tile vertices order is CCW, which it is in Godot 3.2
            int rawIndex = (i+topIndex);
            Vector2 vertexA = vertices[rawIndex%vertices.Count];
            Vector2 vertexB = vertices[(rawIndex + 1)%vertices.Count];
            bool invalidSide = false;

             //check the index of point B
            switch ((rawIndex + 1)%vertices.Count)
            {
                case (int)CCWSide.WEST:
                    this.west = new TileEdge(vertexA, vertexB, cell, (int)Globals.Side.WEST);
                    break;
                case (int)CCWSide.SOUTH:
                    this.south = new TileEdge(vertexA, vertexB, cell, (int)Globals.Side.SOUTH);
                    break;
                case (int)CCWSide.EAST:
                    this.east = new TileEdge(vertexA, vertexB, cell, (int)Globals.Side.EAST);
                    break;
                case (int)CCWSide.NORTH:
                    this.north = new TileEdge(vertexA, vertexB, cell, (int)Globals.Side.NORTH);
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
                topIndex =i ;
            }
        }
        return topIndex;
    }

    [Serializable]
    private class VerticesArraySizeMismatchException : Exception
    {
        /// <summary>
        /// An exception to be invoked if this class is constructed with an edges array that
        /// does not have a size of 4.
        /// </summary>
        public VerticesArraySizeMismatchException(string message) : base(message) {}
            public VerticesArraySizeMismatchException()
            {
            }
            public VerticesArraySizeMismatchException(string message, Exception innerException) : base(message, innerException)
            {
            }
    }

    [Serializable]

    private class InvalidSideException : Exception
    {
        /// <summary>
        /// An exception to be invoked if this class's constructor attempts to initialise
        /// an edge with a side that does match up with the Globals side enum (NESW)
        /// </summary>
        public InvalidSideException(string message) : base(message) {}
            public InvalidSideException()
            {
            }
            public InvalidSideException(string message, Exception innerException) : base(message, innerException)
            {
            }
    }

}
}