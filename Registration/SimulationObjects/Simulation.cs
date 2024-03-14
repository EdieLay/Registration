using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Registration
{
    internal class Simulation
    {
        Dictionary<string, Flight> _flights;
        Rabbit _rabbit;
        Parser _parser;

        public Simulation() 
        {
            _flights = new Dictionary<string, Flight>();
            _rabbit = Rabbit.GetInstance();
            _parser = new Parser();
        }

        public void StartPolling()
        {
            IModel ticketsRQ = _rabbit.GetQueue(_rabbit.TicketsRQ);
            ticketsRQ.BasicQos(0, 1, false);
            var ticketsConsumer = new EventingBasicConsumer(ticketsRQ);
            ticketsConsumer.Received += (ch, ea) =>
            {
                var body = ea.Body.ToArray();
                string receivedMes = Encoding.UTF8.GetString(body);

                using (JsonDocument jsonDoc = _parser.ParseMessage(receivedMes))
                {
                    JsonElement data = jsonDoc.RootElement;
                    string passengerGuid = data.GetProperty(_parser.PassengerKey).ToString();
                    string flightGuid = data.GetProperty(_parser.FlightKey).ToString();
                    string baggageString = data.GetProperty(_parser.BaggageKey).ToString();
                    bool baggage = baggageString == "1" ? true : false;
                    _flights[flightGuid].AddPassenger(passengerGuid, baggage);
                }

                ticketsRQ.BasicAck(ea.DeliveryTag, false);
            };


            IModel passengersRQ = _rabbit.GetQueue(_rabbit.PassengersRQ);
            passengersRQ.BasicQos(0, 1, false);
            var passengersConsumer = new EventingBasicConsumer(passengersRQ);
            passengersConsumer.Received += (ch, ea) =>
            {
                var body = ea.Body.ToArray();
                string receivedMes = Encoding.UTF8.GetString(body);

                using (JsonDocument jsonDoc = _parser.ParseMessage(receivedMes))
                {
                    JsonElement data = jsonDoc.RootElement;
                    string passengerGuid = data.GetProperty(_parser.PassengerKey).ToString();
                    string flightGuid = data.GetProperty(_parser.FlightKey).ToString();
                    string curTimeString = data.GetProperty(_parser.TimeKey).ToString();
                    DateTime curTime = DateTime.Parse(curTimeString);

                    string registered = _flights[flightGuid].Register(passengerGuid, curTime);
                    Dictionary<string, string> sendData = new Dictionary<string, string>();
                    sendData[_parser.PassengerKey] = passengerGuid;
                    sendData[_parser.ResponseKey] = registered;

                    string json = _parser.ParseDict(sendData);
                    _rabbit.PutMessage(_rabbit.PassengersWQ, json);

                    if (registered == _parser.SuccessValue)
                    {
                        // отправить в багажи и в автобус
                    }
                }

                ticketsRQ.BasicAck(ea.DeliveryTag, false);
            };

        }
    }
}
