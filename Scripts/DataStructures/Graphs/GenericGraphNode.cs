using System.Collections.Generic;

namespace EFSMono.Scripts.DataStructures.Graphs
{
/// <summary>
/// Generic Node Abstract for Generic Graphs.
/// Needs only an ID and connected node IDs.
/// </summary>
public abstract class GenericGraphNode
{
    public int id { get; }
    public List<int> connectedNodeIDs { get; }

    protected GenericGraphNode(int id, List<int> connectedNodeIDs)
    {
        this.id = id;
        this.connectedNodeIDs = connectedNodeIDs;
    }

    public override int GetHashCode()
    {
        return this.id.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return this.Equals(obj as GenericGraphNode);
    }

    private bool Equals(GenericGraphNode obj)
    {
        return this.id == obj.id;
    }
}
}