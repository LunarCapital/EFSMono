using Godot;
using System;
using SCol = System.Collections.Generic;

namespace TileProcessorNamespace
{
/// <summary>
/// <para>Edge Collection class which holds a group of edges that may or may not form a closed loop.
/// Holds functions specifically for a collection of edges, such as sorting them in order,
/// 'smoothing' them (merging collinear edges), etc.
/// Thank god I can extend lists in C#!
/// </para>
/// </summary>
public class EdgeCollection : SCol.List<Edge>
{

    //CONSTRUCTORS
    public EdgeCollection() {}
    public EdgeCollection(SCol.IEnumerable<Edge> collection)
    {
        foreach (Edge edge in collection)
        {
            this.Add(edge);
        }
    }

    public EdgeCollection Clone()
    {
        return (EdgeCollection)base.MemberwiseClone();
    }

    /// <summary>
    /// Returns true if this EdgeCollection forms a closed loop, false if open loop.
    /// If empty, returns false (cannot have a loop without vertices).
    /// Assumes ordered.  If not ordered, function will not work properly.
    /// </summary>
    /// <returns>Boolean, true if closed loop, false if open loop.</returns>
    public bool IsClosed()
    {
        if (this.Count == 0)
        {
            return false;
        } else
        {
                return this[0].a == this[this.Count - 1].b;
            }
    }

    /// <summary>
    /// Checks if EdgeCollection contains an Edge with the same points as the input Edge (note that
    /// this does not mean that they are the same Edge object).
    /// </summary>
    /// <param name="edge">Input edge that this func looks for.</param>
    /// <returns>True if contains edge with matching points, false otherwise.</returns>
    public bool HasEdgePoints(Edge edge)
    {
        foreach (Edge searchEdge in this)
        {
            if (searchEdge.IsIdentical(edge) || searchEdge.GetReverseEdge().IsIdentical(edge))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns a set of this EdgeCollection, AKA without duplicate edges.
    /// </summary>
    /// <returns>The same EdgeCollection but without duplicates</returns>
    public EdgeCollection GetSetCollection()
    {
        var hashSet = new SCol.HashSet<Edge>();
        var edgeSet = new EdgeCollection();
        foreach (Edge edge in this)
        {
            if (!hashSet.Contains(edge))
            {
                hashSet.Add(edge);
                edgeSet.Add(edge);
            }
        }
        return edgeSet;
    }

    /// <summary>
    /// Get a clone of this class but without any edges that appear in the input collection.
    /// </summary>
    /// <param name="excludeCollection">Collection of edges to exclude from this one.</param>
    /// <returns>This collection but without any edges that are in excludeCollection.</returns>
    public EdgeCollection GetExcludedCollection(EdgeCollection excludeCollection)
    {
        EdgeCollection cloneCollection = this.Clone();
        foreach (Edge edge in excludeCollection)
        {
            cloneCollection.Remove(edge);
        }
        return cloneCollection;
    }

    /// <summary>
    /// Attempts to order the edges contained within this collection in CCW order.  IF there are multiple
    /// 'groups' of edges, AKA some edges are disconnected from each other, only ONE group will be ordered
    /// and returned.
    /// </summary>
    /// <returns>The same EdgeCollection but with ordered edges, and without disconnected groups</returns>
    public EdgeCollection GetOrderedCollection()
    {
        var orderedList = new SCol.LinkedList<Edge>();
        this._ResetCheckedEdges();
        SCol.List<Edge> unorderedEdges = this._GetUncheckedEdges();

        while (orderedList.Count < this.Count)
        {
            if (orderedList.Count == 0) //just began ordering, throw in the first edge we find
            {
                orderedList.AddFirst(unorderedEdges[0]);
                unorderedEdges[0].isChecked = true;
            }
            else
            {
                Edge backEdge = orderedList.Last.Value;
                Edge frontEdge = orderedList.First.Value.GetReverseEdge(); //because GrabSharedEdge uses edge.b to find connected edge
                int edgeConnToBackID = this._GrabConnectedEdgeID(unorderedEdges, backEdge);
                int edgeConnToFrontID = this._GrabConnectedEdgeID(unorderedEdges, frontEdge);

                if (edgeConnToBackID != -1)
                {
                    orderedList = this._AddBackEdge(orderedList, backEdge, unorderedEdges[edgeConnToBackID]);
                    unorderedEdges[edgeConnToBackID].isChecked = true;
                }
                if (edgeConnToFrontID != -1 && edgeConnToFrontID != edgeConnToBackID)
                { //ensure we don't add the same edge twice
                    orderedList = this._AddFrontEdge(orderedList, frontEdge, unorderedEdges[edgeConnToFrontID]);
                    unorderedEdges[edgeConnToFrontID].isChecked = true;
                }
                if (edgeConnToBackID == -1 && edgeConnToFrontID == -1)
                {
                    break; // no longer possible to order edges as there is a disconnection between them
                }
            }
        }
        return new EdgeCollection(orderedList);
    }

    /// <summary>
    /// <para>
    /// Makes and returns a list of Vector2s that contain only the vertices of the shape
    /// that the edges in this class make. In other words, adjacent collinear edges are merged
    /// into a single edge.
    /// This function relies on the following assumptions:
    ///     1. The edges within this collection are ordered
    ///     2. There are no disconnected edges within this collection
    /// If either of these are not true, this function will return an empty list.
    ///
    /// On the other hand, it is okay if the edges within this collection DO NOT make a closed loop.
    /// To indicate whether the shape made by these edges is open or closed, the last coordinate point
    /// in the returned list will equal the first coordinate IFF closed.
    /// </para>
    /// </summary>
    /// <returns>A list of Vector2 vertices describing the shape made by the edges in this
    /// colltion, OR an empty list if the edges are unordered or disconnected.</returns>
    public SCol.List<Vector2> GetSimplifiedPerim()
    {
        var simplifiedPerim = new SCol.List<Vector2>();

        foreach (Edge edge in this)
        {
            if (simplifiedPerim.Count == 0) //nothing added yet, just throw in both points
            {
                simplifiedPerim.Add(edge.a);
                simplifiedPerim.Add(edge.b);
            } else
            {
                if (edge.a != simplifiedPerim[simplifiedPerim.Count - 1])
                { //UNORDERED OR DISCONNECTED EDGES
                    return new SCol.List<Vector2>(); // return empty arr
                }
                if (simplifiedPerim.Count == this.Count) //we're now looking at the last edge
                {
                    simplifiedPerim = this._ProcessFinalExtension(simplifiedPerim, edge);
                }
                else
                {
                    simplifiedPerim = this._ProcessExtension(simplifiedPerim, edge);
                }
            }
        }
        return simplifiedPerim;
    }

    ///////////////////////////
    ///////////////////////////
    ////PRIVATE FUNCS BELOW////
    ///////////////////////////
    ///////////////////////////

    /// <summary>
    /// Sets all edges' isChecked property within this class to false.
    /// </summary>
    private void _ResetCheckedEdges()
    {
        foreach (Edge edge in this)
        {
            edge.isChecked = false;
        }
    }

    /// <summary>
    /// Places all unchecked edges within this class into a new list.
    /// </summary>
    /// <returns>A list with all the unchecked edges within this class</returns>
    private SCol.List<Edge> _GetUncheckedEdges()
    {
        var uncheckedEdges = new SCol.List<Edge>();
        foreach (Edge edge in this)
        {
            if (!edge.isChecked)
            {
                uncheckedEdges.Add(edge);
            }
        }
        return uncheckedEdges;
    }

    /// <summary>
    /// Given some input list of edges AND a reference edge, looks for the index of an edge in
    /// the list that shares a coordinate point with the reference edge.b
    /// If we cannot find an edge that connects to the reference edge, we return -1.
    /// </summary>
    /// <param name="searchList">List of edges that we look for a connecting edge in</param>
    /// <param name="referenceEdge">Edge we are trying to find a connecting edge to</param>
    /// <returns>Index of an edge that connects to the reference edge's point B</returns>
    private int _GrabConnectedEdgeID(SCol.List<Edge> searchList, Edge referenceEdge)
    {
        for (int i = 0; i < searchList.Count; i++)
        {
            if (searchList[i].isChecked || searchList[i].IsIdentical(referenceEdge))
            { //ignore already ordered/identical edges
                continue;
            } else if (searchList[i].a == referenceEdge.b || searchList[i].b == referenceEdge.b)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Adds an edge that connects to the orderedList's last edge.  If needed, reverses the new
    /// edge so that its point A matches the orderedList's last edge's point B.
    /// </summary>
    /// <param name="orderedList">List that this function adds to</param>
    /// <param name="backEdge">The current last edge</param>
    /// <param name="edgeConnToBack">The edge connecting to the last edge</param>
    /// <returns>orderedList but with the new edge appended to its back</returns>
    private SCol.LinkedList<Edge> _AddBackEdge(SCol.LinkedList<Edge> orderedList, Edge backEdge, Edge edgeConnToBack)
    {
        var orderedListClone = new SCol.LinkedList<Edge>(orderedList); //clone seems redundant but we're following the mutability principle
        if (edgeConnToBack.a == backEdge.b)
        {
            orderedListClone.AddLast(edgeConnToBack);
        } else
        { // make sure we add in the edge the right way
            orderedListClone.AddLast(edgeConnToBack.GetReverseEdge());
        }
        return orderedListClone;
    }

    /// <summary>
    /// Adds an edge that connects to the orderedList's first edge.  If needed, reverses the new
    /// edge so that its point B matches the orderedList's first edge's point A.
    /// </summary>
    /// <param name="orderedList">List that this function adds to</param>
    /// <param name="frontEdge"></param>
    /// <param name="edgeConnToFront"></param>
    /// <returns></returns>
    private SCol.LinkedList<Edge> _AddFrontEdge(SCol.LinkedList<Edge> orderedList, Edge frontEdge, Edge edgeConnToFront)
    {
        var orderedListClone = new SCol.LinkedList<Edge>(orderedList);
        if (edgeConnToFront.b == frontEdge.b)
        {
            orderedListClone.AddFirst(edgeConnToFront);
        }
        else
        { //note that for front list conditions are reversed compared to back list
            orderedListClone.AddFirst(edgeConnToFront.GetReverseEdge());
        }
        return orderedListClone;
    }

    /// <summary>
    /// Attempts to add the points of an edge to the simplifedPerim list, which aims to only hold vertices.
    /// If the edge being added is collinear to the previously added edge, the most recent Vector2 in simplifedPerim
    /// is extended.  If the edge being added is not collinear, we append a new vertice instead.
    /// We do not worry about the last edge in this function, which is a special case.
    /// </summary>
    /// <param name="simplifiedPerim">List containing prev edge that this func wants to look at</param>
    /// <param name="extendingEdge">The edge being added, possible extension of previous edge </param>
    /// <returns>A clone of the passed-in List parameter with the new edge appended or extended</returns>
    private SCol.List<Vector2> _ProcessExtension(SCol.List<Vector2> simplifiedPerim, Edge extendingEdge)
    {
        var simplifiedPerimClone = new SCol.List<Vector2>(simplifiedPerim);
        Vector2 previousA = simplifiedPerimClone[simplifiedPerimClone.Count - 2];
        Vector2 previousB = simplifiedPerimClone[simplifiedPerimClone.Count - 1];
        Vector2 previousSlope = previousB - previousA.Normalized();
        Vector2 currentSlope = (extendingEdge.b - extendingEdge.a).Normalized();

        if (previousSlope == currentSlope)
        { //collinear, extend
            simplifiedPerimClone[simplifiedPerimClone.Count - 1] = extendingEdge.b;
        } else
        { //not collinear, append
            simplifiedPerimClone.Add(extendingEdge.b);
        }
        return simplifiedPerimClone;
    }

    /// <summary>
    /// Checks if any changes need to be made to simplifiedPerim due to the last un-added edge, which are reliant
    /// on the following conditions:
    ///     1. Last edge is extension of previously added edge, so we extend previous edge to last edge.b
    ///     2. Last edge is extension of FIRST edge, so we extend first edge's first point to last edge.a
    ///     3. Last edge is extension of both previous and first edge, in which case we extend previous to first edge
    /// Also ensures that the last Vector2 in simplifiedPerim == the first Vector2 if edges form a closed loop, and
    /// that the last Vector2 != first Vector2 if edges form an open loop.
    /// "couldn't u hav merged this with the previous function"
    /// "yes but it would look like shit because it would be 100+ lines"
    /// </summary>
    /// <param name="simplifiedPerim">List containing prev and first edge that this func wants to look at</param>
    /// <param name="finalEdge">The edge being added, possible extension of previous AND first edge</param>
    /// <returns>Clone of the passed-in List parameter with the final edge appended/extended</returns>
    private SCol.List<Vector2> _ProcessFinalExtension(SCol.List<Vector2> simplifiedPerim, Edge finalEdge)
    {
        var simplifiedPerimClone = new SCol.List<Vector2>(simplifiedPerim);
        Vector2 previousA = simplifiedPerimClone[simplifiedPerimClone.Count - 2];
        Vector2 previousB = simplifiedPerimClone[simplifiedPerimClone.Count - 1];
        Vector2 firstA = simplifiedPerimClone[0];
        Vector2 firstB = simplifiedPerimClone[1]; //fear not friend mine algorithm hath ensured that at all times these hardcoded indices will forever be present. amen
        Vector2 previousSlope = (previousB - previousA).Normalized();
        Vector2 firstSlope = (firstB - firstA).Normalized();
        Vector2 finalSlope = (finalEdge.b - finalEdge.a).Normalized();

        if (finalEdge.b == firstA)
        { //CLOSED LOOP
            if (previousSlope == finalSlope && firstSlope == finalSlope)
            { //extend previous TO first edge
                simplifiedPerimClone[0] = previousA;
                simplifiedPerimClone.RemoveAt(simplifiedPerimClone.Count - 1);
            }
            else if (previousSlope == finalSlope)
            { //extend previous TO final edge
                simplifiedPerimClone[simplifiedPerimClone.Count - 1] = firstA;
            }
            else if (firstSlope == finalSlope)
            { //extend first TO final edge
                simplifiedPerimClone[0] = previousB;
            }
            else
            { //extend nothing. add first edge's point A because loop is closed
                simplifiedPerimClone.Add(firstA);
            }
        }
        else
        { //OPEN LOOP
            if (previousSlope == finalSlope)
            { //extend previous TO final edge
                simplifiedPerimClone[simplifiedPerimClone.Count - 1] = finalEdge.b;
            }
            else
            { //extend nothing, add final edge's point B because loop is open
                simplifiedPerimClone.Add(finalEdge.b);
            }
        }
        return simplifiedPerimClone;
    }

}
}
