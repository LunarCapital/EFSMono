using System;
using System.Linq;
using EFSMono.Scripts.Autoload;
using EFSMono.Scripts.DataStructures.Geometry;
using Godot;
using System.Collections.Generic;

namespace EFSMono.Scripts.DataStructures.Graphs.PolygonSplittingGraphObjects
{
/// <summary>
/// A class that represents a group of nodes that have connected edges to each other.
/// </summary>
public class ConnectedNodeGroup : IComparable
{
    public int id { get; }
    public SortedList<int, PolygonSplittingGraphNode> nodes { get; }
    private readonly SortedDictionary<PolygonSplittingGraphNode, int> _xySortedNodes;
    public List<PolygonSplittingGraphNode> outerPerimNodes { get; }
    public Vector2[] outerPerimSimplified { get; }
    public Dictionary<int, HashSet<int>> bridges { get; }
    public float area { get; }
    
    public ConnectedNodeGroup(int id, SortedList<int, PolygonSplittingGraphNode> nodes, int[,] adjMatrix,
                              Dictionary<int, HashSet<int>> bridges = null)
    {
        if (bridges == null) bridges = new Dictionary<int, HashSet<int>>();
        this.id = id;
        this.nodes = nodes;
        this._xySortedNodes = new SortedDictionary<PolygonSplittingGraphNode, int>();
        foreach (KeyValuePair<int, PolygonSplittingGraphNode> idToNode in this.nodes)
        {
            this._xySortedNodes.Add(idToNode.Value, idToNode.Key);
        }
        this.outerPerimNodes = this._FindOuterPerim(adjMatrix);
        this.outerPerimSimplified = this._SimplifyOuterPerim();
        this.bridges = bridges;
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
    private List<PolygonSplittingGraphNode> _FindOuterPerim(int[,] adjMatrix)
    {
        PolygonSplittingGraphNode startVertex = this._xySortedNodes.First().Key;
        var localOuterPerim = new List<PolygonSplittingGraphNode>{startVertex};
        Vector2 bearing = Globals.NORTH_VEC2;
        PolygonSplittingGraphNode currentVertex = startVertex;
        do
        {
            var validNeighbours = new HashSet<PolygonSplittingGraphNode>();
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
                                                           HashSet<PolygonSplittingGraphNode> validNeighbours)
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
    private Vector2[] _SimplifyOuterPerim()
    {
        var edgeCollection = new EdgeCollection<PolyEdge>();
        for (int i = 0; i < this.outerPerimNodes.Count; i++)
        {
            var thisCoord = new Vector2(this.outerPerimNodes[i].x, this.outerPerimNodes[i].y);
            var nextCoord = new Vector2(this.outerPerimNodes[(i + 1) % this.outerPerimNodes.Count].x,
                this.outerPerimNodes[(i + 1) % this.outerPerimNodes.Count].y);
            if (thisCoord == nextCoord) continue;
            var polyEdge = new PolyEdge(thisCoord, nextCoord);
            edgeCollection.Add(polyEdge);
        }
        return edgeCollection.GetSimplifiedPerim().ToArray();
    }
    
    /// <summary>
    /// Calculates the area of this group's outer perimeter.
    /// Violates the convention that collections of closed loops has collection[first] = collections[last].
    /// </summary>
    /// <returns>Returns a float of the area.</returns>
    private float _CalculateArea()
    {
        return GeometryFuncs.GetAreaOfPolygon(this.outerPerimSimplified);
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
        return this.id == other.id && this.nodes.Equals(other.nodes) && this.outerPerimNodes.Equals(other.outerPerimNodes);
    }

    public int CompareTo(object obj)
    {
        return this._CompareTo((ConnectedNodeGroup) obj);
    }

    /// <summary>
    /// Checks if another ConnectedNodeGroup shares a vertex (or vertices) with this group, IFF the shared vertex nodes
    /// have different IDs.
    /// </summary>
    /// <param name="otherGroup"></param>
    /// <returns>A HashSet of Vector2s of the vertices that appear in both groups but with different IDs.</returns>
    public HashSet<Vector2> GetSharedVertices(ConnectedNodeGroup otherGroup)
    {
        var sharedVertices = new HashSet<Vector2>();
        foreach (PolygonSplittingGraphNode thisNode in this.outerPerimNodes)
        {
            foreach (PolygonSplittingGraphNode otherNode in otherGroup.outerPerimNodes)
            {
                if (thisNode.x == otherNode.x && thisNode.y == otherNode.y)
                {
                    if (thisNode.id == otherNode.id) continue;
                    sharedVertices.Add(new Vector2(thisNode.x, thisNode.y));
                }
            }
        }
        return sharedVertices;
    }

    /// <summary>
    /// Checks if the other group's outer perimeter is inside this group's perimeter.  Inclusive of the two groups
    /// sharing vertices.
    /// </summary>
    /// <param name="otherGroup"></param>
    /// <returns>True if the other group is inside this group, false otherwise.</returns>
    public bool IsOtherGroupInThisGroup(ConnectedNodeGroup otherGroup)
    {
        HashSet<Vector2> sharedVertices = this.GetSharedVertices(otherGroup);
        if (sharedVertices.Count == 0)
        {
            return GeometryFuncs.IsPolyInPoly(otherGroup.outerPerimSimplified, this.outerPerimSimplified);
        }
        else
        {
            bool isInside = true;
            foreach (PolygonSplittingGraphNode node in otherGroup.nodes.Values)
            {
                var nodeCoord = new Vector2(node.x, node.y);
                if (sharedVertices.Contains(nodeCoord)) continue;
                if (!GeometryFuncs.IsPointInPoly(nodeCoord, this.outerPerimSimplified))
                {
                    isInside = false;
                    break;
                }
            }
            return isInside;
        }
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