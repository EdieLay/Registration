using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Registration
{
    internal class Flight
    {
        readonly string _guid;
        readonly DateTime _date;
        readonly DateTime _registrationEnd;
        readonly DateTime _registrationStart;

        private Dictionary<string, bool> _passengers; // <guid, baggage>
        private int _boughtTickets;
        private int _registered;
        private Parser _parser;


        public string GUID { get => _guid; }
        public DateTime Date { get => _date; }
        public int PassengersBoughtTickets { get => _boughtTickets; }
        public int PassengersRegistered { get => _registered; }

        public Flight(string guid, DateTime date)
        {
            _passengers = new Dictionary<string, bool>();
            _guid = guid;
            _date = date;
            _registrationEnd = _date.AddMinutes(-25);
            _registrationStart = _date.AddMinutes(-120);
            _boughtTickets = 0;
            _registered = 0;
            _parser = new Parser();
        }

        public string Register(string guid, DateTime curTime, out bool baggage)
        {
            baggage = false;
            if (curTime > _registrationEnd)
                return _parser.FailValue;
            if (curTime < _registrationStart)
                return _parser.RegistrationNotStartedValue;
            if (_passengers.ContainsKey(guid))
            {
                _registered++;
                baggage = _passengers[guid];
                _passengers.Remove(guid);
                return _parser.SuccessValue;
            }
            return _parser.FailValue;
        }

        public void AddPassenger(string guid, bool baggage)
        {
            _passengers[guid] = baggage;
            _boughtTickets++;
        }

        public bool GetPassengerBaggage(string guid)
        {
            return _passengers[]
        }
    }
}
