using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.ServiceBusIntegration;
using Lykke.ServiceBusIntegration.Subscriber;

namespace TestInvoke.Subscriber
{
    public static class HowToSubscribe
    {

        private static ServiceBarSubscriber<string> _connection;

        public static void Example(ServiceBusSettings settings)
        {

            _connection 
                = new ServiceBarSubscriber<string>("HowToSubscribe example", settings)
                .SetDeserializer(new TestDeseializer())
                .Subscribe(MessageHandler)
                .Start();
        }

        private static Task MessageHandler(string message)
        {
            Console.WriteLine(message);
            return Task.FromResult(0);
        }


        public static void Stop()
        {
            _connection.Stop();
        }
    }

    public class TestDeseializer : IServiceBusMessageDeserializer<string>
    {
        public string Deserialize(string data)
        {
            return data;
        }
    }

}

