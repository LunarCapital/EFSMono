using TinyMessenger;

namespace EFSMono.SystemModules.EntityTrackerModule.HubMessages
{
    public class DebugFallMessage : ITinyMessage
    {
        public object Sender { get; }
    }
}