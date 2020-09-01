using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using EFSMono.Common.Autoload;
using EFSMono.Common.DataStructures.Geometry;
using Godot;

namespace EFSMono.Common.DataStructures.Graphs.PolygonSplittingGraphObjects
{
    /// <summary>
    /// A class that represents a group of nodes that have connected edges to each other.
    /// </summary>
    public class ConnectedNodeGroup
    {
        public int id { get; }
        public SortedList<int, PolygonSplittingGraphNode> nodes { get; }
        public List<PolygonSplittingGraphNode> outerPerimNodes { get; }
        private readonly Vector2[] _outerPerimSimplified;
        public ImmutableArray<Vector2> outerPerimSimplified { get { return ImmutableArray.Create(this._outerPerimSimplified); } }
        public Dictionary<int, HashSet<int>> bridges { get; }

        private const int NIL = -1;
        private const int NUM_SIDES = 4;
        private readonly SortedDictionary<PolygonSplittingGraphNode, int> _xySortedNodes;
        private readonly Dictionary<int, List<int>> _ccwAdjOrdering;

        public ConnectedNodeGroup(int id, SortedList<int, PolygonSplittingGraphNode> nodes,
                                  Dictionary<int, HashSet<int>> bridges = null)
        {
            if (bridges == null) bridges = new Dictionary<int, HashSet<int>>();
            this.id = id;
            this.nodes = nodes;
            this._ccwAdjOrdering = this._ComputeCCWAdjacencyOrdering();
            this._xySortedNodes = new SortedDictionary<PolygonSplittingGraphNode, int>();
            foreach (KeyValuePair<int, PolygonSplittingGraphNode> idToNode in this.nodes)
            {
                this._xySortedNodes.Add(idToNode.Value, idToNode.Key);
            }
            this.outerPerimNodes = this._FindOuterPerim();
            this._outerPerimSimplified = this._SimplifyOuterPerim();
            this.bridges = bridges;
        }

        private Dictionary<int, List<int>> _ComputeCCWAdjacencyOrdering()
        {
            var localCCWAdjOrdering = new Dictionary<int, List<int>>();
            foreach (PolygonSplittingGraphNode node in this.nodes.Values)
            {
                if (node.connectedNodeIDs.Count > 2)
                { //no point ordering anything with 2 or less connections
                    localCCWAdjOrdering[node.id] = Enumerable.Repeat(NIL, NUM_SIDES).ToList();
                    var origin = new Vector2(node.x, node.y);
                    foreach (int connID in node.connectedNodeIDs)
                    {
                        if (!this.nodes.ContainsKey(connID)) continue;
                        Vector2 connDir = (new Vector2(this.nodes[connID].x, this.nodes[connID].y) - origin).Normalized();
                        int connVal = Globals.GetCCWVectorValue(connDir);
                        if (connVal != -1) localCCWAdjOrdering[node.id][connVal] = connID;
                    }
                    localCCWAdjOrdering[node.id].RemoveAll(x => x == NIL);
                }
                else
                {
                    localCCWAdjOrdering[node.id] = new List<int>();
                    localCCWAdjOrdering[node.id].AddRange(node.connectedNodeIDs);
                }
            }
            return localCCWAdjOrdering;
        }

        /// <summary>
        /// Sorts two PolygonSplittingGraphNode IDs by their CCW orientation around some origin coordinate.
        /// The order is North is always first, followed by West, South, East, etc.
        /// </summary>
        /// <param name="aID">ID of first node.</param>
        /// <param name="bID">ID of second node.</param>
        /// <param name="origin">Coordinate of node that A and B are being ordered around.</param>
        /// <returns>Less than 0 if A is of a lower 'value' direction than B, more than 0 if the opposite, and 0 if they are
        /// the same direction.</returns>
#pragma warning disable IDE0051 // Remove unused private members
        private int _SortCCWAdjacent(int aID, int bID, Vector2 origin)
#pragma warning restore IDE0051 // Remove unused private members
        {
            Vector2 aDir = (new Vector2(this.nodes[aID].x, this.nodes[aID].y) - origin).Normalized();
            Vector2 bDir = (new Vector2(this.nodes[bID].x, this.nodes[bID].y) - origin).Normalized();
            int aValue = Globals.GetCCWVectorValue(aDir);
            int bValue = Globals.GetCCWVectorValue(bDir);

            return aValue - bValue;
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
        private List<PolygonSplittingGraphNode> _FindOuterPerim()
        {
            PolygonSplittingGraphNode startVertex = this._xySortedNodes.First().Key;
            var localOuterPerim = new List<PolygonSplittingGraphNode> { startVertex };
            Vector2 bearing = Globals.NorthVec2;
            PolygonSplittingGraphNode currentVertex = startVertex;
            do
            {
                var validNeighbours = new HashSet<PolygonSplittingGraphNode>();
                foreach (int neighbourID in currentVertex.connectedNodeIDs.Where(neighbourID => this.nodes.ContainsKey(neighbourID)))
                {
                    validNeighbours.Add(this.nodes[neighbourID]);
                }
                PolygonSplittingGraphNode nextNeighbour = _ChooseNextNeighbour(currentVertex, bearing, validNeighbours);
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
        private static PolygonSplittingGraphNode _ChooseNextNeighbour(PolygonSplittingGraphNode currentVertex, Vector2 bearing,
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

        public override int GetHashCode()
        {
            return this.id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return this._Equals((ConnectedNodeGroup)obj);
        }

        private bool _Equals(ConnectedNodeGroup other)
        {
            return this.id == other.id && this.nodes.Equals(other.nodes) && this.outerPerimNodes.Equals(other.outerPerimNodes);
        }

        /// <summary>
        /// Checks if another ConnectedNodeGroup shares a vertex (or vertices) with this group, IFF the shared vertex nodes
        /// have different IDs.
        /// </summary>
        /// <param name="otherGroup"></param>
        /// <returns>A HashSet of Vector2s of the vertices that appear in both groups but with different IDs.</returns>
        public HashSet<Vector2> GetSharedVertices(ConnectedNodeGroup otherGroup)
        {
            if (otherGroup is null) throw new ArgumentNullException(nameof(otherGroup));

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
        /// Uses the <param>currentNodeID</param>'s CCW ordering to select the node ID immediately after
        /// <param>prevNodeID</param>. If currentNodeID is not part of this ConnectedNodeGroup, or <param>prevNodeID</param>
        /// is not a connection in <param>currentNodeID</param>'s CCW ordering, returns -1.
        /// </summary>
        /// <param name="prevNodeID"></param>
        /// <param name="currentNodeID"></param>
        /// <returns></returns>
        public int GetCCWNextNodeID(int prevNodeID, int currentNodeID)
        {
            if (!this._ccwAdjOrdering.ContainsKey(currentNodeID)) return NIL;
            List<int> currentCCWConns = this._ccwAdjOrdering[currentNodeID];
            int nextNodeID = -1;
            for (int i = 0; i < currentCCWConns.Count; i++)
            {
                if (prevNodeID == currentCCWConns[i])
                {
                    nextNodeID = currentCCWConns[(i + 1) % currentCCWConns.Count];
                    break;
                }
            }
            return nextNodeID;
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
                return GeometryFuncs.IsPolyInPoly(otherGroup.outerPerimSimplified, this.outerPerimSimplified);
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