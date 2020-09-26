using EFSMono.Entities.Players.States;
using Godot;

namespace EFSMono.GameObjects.Players.HourglassPlayerStates
{
    public class IdleBacklineState : IPlayerState
    {
        private readonly Entity _parent;

        public IdleBacklineState(Entity parent)
        {
            this._parent = parent;
        }

        public IPlayerState ParseInput(float delta, Vector2 motion, Vector2 target)
        {
            
        }
    }
}
