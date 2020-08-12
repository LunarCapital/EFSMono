using Godot;
using TinyMessenger;

namespace EFSMono.Scripts.SystemModules.EntityTrackerModule.HubMessages
{
public class EntityChangedZIndexMessage : ITinyMessage
{
    public object Sender { get; }
    private readonly RID _entityRID;
    private readonly int _zIndex;

    public EntityChangedZIndexMessage(object sender, RID entityRID, int zIndex)
    {
        this.Sender = sender;
        this._entityRID = entityRID;
        this._zIndex = zIndex;
    }

    public RID GetEntityRID()
    {
        return this._entityRID;
    }

    public int GetZIndex()
    {
        return this._zIndex;
    }
}
}