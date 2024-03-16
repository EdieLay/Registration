using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
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
            IModel ticketsRQ = _rabbit.GetQueue(_rabbit.TicketsRQ); // обработка сообщений с покупкой билетов
            var ticketsConsumer = new EventingBasicConsumer(ticketsRQ);
            ticketsConsumer.Received += (ch, ea) =>
            {
                var body = ea.Body.ToArray();
                string receivedMes = Encoding.UTF8.GetString(body);
                Log.Information("Got from queue {Queue} message {Message}.", _rabbit.TicketsRQ, receivedMes);

                try
                {
                    using (JsonDocument jsonDoc = _parser.ParseMessage(receivedMes))
                    {
                        JsonElement data = jsonDoc.RootElement;
                        string passengerGuid = data.GetProperty(_parser.PassengerKey).ToString(); // айди пассажира
                        string flightGuid = data.GetProperty(_parser.FlightKey).ToString(); // айди полета
                        string baggageString = data.GetProperty(_parser.BaggageKey).ToString(); // флаг наличия багажа
                        bool baggage = baggageString == "1" ? true : false;
                        _flights[flightGuid].AddPassenger(passengerGuid, baggage); // добавляем пассажира
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Couldn't parse JSON from {QueueName}", _rabbit.TicketsRQ);
                }

                ticketsRQ.BasicAck(ea.DeliveryTag, false);
            };
            string ticketsConsumerTag = ticketsRQ.BasicConsume(_rabbit.TicketsRQ, false, ticketsConsumer);
            //ticketsRQ.BasicCancel(ticketsConsumerTag);


            IModel passengersRQ = _rabbit.GetQueue(_rabbit.PassengersRQ); // обработка сообщений от пассажиров на регистрацию
            var passengersConsumer = new EventingBasicConsumer(passengersRQ);
            passengersConsumer.Received += (ch, ea) =>
            {
                var body = ea.Body.ToArray();
                string receivedMes = Encoding.UTF8.GetString(body);
                Log.Information("Got from queue {Queue} message {Message}.", _rabbit.PassengersRQ, receivedMes);

                try
                {
                    using (JsonDocument jsonDoc = _parser.ParseMessage(receivedMes))
                    {
                        JsonElement data = jsonDoc.RootElement;
                        string passengerGuid = data.GetProperty(_parser.PassengerKey).ToString(); // айди пассажира
                        string flightGuid = data.GetProperty(_parser.FlightKey).ToString(); // айди рейса

                        bool baggage;

                        if (_flights.ContainsKey(flightGuid)) // если рейс существует
                        {
                            string registered = _flights[flightGuid].Register(passengerGuid, out baggage); // регистрация пассажиров
                            // вернётся одно из 3 значений: 0 - провал, 1 - успех, 2 - регистрация не началась
                            Dictionary<string, string> sendData = new Dictionary<string, string>();
                            sendData[_parser.PassengerKey] = passengerGuid;
                            sendData[_parser.ResponseKey] = registered;

                            string json = _parser.ParseDict(sendData);
                            _rabbit.PutMessage(_rabbit.PassengersWQ, json);

                            if (registered == _parser.SuccessValue) // если пасссажир зарегистрировался, то отправить его автобусу и его багаж
                            {
                                // отправить в багажи и в автобус
                                sendData = new Dictionary<string, string>();
                                sendData[_parser.PassengerKey] = passengerGuid;
                                sendData[_parser.RetardedFlightKey] = flightGuid;
                                json = _parser.ParseDict(sendData);
                                _rabbit.PutMessage(_rabbit.BusWQ, json);

                                if (baggage) // если есть багаж
                                {
                                    _rabbit.PutMessage(_rabbit.BaggageWQ, json);
                                }
                            }
                        }
                        else // если рейс не существует
                        {
                            Dictionary<string, string> sendData = new Dictionary<string, string>();
                            sendData[_parser.PassengerKey] = passengerGuid;
                            sendData[_parser.ResponseKey] = _parser.FailValue;

                            string json = _parser.ParseDict(sendData);
                            _rabbit.PutMessage(_rabbit.PassengersWQ, json); // отправляем провал
                        }
                    }

                    passengersRQ.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex) 
                {
                    Log.Error(ex, "Couldn't parse JSON from {QueueName}", _rabbit.PassengersRQ);
                }
            };
            string passengerConsumerTag = passengersRQ.BasicConsume(_rabbit.PassengersRQ, false, passengersConsumer);
            //passengersRQ.BasicCancel(passengerConsumerTag);

            IModel flightsRQ = _rabbit.GetQueue(_rabbit.FlightsRQ); // обработка сообщений по рейсам
            var flightsConsumer = new EventingBasicConsumer(flightsRQ);
            flightsConsumer.Received += (ch, ea) =>
            {
                var body = ea.Body.ToArray();
                string receivedMes = Encoding.UTF8.GetString(body);
                Log.Information("Got from queue {Queue} message {Message}.", _rabbit.FlightsRQ, receivedMes);

                try
                {
                    using (JsonDocument jsonDoc = _parser.ParseMessage(receivedMes))
                    {
                        JsonElement data = jsonDoc.RootElement;
                        string flightGuid = data.GetProperty(_parser.FlightKey).ToString();
                        if (!_flights.ContainsKey(flightGuid)) // если в словаре нет этого рейса
                        {
                            _flights[flightGuid] = new Flight(flightGuid); // то инициализируем его, регистрация ещё не началась
                            string registrationFlag = data.GetProperty(_parser.RegistrationKey).ToString();
                            bool registrationStarted = registrationFlag == "1"; // если ключ == 1, то регистрация началась, в остальных случая закончилась
                            if (registrationStarted)
                                _flights[flightGuid].ChangeRegstrationState(registrationStarted);
                        }
                        else // если он есть в словаре, то проверяем ключ
                        {
                            string registrationFlag = data.GetProperty(_parser.RegistrationKey).ToString();
                            bool registrationStarted = registrationFlag == "1"; // если ключ == 1, то регистрация началась, в остальных случая закончилась
                            _flights[flightGuid].ChangeRegstrationState(registrationStarted);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Couldn't parse JSON from {QueueName}", _rabbit.FlightsRQ);
                }


                flightsRQ.BasicAck(ea.DeliveryTag, false);
            };
            string flightsConsumerTag = flightsRQ.BasicConsume(_rabbit.FlightsRQ, false, flightsConsumer);
            //flightsRQ.BasicCancel(flightsConsumerTag);

            Console.ReadLine();
        }
    }
}
