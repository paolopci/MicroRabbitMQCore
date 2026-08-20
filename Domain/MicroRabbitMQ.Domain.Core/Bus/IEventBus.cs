using MicroRabbitMQ.Domain.Core.Commands;
using MicroRabbitMQ.Domain.Core.Events;

namespace MicroRabbitMQ.Domain.Core.Bus
{
    public interface IEventBus
    {
        Task SenderCommand<T>(T command) where T : Command;

        Task Publish<T>(T @event) where T : Event;

        Task Subscribe<T, TH>()
            where T : Event
            where TH : IEventHandler<T>;
    }
}
