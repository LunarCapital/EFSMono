using Godot;
using TinyMessenger;

namespace EFSMono.SystemModules.PhysicsControllerModule.HubMessages
{
    /// <summary>
    /// A message sent when PhysicsController detects that one of its floor area2Ds has been entered or exited by some entity.
    /// </summary>
    public class Area2DCallbackMessage : ITinyMessage
    {
        public object Sender { get; private set; }
        public Physics2DServer.AreaBodyStatus areaBodyStatus { get; }
        public RID entityRID { get; }
        public int areaID { get; }
        public int areaShapeIndex { get; }
        public int areaZIndex { get; }

        public Area2DCallbackMessage(object sender, Physics2DServer.AreaBodyStatus areaBodyStatus, RID entityRID, int areaID, int areaShapeIndex, int areaZIndex)
        {
            this.Sender = sender;
            this.areaBodyStatus = areaBodyStatus;
            this.entityRID = entityRID;
            this.areaID = areaID;
            this.areaShapeIndex = areaShapeIndex;
            this.areaZIndex = areaZIndex;
        }
    }
}
