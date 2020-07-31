using System;
using System.Collections.Generic;
using EFSMono.Scripts.Autoload;
using Godot;

namespace EFSMono.Scripts.DataStructures.Graphs.PolygonSplittingGraphObjects
{
    public class PolygonSplittingGraphNode : GenericGraphNode, IComparable
    {
        public float x { get; }
        public float y { get; }
        
        public PolygonSplittingGraphNode(int id, List<int> connectedNodeIDs, float x, float y) : base(id, connectedNodeIDs)
        {
            this.x = x;
            this.y = y;
        }

        public int CompareTo(object obj)
        {
            return this._CompareTo((PolygonSplittingGraphNode) obj);
        }

        private int _CompareTo(PolygonSplittingGraphNode other)
        {
            return SortFuncs.SortByXThenYAscending(new Vector2(this.x, this.y), new Vector2(other.x, other.y));
        }
    }
}