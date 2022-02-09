using System.IO;
using System.Net;
using System.Text.Json;
using UDP_Com;
using MessagePack;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("CoSim Middleware gestartet");

string configFileName = "config.json";
if (args.Length > 0)
{
    if (args[0] == "-h")
    {
        Console.WriteLine("Als Parameter bitte den Pfad zu Konfigurationsdatei angeben");
        Environment.Exit(0);
    }
    else
    {
        configFileName = args[0];
    }
}
if(!File.Exists(configFileName)) {
    Console.WriteLine($"Konfigurationsdatei {configFileName} nicht gefunden.\nBitte Dateipfad zur Konfigurationsdatei als Parameter angeben.");
    Environment.Exit(0);
}
//var ip = IPEndPoint.Parse("127.0.0.1:5557");
//Variable variable = new Variable();
//variable.SourceIPEndPoint = "127.0.0.1:5557";
string jsonString = File.ReadAllText(configFileName);
CoSimMiddlewareConfig config = JsonSerializer.Deserialize<CoSimMiddlewareConfig>(jsonString);
/*config.Variables = new List<List<Variable>>();
config.Variables.Add(new List<Variable>());
config.Variables[0].Add(new Variable());
config.Variables[0][0].SourceIPEndPoint_ = new IPEndPoint(IPAddress.Loopback, 5558);
string json = JsonSerializer.Serialize(config);*/
Dictionary<uint, Variable> topic_var_Mapping = new Dictionary<uint, Variable>();

CancellationTokenSource cts = new CancellationTokenSource();

UDP_Com.Transceiver transceiver = new Transceiver(cts.Token, (int)config.Port);

List<IModell> ModellInstanzen = new List<IModell>();

Console.WriteLine($"Modell {config.Modellname} wird geladen ...");
Console.WriteLine($"Middleware nutzt Port {transceiver.Port}");

for (int Instanz = 0; Instanz < config.Variables.Count; Instanz++)
{
    IModell modell;

    switch (config.Modellname)
    {
        case "A":
            modell = new ModellA();
            break;
        case "B":
            modell = new ModellB();
            break;
        default:
            throw new Exception("Modell unbekannt");
    }

    ModellInstanzen.Add(modell);
    for (int index = 0; index < config.Variables[Instanz].Count; index++)
    {
        transceiver.createTopic(config.Variables[Instanz][index].DestinationTopic);
        if (config.Variables[Instanz][index].SourceTopic != null)
        {
            topic_var_Mapping.Add((uint)config.Variables[Instanz][index].SourceTopic, config.Variables[Instanz][index]);

        }
        
        
    }
}

Console.WriteLine("Verbindung zu anderen Modellen aufbauen? Weiter mit [Enter]");
Console.ReadLine();

for (int Instanz = 0; Instanz < config.Variables.Count; Instanz++)
{
    for (int index = 0; index < config.Variables[Instanz].Count; index++)
    {
        if (config.Variables[Instanz][index].SourceTopic != null && config.Variables[Instanz][index].SourceIPEndPoint != null)
        {
            transceiver.subscribe((uint)config.Variables[Instanz][index].SourceTopic, config.Variables[Instanz][index].SourceIPEndPoint_);
            transceiver.subscribeToTopic((uint)config.Variables[Instanz][index].DestinationTopic, config.Variables[Instanz][index].SourceIPEndPoint_);
        }
        
    }
}

Console.WriteLine("Modell geladen");

double deltaT = 1;

ReceivedData? onDataReceived = OnVarUpdate;


Console.WriteLine("Simulation starten? Weiter mit [Enter]");
Console.ReadLine();

for (double t = 0; t < 100; t+= deltaT)
{
    List<Task> tasks = new List<Task>();
    for (int Instanz_ = 0; Instanz_ < config.Variables.Count; Instanz_++)
    {
        for (int index = 0; index < config.Variables[Instanz_].Count; index++)
        {
            if (config.Variables[Instanz_][index].Value != null)
            {
                ModellInstanzen[Instanz_].writeDouble(config.Variables[Instanz_][index].Name, (double)config.Variables[Instanz_][index].Value);
            }
        }
        Console.WriteLine(Instanz_);
        iterateAndPublish(Instanz_, deltaT);
        //tasks.Add(Task.Run(() => iterateAndPublish(ModellInstanzen[Instanz_], Instanz_, deltaT)));
    }
    Thread.Sleep(500);
    IPEndPoint remoteEP = null;
    byte[] data = transceiver.udpClient.Receive(ref remoteEP);
    var dt2 = data;
    //Task.WaitAll(tasks.ToArray());
}

Console.WriteLine("Simulation abgeschlossen");

void iterateAndPublish(int Instanz, double deltaT)
{
    IModell modell = ModellInstanzen[Instanz];
    modell.doStep(deltaT);

    for (int index = 0; index < config.Variables[Instanz].Count; index++)
    {
        if (config.Variables[Instanz][index].Value != null)
        {
            double value = ModellInstanzen[Instanz].readDouble(config.Variables[Instanz][index].Name);
            Variablenupdate variablenupdate = new Variablenupdate { Topic = config.Variables[Instanz][index].DestinationTopic, Value = value };
            byte[] data = MessagePackSerializer.Serialize(variablenupdate);
            transceiver.send(data, data.Length, variablenupdate.Topic, "V");

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"Variablenupdate senden:\tTopic: {variablenupdate.Topic}\tValue: {variablenupdate.Value}");
            Console.ResetColor();
        }
    }
}

void OnVarUpdate(IPEndPoint iPEndPoint, string? command, byte[] data)
{
    Console.WriteLine("Daten empfangen");
    switch (command)
    {
        case "V":
            Variablenupdate variablenupdate = MessagePackSerializer.Deserialize<Variablenupdate>(data);
            topic_var_Mapping[variablenupdate.Topic].Value = variablenupdate.Value;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Variablenupdate empfangen:\tTopic: {variablenupdate.Topic}\tValue: {variablenupdate.Value}");
            Console.ResetColor();
            break;
        case "T":
            for (int i = 0; i < ModellInstanzen.Count; i++)
            {
                iterateAndPublish(i, deltaT);
            }
            break;

        default:
            break;
    }

}

public class CoSimMiddlewareConfig
{
    public string Modellname { get; set; }
    public int? Port { get; set; }
    public List<List<Variable>> Variables { get; set; }
}

public class Variable
{
    public string Name { get; set; }
    public double? Value { get; set; }

    public string SourceIPEndPoint
    {
        set { SourceIPEndPoint_ = IPEndPoint.Parse(value); }
        get { return $"{SourceIPEndPoint_.Address}:{SourceIPEndPoint_.Port}"; }
    }
    public IPEndPoint? SourceIPEndPoint_ { get; set; }
    public uint? SourceTopic { get; set; }
    public uint DestinationTopic { get; set; }

}

[MessagePackObject]
public class Variablenupdate
{
    [Key(0)]
    public uint Topic;
    [Key(1)]
    public double Value;
}

abstract class IModell
{
    internal Dictionary<string, double> Variables = new Dictionary<string, double>();

    public double Time = 0.0;

    public IModell(params (string, double)[] VariablesList)
    {
        for (int i = 0; i < VariablesList.Length; i++)
        {
            Variables[VariablesList[i].Item1] = VariablesList[i].Item2;
        }
    }

    public double readDouble(string Varname)
    {
        return Variables[Varname];
    }

    public void writeDouble(string Varname, double value)
    {
        Variables[Varname] = value;
    }

    public abstract void doStep(double deltaT);

}
class ModellA : IModell
{
    public ModellA(double a = 1, double b = 2)
    {
        Variables = new Dictionary<string, double>();
        Variables["a"] = a;
        Variables["b"] = b;
    }
    public override void doStep(double deltaT)
    {
        Time += deltaT;
        Variables["a"] /= 2;
        Variables["b"] += Variables["a"]* deltaT;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Berechnet:\ta: {Variables["a"]}\tb: {Variables["b"]}");
        Console.ResetColor();
    }
}

class ModellB : IModell
{
    public ModellB(double a = 1, double b = 2)
    {
        Variables = new Dictionary<string, double>();
        Variables["a"] = a;
        Variables["b"] = b;
    }
    public override void doStep(double deltaT)
    {
        Time += deltaT;
        if (Variables["a"] % 2 == 0)
        {
            Variables["a"] /= 2;
        } else
        {
            Variables["a"] *= 3;
        }
        Variables["b"] += Variables["a"] * deltaT;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Berechnet:\ta: {Variables["a"]}\tb: {Variables["b"]}");
        Console.ResetColor();
    }
}