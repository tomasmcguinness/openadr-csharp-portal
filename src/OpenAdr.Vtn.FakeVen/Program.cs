using OpenAdr.Vtn.FakeVen;

// Usage: OpenAdr.Vtn.FakeVen <vtnBaseUrl> <mqttHost> [--fast] [--mqtt-port N]
//                            [--ven-name NAME] [--client-id ID] [--client-secret SECRET]
var positional = new List<string>();
var fast = false;
var mqttPort = 1883;
var venName = "kitchen-hems";
var clientId = "hems-demo-1";
var clientSecret = "demo-secret";

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--fast":
            fast = true;
            break;
        case "--mqtt-port":
            mqttPort = int.Parse(args[++i]);
            break;
        case "--ven-name":
            venName = args[++i];
            break;
        case "--client-id":
            clientId = args[++i];
            break;
        case "--client-secret":
            clientSecret = args[++i];
            break;
        default:
            positional.Add(args[i]);
            break;
    }
}

if (positional.Count < 2)
{
    Console.Error.WriteLine("Usage: OpenAdr.Vtn.FakeVen <vtnBaseUrl> <mqttHost> [--fast] [--mqtt-port N] [--ven-name NAME] [--client-id ID] [--client-secret SECRET]");
    return 1;
}

var vtnBaseUrl = positional[0];
var mqttHost = positional[1];

Logger.Info($"Starting fake VEN '{venName}' against {vtnBaseUrl}, MQTT at {mqttHost}:{mqttPort} (fast={fast}).");

using var http = new HttpClient { BaseAddress = new Uri(vtnBaseUrl) };
var api = new VtnClient(http, clientId, clientSecret);
var mqtt = new MqttClientWrapper(mqttHost, mqttPort, venName);
var runner = new FakeVenRunner(api, mqtt, venName, fast);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await runner.RunAsync(cts.Token);
return 0;
