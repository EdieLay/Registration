using Registration;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt",
                outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day)
    .CreateLogger();


Simulation simulation = new Simulation();
simulation.StartPolling();

//Rabbit rabbit = Rabbit.GetInstance();
//Parser parser = new Parser();
//Dictionary<string, string> data = new Dictionary<string, string>();
//data[parser.PassengerKey] = "123";
//data[parser.RetardedFlightKey] = "321";
//string json = parser.ParseDict(data);
//rabbit.PutMessage(rabbit.BaggageWQ, json);