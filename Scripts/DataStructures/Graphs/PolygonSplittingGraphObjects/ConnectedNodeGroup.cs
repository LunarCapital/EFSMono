using System;
using System.Linq;
using EFSMono.Scripts.Autoload;
using EFSMono.Scripts.DataStructures.Geometry;
using Godot;
using SCol = System.Collections.Generic;

namespace EFSMono.Scripts.DataStructures.Graphs.PolygonSplittingGraphObjects
{
/// <summary>
/// A class that represents a group of nodes that have connected edges to each other.
/// </summary>
public class ConnectedNodeGroup : IComparable
{
    public int id { get; }
    public SCol.SortedList<int, PolygonSplittingGraphNode> nodes { get; }
    private SCol.SortedDictionary<PolygonSplittingGraphNode, int> _xySortedNodes;
    public SCol.List<PolygonSplittingGraphNode> outerPerim { get; }
    public float area { get; }
    
    public ConnectedNodeGroup(int id, SCol.SortedList<int, PolygonSplittingGraphNode> nodes, int[,] adjMatrix)
    {
        this.id = id;
        this.nodes = nodes;
        this._xySortedNodes = new SCol.SortedDictionary<PolygonSplittingGraphNode, int>();
        foreach (SCol.KeyValuePair<int, PolygonSplittingGraphNode> idToNode in this.nodes)
        {
            this._xySortedNodes.Add(idToNode.Value, idToNode.Key);
        }
        this.outerPerim = this._FindOuterPerim(adjMatrix);
        this._SimplifyOuterPerim();
        this.area = this._CalculateArea();
    }

    /// <summary>
    /// Finds the outer perimeter of the group of nodes in this class by:
    ///     1. Getting the node with the smallest X (and Y if X-value is the same) coordinate.
    ///     2. Defining a bearing as being in the negative Y direction as there are no nodes in that direction.
    ///     3. Out of the valid edges from the current node, picking the one with the least positive CCW angle change
    ///        from the bearing.
    ///     4. Defining a new bearing as being the direction FROM THE NEW node TO THE OLD node.
    ///     5. Repeating until the first node is found again.
    /// As always, the convention for closed loops of Vector2s has list[first] == list[last].
    /// </summary>
    /// <param name="adjMatrix">Adjacency Matrix of nodes.</param>
    /// <returns>List of nodes in CCW order that describe the outer perimeter.</returns>
    private SCol.List<PolygonSplittingGraphNode> _FindOuterPerim(int[,] adjMatrix)
    {
        PolygonSplittingGraphNode startVertex = this._xySortedNodes.First().Key;
        var localOuterPerim = new SCol.List<PolygonSplittingGraphNode>{startVertex};
        Vector2 bearing = Globals.NORTH_VEC2;
        PolygonSplittingGraphNode currentVertex = startVertex;
        do
        {
            var validNeighbours = new SCol.HashSet<PolygonSplittingGraphNode>();
            for (int i = 0; i < adjMatrix.GetLength(1); i++)
            {
                if (adjMatrix[currentVertex.id, i] == 1 && this.nodes.ContainsKey(i))
                {
                    validNeighbours.Add(this.nodes[i]);
                }
            }
            PolygonSplittingGraphNode nextNeighbour = this._ChooseNextNeighbour(currentVertex, bearing, validNeighbours);
            localOuterPerim.Add(nextNeighbour);
            bearing = (new Vector2(currentVertex.x, currentVertex.y) - new Vector2(nextNeighbour.x, nextNeighbour.y)).Normalized();
            currentVertex = nextNeighbour;
        } while (!currentVertex.Equals(startVertex));
        return localOuterPerim;
    }

    /// <summary>
    /// Auxiliary function for _FindOuterPerim.
    /// When called, chooses the next neighbour from the current vertex that should be added to form the outer
    /// perimeter of the polygon represented by the nodes in this class.
    /// </summary>
    /// <param name="currentVertex">Current vertex selected as part of the outer perimeter.</param>
    /// <param name="bearing">Bearing that describes the direction from the current vertex to the previous vertex.</param>
    /// <param name="validNeighbours">A set of valid neighbours </param>
    /// <returns>The next neighbour from the current vertex to make the outer perimeter.</returns>
    private PolygonSplittingGraphNode _ChooseNextNeighbour(PolygonSplittingGraphNode currentVertex, Vector2 bearing,
                                                           SCol.HashSet<PolygonSplittingGraphNode> validNeighbours)
    {
        float minAngle = 360;
        PolygonSplittingGraphNode currentSolution = validNeighbours.First();
        foreach (PolygonSplittingGraphNode neighbour in validNeighbours)
        { //pick the neighbour with the least CCW angle difference
            Vector2 neighbourDirection = (new Vector2(neighbour.x, neighbour.y) - new Vector2(currentVertex.x, currentVertex.y)).Normalized();
            var mirroredBearing = new Vector2(bearing.x, -1 * bearing.y); //MIRROR the y because graphics canvases are ALWAYS MIRRORED, FUCK
            var mirroredNeighbourDirection = new Vector2(neighbourDirection.x, -1 * neighbourDirection.y);
            float angleDiff = Mathf.Rad2Deg(Mathf.Atan2(mirroredBearing.x * mirroredNeighbourDirection.y - mirroredBearing.y * mirroredNeighbourDirection.x,
                                                        mirroredBearing.x * mirroredNeighbourDirection.x + mirroredBearing.y * mirroredNeighbourDirection.y));
            if (angleDiff <= 0) angleDiff += 360;
            if (!(angleDiff < minAngle)) continue;
            minAngle = angleDiff;
            currentSolution = neighbour;
        }
        return currentSolution;
    }

    /// <summary>
    /// Simplifies the outer perim using EdgeCollections simplification algorithm.
    /// </summary>
    private void _SimplifyOuterPerim()
    {
        for (int i = 0; i < this.outerPerim.Count; i++)
        {
            var thisCoord = new Vector2(this.outerPerim[i].x, this.outerPerim[i].y);
            var nextCoord = new Vector2(this.outerPerim[(i+1)% this.outerPerim.Count].x, this.outerPerim[(i+1)% this.outerPerim.Count].y);
            if (thisCoord == nextCoord) continue;
            
        }
    }
    
    /// <summary>
    /// Calculates the area of this group's outer perimeter.
    /// Violates the convention that collections of closed loops has collection[first] = collections[last].
    /// </summary>
    /// <returns>Returns a float of the area.</returns>
    private float _CalculateArea()
    {
        return GeometryFuncs.GetAreaOfPolygon(this.GetOuterPerimAsVectorArray());
    }

    /// <summary>
    /// Returns the outer perimeter as a block array of Vector2s describing its vertices.
    /// Note that we DO NOT follow the convention of closed loops having collection[first] = collection[last] because
    /// this function is likely used in tandem with GeometryFuncs to calculate area/compare polygons.
    /// </summary>
    /// <returns></returns>
    public Vector2[] GetOuterPerimAsVectorArray()
    {
        var outerVertices = new Vector2[this.outerPerim.Count];
        for (int i = 0; i < this.outerPerim.Count; i++)
        {
            PolygonSplittingGraphNode outerNode = this.outerPerim[i];
            outerVertices[i] = new Vector2(outerNode.x, outerNode.y);
        }
        return outerVertices;
    }

    public override int GetHashCode()
    {
        return this.id.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return this.Equals((ConnectedNodeGroup) obj);
    }

    private bool Equals(ConnectedNodeGroup other)
    {
        return this.id == other.id && this.nodes.Equals(other.nodes) && this.outerPerim.Equals(other.outerPerim);
    }

    public int CompareTo(object obj)
    {
        return this._CompareTo((ConnectedNodeGroup) obj);
    }
    
    /// <summary>
    /// Should be sorted in descending order of area size.
    /// </summary>
    /// <param name="other">The comparison object.</param>
    /// <returns></returns>
    private int _CompareTo(ConnectedNodeGroup other)
    {
        if (this.area < other.area)
        {
            return 1;
        } 
        else if (this.area > other.area)
        {
            return -1;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Checks if this class contains the input <param>node</param>.
    /// </summary>
    /// <param name="node"></param>
    /// <returns>True if contained, false otherwise.</returns>
    public bool ContainsNode(PolygonSplittingGraphNode node)
    {
        return this.nodes.ContainsValue(node);
    }

    public int GetSize()
    {
        return this.nodes.Count;
    }
}
}