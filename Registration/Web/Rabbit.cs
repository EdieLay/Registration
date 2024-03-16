using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Registration
{
    internal class Rabbit
    {
        private static Rabbit? _instance = null;

        private ConnectionFactory _factory;
        private IConnection _connection;
        private string _exchangeName = "PassengersExchange";
        private Parser _parser;

        // WQ - Write Queue - очередь для записи
        // RQ - Read Queue - очередь для считывания
        public string TicketsWQ { get => "TicketsRequest"; }
        public string TicketsRQ { get => "TicketsResponse"; }
        public string PassengersWQ { get => "RegistrationToPassengers"; }
        public string PassengersRQ { get => "PassengersToRegistration"; }
        public string BaggageWQ { get => "RegistrationToBaggage"; }
        public string BusWQ { get => "RegistrationToBus"; }
        public string FlightsRQ { get => "FlightsToRegistration"; }

        private Rabbit()
        {
            _factory = new ConnectionFactory
            {
                VirtualHost = "itojxdln",
                HostName = "hawk-01.rmq.cloudamqp.com",
                Password = "DEL8js4Cg76jY_2lAt19CjfY2saZT0yW",
                UserName = "itojxdln",
                ClientProvidedName = "Registration"
            };
            while (true)
            {
                try
                {
                    _connection = _factory.CreateConnection();
                    Log.Information("Connected to RabbitMQ host.");
                    break;
                }
                catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException)
                {
                    int retrySeconds = 3;
                    Log.Error("Can't connect to RabbitMQ host. Trying again in {RetrySeconds} sec.", retrySeconds);
                    Thread.Sleep(TimeSpan.FromSeconds(retrySeconds));
                }
            }
            _parser = new Parser();
        }

        public static Rabbit GetInstance()
        {
            if (_instance == null)
                _instance = new Rabbit();
            return _instance;
        }

        public IModel GetQueue(string queueName)
        {
            IModel channel = _connection.CreateModel();
            string routingKey = queueName + "Key";

            channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct);
            channel.QueueDeclare(queueName, false, false, false, null);
            channel.QueueBind(queueName, _exchangeName, routingKey, null);

            return channel;
        }

        public void PutMessage(string queueName, string message)
        {
            string routingKey = queueName + "Key";
            IModel channel = GetQueue(queueName);

            byte[] messageBodyBytes = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(_exchangeName, routingKey, null, messageBodyBytes);

            Log.Information("Sent to queue {Queue} message {Message}.", queueName, message);

            channel.Close();
        }

        public string GetMessage(string queueName)
        {
            string routingKey = queueName + "Key";
            IModel channel = GetQueue(queueName);
            channel.BasicQos(0, 1, false);

            string receivedMes = String.Empty;

            BasicGetResult result = channel.BasicGet(queueName, true);

            if (result == null)
            {
                Log.Information("Queue {Queue} is empty", queueName);
            }
            else
            {
                byte[] body = result.Body.ToArray();
                receivedMes = Encoding.UTF8.GetString(body);
                Log.Information("Got from queue {Queue} message {Message}.", queueName, receivedMes);
            }

            channel.Close();

            return receivedMes;
        }

        public void CloseConnection()
        {
            _connection.Close();
        }
    }
}
