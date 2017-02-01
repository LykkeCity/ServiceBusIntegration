using System.Text;
using Common.Log;
using Lykke.ServiceBusIntegration;
using Lykke.ServiceBusIntegration.Publisher;

namespace TestInvoke.Publisher
{
    public static class HowToPublish
    {

        private static ServiceBusPublisher<string> _connection;

        public static void Example(ServiceBusSettings settings)
        {
            _connection 
                = new ServiceBusPublisher<string>("HowToPublish example", settings)
                .SetLog(new LogToConsole())
                .SetSerializer(new TestServiceBusSerializer())
                .Start();
        }

        public static void Stop()
        {
            _connection.Stop();
        }
    }


    public class TestServiceBusSerializer : IServiceBusSerializer<string>
    {
        public byte[] Serialize(string item)
        {
            return Encoding.UTF8.GetBytes(item);
        }
    }
}
