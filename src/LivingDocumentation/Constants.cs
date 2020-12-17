using System;

namespace Pitstop.LivingDocumentation
{
    internal class Constants
    {
        public const string Command = "Pitstop.Infrastructure.Messaging.Command";
        public const string Event = "Pitstop.Infrastructure.Messaging.Event";
        
        public const string MessageHandlerCallback = "Pitstop.Infrastructure.Messaging.IMessageHandlerCallback";
        public const string MessagePublisher = "Pitstop.Infrastructure.Messaging.IMessagePublisher";
        public const string PublishMessage = "PublishMessageAsync";

        public const string AggregateRoot = "Pitstop.WorkshopManagementAPI.Domain.Core.AggregateRoot<";
        public const string Entity = "Pitstop.WorkshopManagementAPI.Domain.Core.Entity<";
    }
}
