using EFSMono.GameObjects.Common.Components.Action;
using Godot;
using ActionID = EFSMono.Common.Enums.Actions.ActionID;

namespace EFSMono.Entities.Common.Components.Controller
{
    /// <summary>
    /// Controller component interface for all entities.  Designates how an entity is controller, for example, by the player's input, or via AI.
    /// </summary>
    public interface IController
    {
        Vector2 motion { get; }
        Vector2 target { get; }

        void ControlEntity(float delta);
        ActionComponent GetAction(ActionID actionID);
    }
}
