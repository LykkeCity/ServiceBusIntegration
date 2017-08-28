using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amqp;
using Autofac;
using Common;
using Common.Log;

namespace Lykke.ServiceBusIntegration.Subscriber
{

    public interface IServiceBusMessageDeserializer<out TModel>
    {
        TModel Deserialize(byte[] data);
    }

    public interface IServiceBusReadStrategy
    {
        void Configure(ServiceBusSettings settings, Session session, ReceiverLink reciever);
    }

    public class ServiceBarSubscriber<TModel> : IStartable, IMessageConsumer<TModel>
    {
        private readonly string _applicationName;
        private readonly ServiceBusSettings _settings;
        private readonly int _reconnectTimeOut;
        private IServiceBusMessageDeserializer<TModel> _deserializer;
        private IServiceBusReadStrategy _serviceBusReadStrategy;

        private ILog _log;

        public ServiceBarSubscriber(string applicationName, ServiceBusSettings settings, int reconnectTimeOut = 3000)
        {
            _applicationName = applicationName;
            _settings = settings;
            _reconnectTimeOut = reconnectTimeOut;
        }


        #region Configure

        public ServiceBarSubscriber<TModel> SetDeserializer(IServiceBusMessageDeserializer<TModel> deserializer)
        {
            _deserializer = deserializer;
            return this;
        }

        public ServiceBarSubscriber<TModel> SetLogger(ILog log)
        {
            _log = log;
            return this;
        }

        public ServiceBarSubscriber<TModel> SetReadStrategy(IServiceBusReadStrategy strategy)
        {
            _serviceBusReadStrategy = strategy;
            return this;
        }

        #endregion


        private Task _task;

        private async Task TheTask()
        {

            var policyName = WebUtility.UrlEncode(_settings.PolicyName);
            var key = WebUtility.UrlEncode(_settings.Key);
            var connectionString = $"amqps://{policyName}:{key}@{_settings.NamespaceUrl}/";


            while (_task != null)
            try
            {
                await ConnectAndReadAsync(connectionString);
            }
            catch (Exception e)
            {
                if (_log != null)
                    await _log.WriteErrorAsync(_applicationName, "TheTask", "", e);
            }
            finally
            {
                await Task.Delay(_reconnectTimeOut);
            }

        }

        private async Task ConnectAndReadAsync(string connectionString)
        {

            var connection = await Connection.Factory.CreateAsync(new Address(connectionString));
            var session = new Session(connection);
            var receiver = new ReceiverLink(session, "receiver-link", _settings.QueueName);

            _serviceBusReadStrategy?.Configure(_settings, session, receiver);

            while (_task != null)
            {
                var message = await receiver.ReceiveAsync(5000);

                if (message == null)
                    continue;

                var body = message.GetBody<byte[]>();
                var data = _deserializer.Deserialize(body);

                foreach (var subscriber in _subscribers)
                    await subscriber(data);

                receiver.Accept(message);
            }

        }

        void IStartable.Start()
        {
            Start();

        }

        public ServiceBarSubscriber<TModel> Start()
        {
            if (_task != null)
                return this;

            if (_log == null)
                throw new Exception("Please specify ILog for: "+_applicationName);

            if (_deserializer == null)
                throw new Exception("Please specify deserializer: " + _applicationName);

            _task = TheTask();

            return this;
        }

        public void Stop()
        {
            if (_task == null)
                return;

            var task = _task;
            _task = null;
            task.Wait();
        }

        private readonly List<Func<TModel, Task>> _subscribers = new List<Func<TModel, Task>>();

        public ServiceBarSubscriber<TModel> Subscribe(Func<TModel, Task> callback)
        {
            _subscribers.Add(callback);
            return this;
        }

        void IMessageConsumer<TModel>.Subscribe(Func<TModel, Task> callback)
        {
            Subscribe(callback);
        }
    }
}
