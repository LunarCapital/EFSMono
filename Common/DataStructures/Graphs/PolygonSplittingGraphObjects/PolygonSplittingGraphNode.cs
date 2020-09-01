using System;
using System.Collections.Generic;
using EFSMono.Common.Autoload;
using Godot;

namespace EFSMono.Common.DataStructures.Graphs.PolygonSplittingGraphObjects
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

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return this._Equals((PolygonSplittingGraphNode)obj);
        }

        private bool _Equals(PolygonSplittingGraphNode other)
        {
            return this.x == other.x && this.y == other.y;
        }

        public override int GetHashCode()
        {
            return this.x.GetHashCode() ^ this.y.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            if (obj is null) return -1;
            return this._CompareTo((PolygonSplittingGraphNode)obj);
        }

        private int _CompareTo(PolygonSplittingGraphNode other)
        {
            return SortFuncs.SortByXThenYAscending(new Vector2(this.x, this.y), new Vector2(other.x, other.y));
        }

        public static bool operator ==(PolygonSplittingGraphNode left, PolygonSplittingGraphNode right)
        {
            if (left is null || right is null) return false;
            return left.x == right.x && left.y == right.y;
        }

        public static bool operator !=(PolygonSplittingGraphNode left, PolygonSplittingGraphNode right)
        {
            return !(left == right);
        }

        public static bool operator <(PolygonSplittingGraphNode left, PolygonSplittingGraphNode right)
        {
            if (left is null || right is null) return false;
            if (left.x < right.x) return true;
            else return (left.y < right.y);
        }

        public static bool operator <=(PolygonSplittingGraphNode left, PolygonSplittingGraphNode right)
        {
            if (left is null || right is null) return false;
            if (left.x <= right.x) return true;
            else return (left.y <= right.y);
        }

        public static bool operator >(PolygonSplittingGraphNode left, PolygonSplittingGraphNode right)
        {
            if (left is null || right is null) return false;
            if (left.x > right.x) return true;
            else return (left.y > right.y);
        }

        public static bool operator >=(PolygonSplittingGraphNode left, PolygonSplittingGraphNode right)
        {
            if (left is null || right is null) return false;
            if (left.x >= right.x) return true;
            else return (left.y >= right.y);
        }
        //i implemented these to shut up the code analyzer and somewhat regret it
    }
}