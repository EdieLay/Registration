using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Registration
{
    internal class Parser
    {
        public string PassengerKey { get => "Passenger"; }
        public string FlightKey { get => "Flight"; }
        public string RetardedFlightKey { get => "Voyage"; }
        public string BaggageKey { get => "Baggage"; }
        public string FoodKey { get => "Food"; }
        public string RegistrationKey { get => "Registration"; }
        public string ResponseKey { get => "Response"; }
        public string TimeKey { get => "Time"; }
        public string FailValue { get => "0"; }
        public string SuccessValue { get => "1"; }
        public string RegistrationNotStartedValue { get => "2"; }

        public JsonDocument ParseMessage(string json)
        {
            return JsonDocument.Parse(json);
        }

        public string ParseDict(Dictionary<string, string> dict)
        {
            string json = JsonSerializer.Serialize(dict);
            return json;
        }
    }
}
