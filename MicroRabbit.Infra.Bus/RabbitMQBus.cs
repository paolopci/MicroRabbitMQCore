using MediatR;
using MicroRabbitMQ.Domain.Core.Bus;
using MicroRabbitMQ.Domain.Core.Commands;
using MicroRabbitMQ.Domain.Core.Events;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace MicroRabbit.Infra.Bus
{
    public sealed class RabbitMQBus : IEventBus
    {
        private readonly IMediator _mediator;
        private readonly Dictionary<string, List<Type>> _handlers;
        private readonly List<Type> _eventTypes;

        public RabbitMQBus(IMediator mediator)
        {
            _mediator = mediator;
            // inizializzo la lista degli handler e degli eventi
            _handlers = new Dictionary<string, List<Type>>();
            _eventTypes = new List<Type>();
        }

        public Task SenderCommand<T>(T command) where T : Command
        {
            // invio il comando al mediatore
            return _mediator.Send(command);
        }

        public async Task Publish<T>(T @event) where T : Event
        {
            // creo una connessione al server RabbitMQ
            var factory = new ConnectionFactory() { HostName = "localhost" };
            // creo una connessione per inviare il messaggio
            await using var connection = await factory.CreateConnectionAsync();
            // creo un canale per inviare il messaggio
            await using var channel = await connection.CreateChannelAsync();
            {
                // ottengo il nome dell'evento
                var eventName = @event.GetType().Name;
                // dichiaro la coda per l'evento
                await channel.QueueDeclareAsync(eventName, false, false, false, null);
                // serializzo l'evento in formato JSON
                var message = JsonConvert.SerializeObject(@event);
                // converto il messaggio in un array di byte
                var body = Encoding.UTF8.GetBytes(message);

                // invio il messaggio alla coda
                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: eventName,
                    body: body);
            }
        }

        public async Task Subscribe<T, TH>() where T : Event where TH : IEventHandler<T>
        {
            // ottengo il nome dell'evento e il tipo dell'handler
            var eventName = typeof(T).Name;
            var handlerType = typeof(TH);

            if (!_eventTypes.Contains(typeof(T)))
            {
                // se non esiste questo tipo di evento lo aggiungo alla lista degli eventi
                _eventTypes.Add(typeof(T));
            }
            // se non esiste questo tipo di handler lo aggiungo alla lista degli handler
            if (!_handlers.ContainsKey(eventName))
            {
                _handlers.Add(eventName, new List<Type>());
            }
            // se esiste già questo tipo di handler per questo evento lancio un'eccezione
            if (_handlers[eventName].Any(s => s.GetType() == handlerType))
            {
                throw new ArgumentException(
                         $"Handler Type {handlerType.Name} already is registered for {eventName}",
                         nameof(handlerType));
            }
            // aggiungo il tipo di handler alla lista degli handler per questo evento
            _handlers[eventName].Add(handlerType);

            await StartBasicConsume<T>();
        }

        private async Task StartBasicConsume<T>() where T : Event
        {
            // creo una connessione al server RabbitMQ
            var factory = new ConnectionFactory()
            {
                HostName = "localhost"
            };
            // creo una connessione e un canale per ricevere i messaggi
            var connection = await factory.CreateConnectionAsync();
            // creo un canale per ricevere i messaggi
            var channel = await connection.CreateChannelAsync();

            // ottengo il nome dell'evento
            var eventName = typeof(T).Name;

            // dichiaro la coda per l'evento
            await channel.QueueDeclareAsync(eventName, false, false, false, null);

            // creo un consumer asincrono per ricevere i messaggi
            var consumer = new AsyncEventingBasicConsumer(channel);

            // creo un delegate
            consumer.ReceivedAsync += Consumer_Received;
            // avvio il consumer per ricevere i messaggi dalla coda
            await channel.BasicConsumeAsync(eventName, true, consumer);
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            var eventName = @event.RoutingKey;
            // var message=Encoding.UTF8.GetString(@event.Body.Span);
            var messageBytes = @event.Body.ToArray();
            var message = Encoding.UTF8.GetString(messageBytes);

            try
            {
                await ProcessEvent(eventName, message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        // metodo per processare l'evento ricevuto
        private async Task ProcessEvent(string eventName, string message)
        {
            // se esiste un handler per questo evento lo eseguo
            if (_handlers.ContainsKey(eventName))
            {
                // ottengo la lista degli handler per questo evento
                var subscriptions = _handlers[eventName];
                foreach (var subscription in subscriptions)
                {
                    // creo un'istanza dell'handler
                    var handle = Activator.CreateInstance(subscription);
                    if (handle == null)
                    {
                        // se non riesco a creare l'istanza dell'handler continuo con il prossimo
                        continue;
                    }
                    // ottengo il tipo dell'evento
                    var eventType = _eventTypes.SingleOrDefault(t => t.Name == eventName);
                    // deserializzo il messaggio in un oggetto dell'evento
                    var @event = JsonConvert.DeserializeObject(message, eventType);
                    // creo un tipo concreto dell'handler per l'evento
                    var conreteType = typeof(IEventHandler<>).MakeGenericType(eventType);


                    // invoco il metodo Handle dell'handler con l'evento deserializzato
                    await (Task)conreteType.GetMethod("Handle").Invoke(handle, new object[] { @event });
                }
            }
        }
    }
}
