namespace OpenAdr.Vtn.Server;

public class VtnOptions
{
    public const string SectionName = "Vtn";

    public string DatabasePath { get; set; } = "/var/lib/openadr-vtn/vtn.db";
    public int MqttPort { get; set; } = 1883;
    public bool MqttEnabled { get; set; } = true;
}
