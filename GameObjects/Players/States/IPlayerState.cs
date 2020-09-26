using Godot;

namespace EFSMono.Entities.Players.States
{
    public interface IPlayerState
    {
        IPlayerState ParseInput(float delta, Vector2 motion, Vector2 target);
    }
}
