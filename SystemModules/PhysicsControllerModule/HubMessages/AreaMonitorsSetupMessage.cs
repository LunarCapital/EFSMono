using TinyMessenger;

namespace EFSMono.SystemModules.PhysicsControllerModule.HubMessages
{
    /// <summary>
    /// A message PhysicsController publishes to let any interested parties know that it has finished setting up Area2D monitors.
    /// </summary>
    class AreaMonitorsSetupMessage : ITinyMessage
    {
        public object Sender { get; }

        public AreaMonitorsSetupMessage(object sender)
        {
            this.Sender = sender;
        }
    }
}
