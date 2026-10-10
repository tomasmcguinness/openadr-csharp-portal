using System.Net;

namespace OpenAdr.Vtn.FakeVen;

/// <summary>Timestamped console logging, with request/response/MQTT bodies printed in full
/// and indented — this is deliberately verbose: the whole point of this console app is to be
/// a readable reference for porting the same sequence to ESP-IDF C.</summary>
public static class Logger
{
    public static void Info(string message) => Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] {message}");

    public static void Request(string method, string path, string? body)
    {
        Info($"--> {method} {path}");
        if (!string.IsNullOrWhiteSpace(body))
        {
            Console.WriteLine(Indent(body));
        }
    }

    public static void Response(HttpStatusCode status, string? body)
    {
        Info($"<-- {(int)status} {status}");
        if (!string.IsNullOrWhiteSpace(body))
        {
            Console.WriteLine(Indent(body));
        }
    }

    public static void Mqtt(string topic, string payload)
    {
        Info($"MQTT <-- {topic}");
        Console.WriteLine(Indent(payload));
    }

    private static string Indent(string text) =>
        string.Join('\n', text.TrimEnd().Split('\n').Select(line => "    " + line));
}
