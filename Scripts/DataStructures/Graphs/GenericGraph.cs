using System;
using System.Linq;
using Godot;
using System.Collections.Generic;

namespace EFSMono.Scripts.DataStructures.Graphs
{
/// <summary>
/// Generic Graph abstract.
/// Represents a graph via a sorted list of Generic Graph Nodes and an adjacency matrix.
/// </summary>
public abstract class GenericGraph<T> where T : GenericGraphNode
{
    public SortedList<int, T> nodes { get; }
    public int[,] adjMatrix { get; }

    protected GenericGraph(IReadOnlyCollection<T> nodeCollection)
    { 
        this.nodes = new SortedList<int, T>();
        this.adjMatrix = this._ConstructAdjMatrix(nodeCollection);
        foreach (T node in nodeCollection)
        {
            this.nodes[node.id] = node;
        }
        try
        {
            this._CheckValidity(nodeCollection);
        }
        catch (Exception e)
        {
            GD.PrintS(e.Message);
        }
    }

    /// <summary>
    /// DFS over the graph given an input list of nodes to start with, and return all
    /// nodes that were visited during the DFS.
    /// </summary>
    /// <param name="startNodes">List of nodes to start with.</param>
    /// <param name="excludedEdges">Edges to exclude from the DFS.</param>
    /// <returns>List of all nodes visited during the DFS.</returns>
    public List<T> GetDFSCover(List<T> startNodes, Dictionary<int, HashSet<int>> excludedEdges = null)
    {
        if (excludedEdges == null) excludedEdges = new Dictionary<int, HashSet<int>>();
        HashSet<T> visited = startNodes.ToHashSet();
        var stack = new Stack<T>();
        startNodes.ForEach(x => stack.Push(x));

        while (stack.Count > 0)
        {
            T node = stack.Pop();
            
            for (int neighbourID = 0; neighbourID < this.adjMatrix.GetLength(1); neighbourID++)
            {
                if (excludedEdges.ContainsKey(node.id))
                {
                    if (excludedEdges[node.id].Contains(neighbourID)) continue;
                }
                
                if (!visited.Contains(this.nodes[neighbourID]) && adjMatrix[node.id, neighbourID] > 0)
                {
                    visited.Add(this.nodes[neighbourID]);
                    stack.Push(this.nodes[neighbourID]);
                }
            }
        }
        return visited.ToList();
    }

    /// <summary>
    /// Based on the nodes passed into the constructor, creates the adjacency matrix.
    /// </summary>
    /// <param name="nodeCollection">Nodes that will exist in graph.</param>
    /// <returns>2D int array representing the adjacency matrix.</returns>
    protected virtual int[,] _ConstructAdjMatrix(IReadOnlyCollection<T> nodeCollection)
    {
        var localAdjMatrix = new int[nodeCollection.Count, nodeCollection.Count];
        foreach (T node in nodeCollection)
        {
            foreach (int connectedID in node.connectedNodeIDs)
            {
                localAdjMatrix[node.id, connectedID] = 1;
            }
        }
        return localAdjMatrix;
    }

    /// <summary>
    /// Checks if the graph is valid, AKA it has N number of nodes where N = <param>nodeCollection</param>'s length
    /// and its Keys of _nodes are a sequence from 1 to N.
    /// </summary>
    /// <param name="nodeCollection">Collection of nodes (not necessarily sorted) passed in to the constructor.</param>
    /// <exception cref="IndexesNotASequenceException">Thrown if the graph is invalid.</exception>
    private void _CheckValidity(IReadOnlyCollection<T> nodeCollection)
    {
        bool missingIndex = this.nodes.Where((pair, i) => i != this.nodes.Keys[i]).Any();
        bool duplicateIndexes = this.nodes.Count != nodeCollection.Count;

        if (missingIndex || duplicateIndexes)
        {
            string exceptionMsg = "Graph constructed with problem(s): ";
            if (missingIndex) exceptionMsg += "\nAn index is missing from sorted nodelist.";
            if (duplicateIndexes) exceptionMsg += "\nDuplicate indexes present and one node overwrote another.";
            throw new IndexesNotASequenceException(exceptionMsg);
        }
    }
    
    [Serializable]
    private class IndexesNotASequenceException : Exception
    {
        /// <summary>
        /// An exception to be invoked if this class's constructor attempts to initialise
        /// a graph but its nodes are invalid, AKA IDs of its nodes do not form a sequence
        /// OR it has more than one node with the same ID (in which case one node will 'overwrite'
        /// another due to having the same Key in SortedList, and the # of nodes in this graph will
        /// be less than the number of nodes passed into its constructor)
        /// </summary>
        public IndexesNotASequenceException(string message) : base(message) {}

        public IndexesNotASequenceException()
        {
        }

        public IndexesNotASequenceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
}