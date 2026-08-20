# Migrazione a RabbitMQ .NET Client 7.x

Questo progetto usa `RabbitMQ.Client` 7.x. Questa nota riassume le principali
modifiche introdotte dalla versione 7 della libreria.

Fonte ufficiale: <https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/main/v7-MIGRATION.md>

## API asincrona

L'API pubblica del client adotta il modello asincrono basato su `Task` (TAP).
I metodi asincroni terminano con il suffisso `Async` e devono essere invocati
con `await`.

Esempi:

```csharp
await using var connection = await factory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();
```

## Connessioni e canali

- `IModel` è stato rinominato in `IChannel`.
- `DispatchConsumersAsync` non è più necessario: nella versione 7 i consumer
  sono già asincroni.
- Per controllare il parallelismo della consegna, se necessario, configurare
  `ConnectionFactory.ConsumerDispatchConcurrency`. Il valore predefinito è
  adatto quando si desidera elaborare un messaggio alla volta.

```csharp
var factory = new ConnectionFactory
{
    HostName = "localhost",
    ConsumerDispatchConcurrency = 1
};
```

## Pubblicazione dei messaggi

- Non usare più `IModel.CreateBasicProperties()`.
- Creare direttamente le proprietà del messaggio con `new BasicProperties()`.
- Usare i metodi di pubblicazione asincroni, ad esempio `BasicPublishAsync`.

```csharp
var properties = new BasicProperties
{
    Persistent = true
};
```

## Consumer e corpo del messaggio

- Usare `AsyncEventingBasicConsumer` oppure derivare da
  `AsyncDefaultBasicConsumer`.
- Gestire gli eventi asincroni, ad esempio `ReceivedAsync`.
- Il `ReadOnlyMemory<byte>` in `eventArgs.Body` è di proprietà della libreria
  ed è valido solo durante l'esecuzione dell'handler. Se serve oltre l'handler,
  copiarlo prima.

```csharp
consumer.ReceivedAsync += async (_, eventArgs) =>
{
    byte[] body = eventArgs.Body.ToArray();
    // Elaborare o conservare body in modo sicuro.
    await Task.CompletedTask;
};
```

## Checklist per questo progetto

1. Sostituire i riferimenti a `IModel` con `IChannel`.
2. Rendere asincroni i metodi che aprono connessioni, canali, pubblicano o
   registrano consumer.
3. Rimuovere `DispatchConsumersAsync` dalla configurazione.
4. Passare a `AsyncEventingBasicConsumer` e agli eventi con suffisso `Async`.
5. Copiare il body del messaggio con `ToArray()` quando viene usato fuori
   dall'handler.
