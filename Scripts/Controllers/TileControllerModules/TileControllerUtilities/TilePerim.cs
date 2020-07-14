using Godot;
using System;
using SCol = System.Collections.Generic;

namespace TileControllerNamespace
{
/// <summary>
/// A class used to describe the perimeter of a single tile via its four edges.
/// </summary>
public class TilePerim : Reference
{

    public const int UNCOLORED = -1;
    private const int TILE_VERTICES_NUM = 4;

    public Edge north { get; private set;}
    public Edge east { get; private set;}
    public Edge south { get; private set;}
    public Edge west { get; private set;}
    public int color { get; set; }
    public int id { get; set;}
    private readonly Vector2 _tileCoords; //Currently unused, remove if unneeded for ledges calcs

    public TilePerim(Vector2[] vertices, Vector2 cell, int id)
    {
        this.color = UNCOLORED;
        this._tileCoords = cell;
        this.id = id;
        try
        {
            if (vertices.Length != TILE_VERTICES_NUM)
            {
                throw new _VerticesArraySizeMismatchException("Attempted to construct TilePerim with vertices array that does not have a size of 4.");
            }
            else
            {
                this._InitEdges(vertices, cell);
                if (north is null || east is null || south is null || west is null)
                {
                    throw new NullReferenceException("One of an edge's sides is null. N: " + north + ", E: " + east + ", S: " + south + ", W: " + west);
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
        return (TilePerim)base.MemberwiseClone();
    }

    /// <summary>
    /// Put this tile's four edges in a block array and return it.
    /// </summary>
    /// <returns>Block array containing this tile's four edges</returns>
    public Edge[] GetEdgesArray()
    {
        Edge[] edges = new Edge[4];
        edges[(int)Globals.SIDE.NORTH] = this.north;
        edges[(int)Globals.SIDE.EAST] = this.east;
        edges[(int)Globals.SIDE.SOUTH] = this.south;
        edges[(int)Globals.SIDE.WEST] = this.west;
        return edges;
    }

    public bool IsTileAdjacent(TilePerim comparisonTile)
    {
        Edge[] thisEdges = this.GetEdgesArray();
        Edge[] comparisonEdges = comparisonTile.GetEdgesArray();
        foreach (Edge thisEdge in thisEdges)
        {
            foreach (Edge comparisonEdge in comparisonEdges)
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
    /// <param name="edges"></param>
    private void _InitEdges(Vector2[] vertices, Vector2 cell)
    {
        int topIndex = this._GetTopIndex(vertices);
        for (int i = 2*vertices.Length; i > 0; i--) //-- because we go CCW. 2*len so we don't go negative.
        { 
            int rawIndex = (i+topIndex);
            Vector2 vertexA = vertices[rawIndex%vertices.Length];
            Vector2 vertexB = vertices[(rawIndex - 1)%vertices.Length];
            bool invalidSide = false; 

             //check the index of point B
            switch ((rawIndex - 1)%vertices.Length)
            {
                case (int)Globals.SIDE.WEST:
                    this.west = new Edge(vertexA, vertexB, cell, (int)Globals.SIDE.WEST);
                    break;
                case (int)Globals.SIDE.SOUTH:
                    this.south = new Edge(vertexA, vertexB, cell, (int)Globals.SIDE.SOUTH);
                    break;
                case (int)Globals.SIDE.EAST:
                    this.east = new Edge(vertexA, vertexB, cell, (int)Globals.SIDE.EAST);
                    break;
                case (int)Globals.SIDE.NORTH:
                    this.north = new Edge(vertexA, vertexB, cell, (int)Globals.SIDE.NORTH);
                    break;
                default:
                    invalidSide = true;
                    break;
            }
            if (invalidSide)
            {
                throw new _InvalidSideException("Attempted to construct TilePerim with an edge of invalid side (non-NESW)"); 
            }
        }
    }

    /// <summary>
    /// Finds the index of the vertices array that contains the top coordinate.
    /// </summary>
    /// <param name="vertices">Array of four vertices describing a tile</param>
    /// <returns>Index of vertices array that contains top coordinate</returns>
    private int _GetTopIndex(Vector2[] vertices)
    {
        int topIndex = 0;
        float minY = float.PositiveInfinity;
        for (int i = 0; i < vertices.Length; i++)
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
    /// <summary>
    /// An exception to be invoked if this class is constructed with an edges array that
    /// does not have a size of 4.
    /// </summary>
    private class _VerticesArraySizeMismatchException : Exception
    {
        public _VerticesArraySizeMismatchException(string message) : base(message) {}
    }

    [Serializable]
    /// <summary>
    /// An exception to be invoked if this class's constructor attempts to initialise
    /// an edge with a side that does match up with the Globals side enum (NESW)
    /// </summary>
    private class _InvalidSideException : Exception
    {
        public _InvalidSideException(string message) : base(message) {}
    }

}
}