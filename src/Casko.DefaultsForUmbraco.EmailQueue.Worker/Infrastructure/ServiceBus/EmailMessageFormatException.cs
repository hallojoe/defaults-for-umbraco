namespace Casko.DefaultsForUmbraco.EmailQueue.Worker.Infrastructure.ServiceBus;

public sealed class EmailMessageFormatException(string message) : Exception(message);
