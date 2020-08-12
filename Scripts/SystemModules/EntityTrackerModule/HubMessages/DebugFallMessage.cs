using TinyMessenger;

namespace EFSMono.Scripts.SystemModules.EntityTrackerModule.HubMessages
{
    public class DebugFallMessage : ITinyMessage
    {
        public object Sender { get; }
    }
}