using System;
using System.Net;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Autofac;
using Common;
using Common.Log;

namespace Lykke.ServiceBusIntegration.Publisher
{


    public interface IServiceBusSerializer<in TModel>
    {
        byte[] Serialize(TModel item);
    }

    public class ServiceBusPublisher<TModel> : IStartable, IMessageProducer<TModel>
    {
        private readonly string _applicationName;
        private readonly ServiceBusSettings _settings;
        private readonly int _reconnectTimeOut;

        private IServiceBusSerializer<TModel> _serializer;

        private ILog _log;

        private readonly QueueWithConfirmation<TModel> _messages = new QueueWithConfirmation<TModel>();


        public ServiceBusPublisher(string applicationName, ServiceBusSettings settings, int reconnectTimeOut = 3000)
        {
            _applicationName = applicationName;
            _settings = settings;
            _reconnectTimeOut = reconnectTimeOut;
        }


        #region Configuration

        public ServiceBusPublisher<TModel> SetSerializer(IServiceBusSerializer<TModel> serializer)
        {
            _serializer = serializer;
            return this;
        }

        public ServiceBusPublisher<TModel> SetLog(ILog log)
        {
            _log = log;
            return this;
        }
        #endregion



        private Task _workingTask;
        private async Task TheTask()
        {

            try
            {
                await ConnectAndWork();
            }
            catch (Exception e)
            {
                if (_log != null)
                await _log.WriteErrorAsync(_applicationName, "TheThread", "", e);
            }
            finally
            {
                await Task.Delay(_reconnectTimeOut);
            }

        }



        private async Task ConnectAndWork()
        {
            var policyName = WebUtility.UrlEncode(_settings.PolicyName);
            var key = WebUtility.UrlEncode(_settings.Key);
            var connectionString = $"amqps://{policyName}:{key}@{_settings.NamespaceUrl}/";

            var connection = await Connection.Factory.CreateAsync(new Address(connectionString));
            var amqpSession = new Session(connection);
            var senderLink = new SenderLink(amqpSession, "sender-link", _settings.QueueName);

            using (var message = _messages.Dequeue())
            {
                var dataToPost = _serializer.Serialize(message.Item);
                await senderLink.SendAsync(new Message {BodySection = new Data {Binary = dataToPost} });
                message.Compliete();
            }

        }


        public Task ProduceAsync(TModel message)
        {
            _messages.Enqueue(message);
            return Task.FromResult(0);
        }


        public ServiceBusPublisher<TModel> Start()
        {
            if (_workingTask != null)
                return this;

            if (_serializer == null)
                throw new Exception("Please specify serializer for: " + _applicationName);

            if (_log == null)
                throw new Exception("Please specify ILog for: " + _applicationName);

            _workingTask = TheTask(); 

            return this;
        }

        void IStartable.Start()
        {
            Start();
        }

        public void Stop()
        {
            if (_workingTask == null)
                return;


            var workingTask = _workingTask;
            _workingTask = null;
            workingTask.Wait();
        }

    }
}
