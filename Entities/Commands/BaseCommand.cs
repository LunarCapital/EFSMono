namespace EFSMono.Entities.Commands
{
    /// <summary>
    /// Command base class.
    /// </summary>
    public abstract class BaseCommand
    {
        public abstract void Execute();
    }
}