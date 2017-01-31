using System;
using Lykke.ServiceBusIntegration;
using TestInvoke.Publish;
using TestInvoke.Subscribe;

namespace TestInvoke
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var settings = new ServiceBusSettings
            {
                Key = "",
                NamespaceUrl = "",
                PolicyName = "",
                QueueName = ""
            };


            HowToPublish.Example(settings);
            HowToSubscribe.Example(settings);

            Console.WriteLine("This is just and example how to use");

            Console.ReadLine();
        }
    }
}
