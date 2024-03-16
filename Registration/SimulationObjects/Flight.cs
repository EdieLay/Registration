using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Registration
{
    enum RegistrationState
    {
        RegistrationNotStarted,
        RegistrationStarted,
        RegistrationStopped,
    }
    internal class Flight
    {
        readonly string _guid;

        private Dictionary<string, bool> _passengers; // <guid, baggage>
        private int _boughtTickets;
        private int _registered;
        private Parser _parser;


        public string GUID { get => _guid; }
        public int PassengersBoughtTickets { get => _boughtTickets; }
        public int PassengersRegistered { get => _registered; }
        public RegistrationState State { get; set; }

        public Flight(string guid)
        {
            _passengers = new Dictionary<string, bool>();
            _guid = guid;
            _boughtTickets = 0;
            _registered = 0;
            _parser = new Parser();
            State = RegistrationState.RegistrationNotStarted;
        }

        public string Register(string guid, out bool baggage)
        {
            baggage = false;
            if (State == RegistrationState.RegistrationStopped) // если регистрация закончилась
                return _parser.FailValue; // провал
            if (State == RegistrationState.RegistrationNotStarted) // если регитсрация не началась
                return _parser.RegistrationNotStartedValue; // флаг о том, что не началась
            if (_passengers.ContainsKey(guid)) // если есть пассажир в списках на регистрацию
            {
                _registered++; // кол-во зарегистрированных
                baggage = _passengers[guid]; // флаг багажа
                _passengers.Remove(guid); // удаляем пассажира, т.к. мы больше его не ждём на регистрацию
                return _parser.SuccessValue; // успех, пассажир зарегистрировался
            }
            return _parser.FailValue; // если пассажира в списках нет, то провал
        }

        public void ChangeRegstrationState(bool start) // изменение состояния регистрации
        {
            if (start)
                State = RegistrationState.RegistrationStarted;
            else
                State = RegistrationState.RegistrationStopped;
        }

        public void AddPassenger(string guid, bool baggage) // добавление пассажира
        {
            _passengers[guid] = baggage;
            _boughtTickets++;
        }
    }
}
