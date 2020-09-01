using System;
using Godot;

namespace EFSMono.Common.DataStructures.Geometry
{
    /// <summary>
    /// Class that describes a concave vertex. Requires three vertices: the vertex itself, and the two adjacent to it.
    /// </summary>
    public class ConcaveVertex
    {
        public Vector2 vertex { get; }
        public Vector2 prevVertex { get; }
        public Vector2 nextVertex { get; }

        public ConcaveVertex(Vector2 vertex, Vector2 prevVertex, Vector2 nextVertex)
        {
            this.vertex = vertex;
            this.prevVertex = prevVertex;
            this.nextVertex = nextVertex;
        }

        /// <summary>
        /// Gets a unit vector describing the horizontal direction that you could draw a line towards without hitting
        /// either of its adjacent vertices.
        /// </summary>
        /// <returns>Unit vector pointing left or right.</returns>
        public Vector2 GetHorizontalFreeDirection()
        {
            Vector2 adjacentHorizontalVertex;
            if (this.prevVertex.y == this.vertex.y)
                adjacentHorizontalVertex = this.prevVertex;
            else
            {
                adjacentHorizontalVertex = this.nextVertex;
            }

            Vector2 freeDirection = (adjacentHorizontalVertex - this.vertex).Normalized();
            freeDirection.x *= -1;
            return freeDirection;
        }

        /// <summary>
        /// Gets a unit vector describing the horizontal direction that you could draw a line towards without hitting
        /// either of its adjacent vertices.
        /// </summary>
        /// <returns>Unit vector pointing left or right.</returns>
        public Vector2 GetVerticalFreeDirection()
        {
            Vector2 adjacentVerticalVertex;
            if (this.prevVertex.x == this.vertex.x)
                adjacentVerticalVertex = this.prevVertex;
            else
            {
                adjacentVerticalVertex = this.nextVertex;
            }

            Vector2 freeDirection = (adjacentVerticalVertex - this.vertex).Normalized();
            freeDirection.y *= -1;
            return freeDirection;
        }

        public override int GetHashCode()
        {
            return this.vertex.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return this._Equals((ConcaveVertex)obj);
        }

        private bool _Equals(ConcaveVertex other)
        {
            return this.vertex == other.vertex;
        }
    }
}