using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;

namespace EFSMono.Common.DataStructures.Graphs
{
    /// <summary>
    /// Generic Graph abstract.
    /// Represents a graph via a sorted list of Generic Graph Nodes and an adjacency matrix.
    /// </summary>
    public abstract partial class GenericGraph<T> where T : GenericGraphNode
    {
        public SortedList<int, T> nodes { get; }
        private readonly int[,] _adjMatrix;
        public ImmutableArray<ImmutableArray<int>> adjMatrix
        {
            get
            {
                ImmutableArray<ImmutableArray<int>>.Builder adjMatrixBuilder = ImmutableArray.CreateBuilder<ImmutableArray<int>>();
                for (int i = 0; i < this._adjMatrix.GetLength(0); i++)
                {
                    ImmutableArray<int>.Builder adjRowBuilder = ImmutableArray.CreateBuilder<int>();
                    for (int j = 0; j < this._adjMatrix.GetLength(1); j++)
                    {
                        adjRowBuilder.Add(this._adjMatrix[i, j]);
                    }
                    adjMatrixBuilder.Add(adjRowBuilder.ToImmutable());
                }
                return adjMatrixBuilder.ToImmutable();
            }
        }
        protected GenericGraph(IReadOnlyCollection<T> nodeCollection)
        {
            if (nodeCollection is null) throw new ArgumentNullException(nameof(nodeCollection));

            this.nodes = new SortedList<int, T>();
            this._adjMatrix = this.ConstructAdjMatrix(nodeCollection);
            foreach (T node in nodeCollection)
            {
                this.nodes[node.id] = node;
            }
            this._CheckValidity(nodeCollection);
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
            if (startNodes is null) throw new ArgumentNullException(nameof(startNodes));
            if (excludedEdges == null) excludedEdges = new Dictionary<int, HashSet<int>>();

            var visited = startNodes.ToHashSet();
            var stack = new Stack<T>();
            startNodes.ForEach(x => stack.Push(x));

            while (stack.Count > 0)
            {
                T node = stack.Pop();

                for (int neighbourID = 0; neighbourID < this._adjMatrix.GetLength(1); neighbourID++)
                {
                    if (excludedEdges.ContainsKey(node.id))
                        if (excludedEdges[node.id].Contains(neighbourID)) continue;

                    if (!visited.Contains(this.nodes[neighbourID]) && this._adjMatrix[node.id, neighbourID] > 0)
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
        protected int[,] ConstructAdjMatrix(IReadOnlyCollection<T> nodeCollection)
        {
            if (nodeCollection is null) throw new ArgumentNullException(nameof(nodeCollection));

            int[,] localAdjMatrix = new int[nodeCollection.Count, nodeCollection.Count];
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
    }
}