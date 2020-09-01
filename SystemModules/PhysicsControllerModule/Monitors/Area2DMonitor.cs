using System.Collections.Generic;
using EFSMono.SystemModules.PhysicsControllerModule.HubMessages;
using Godot;
using TinyMessenger;

namespace EFSMono.SystemModules.PhysicsControllerModule.Monitors
{
    /// <summary>
    /// A class that monitors the callback for a specific area2D.
    /// </summary>
    class Area2DMonitor : Object
    {
        private readonly RID _areaRID;
        private readonly int _zIndex;
        private readonly TinyMessengerHub _toEntityHub;
        private readonly List<ConvexPolygonShape2D> _areaShapes;

        public Area2DMonitor(RID areaRID, int zIndex, List<ConvexPolygonShape2D> areaShapes, TinyMessengerHub toEntityHub)
        {
            this._areaRID = areaRID;
            this._zIndex = zIndex;
            this._areaShapes = areaShapes;
            this._toEntityHub = toEntityHub;
        }

        /// <summary>
        /// Fires when an Area2D reports that a PhysicsBody has entered or exited one of its shapes.
        /// </summary>
        /// <param name="bodyStatus">Whether body entered or exited area.</param>
        /// <param name="bodyRID">RID of body that interacted with area.</param>
        /// <param name="bodyInstanceID">Instance ID of the object that entered/exited the area.</param>
        /// <param name="bodyShapeIndex">Index of the body's shape.</param>
        /// <param name="areaShapeIndex">Index of the poly within the area2D.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required by Physics2DServer")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "Required by Physics2DServer")]
        public void OnAreaCallback(Physics2DServer.AreaBodyStatus bodyStatus, RID bodyRID, int bodyInstanceID, int bodyShapeIndex, int areaShapeIndex)
        {
            this._toEntityHub.Publish(new Area2DCallbackMessage(this, bodyStatus, bodyRID, this._areaRID.GetId(), this._areaShapes[areaShapeIndex].GetRid().GetId(), this._zIndex));
        }
    }
}
