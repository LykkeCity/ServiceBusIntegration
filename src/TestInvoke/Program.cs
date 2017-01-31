using System;
using Lykke.ServiceBusIntegration;
using TestInvoke.Publisher;
using TestInvoke.Subscriber;

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
