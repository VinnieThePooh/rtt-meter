using System.Net.Sockets;
using DataStreaming.Constants.RTT;
using DataStreaming.Exceptions;
using DataStreaming.Protocols.Factories;
using DataStreaming.Services.Interfaces;
using DataStreaming.Services.RTT;
using DataStreaming.Settings;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var meteringSettings = configuration.GetSection(RttMeteringSettings.SectionName).Get<RttMeteringSettings>();
var protoFactory = (IRttMeteringProtocolFactory)RttMeteringProtocolFactory.Create();

IRttMeteringService meteringService = new RttMeteringService(meteringSettings, protoFactory);
meteringService.RttReceived += (o, eventArgs) =>
{
    if (eventArgs.MeteringType == RttMeteringType.SinglePacket)
    {
        var stats = eventArgs.RttStats.Value;
        Console.WriteLine(
            $"[{nameof(RttMeteringType.SinglePacket)}]: {stats.SequenceNumber}. {Math.Ceiling(stats.RttValue.TotalMicroseconds)} mks");
        return Task.CompletedTask;
    }

    var agStats = eventArgs.AggregatedRttStats.Value;
    Console.WriteLine($"[{nameof(RttMeteringType.AggregationInterval)}]: {agStats.SequenceNumber}.\n" +
                      $"Avg Rtt: {(agStats.AvgRtt.TotalMicroseconds > 1000 ? Math.Round(agStats.AvgRtt.TotalMilliseconds, 2) + "ms" : Math.Round(agStats.AvgRtt.TotalMicroseconds, 2) + "mks")} ({agStats.PacketsCount} packets for {agStats.AggregationInterval} ms)\n" +
                      $"Max Rtt: {(agStats.MaxRtt.TotalMicroseconds > 1000 ? Math.Round(agStats.MaxRtt.TotalMilliseconds, 2) + "ms" : Math.Round(agStats.MaxRtt.TotalMicroseconds, 2) + "mks")}\n" +
                      $"Min Rtt: {(agStats.MinRtt.TotalMicroseconds > 1000 ? Math.Round(agStats.MinRtt.TotalMilliseconds, 2) + "ms" : Math.Round(agStats.AvgRtt.TotalMicroseconds, 2) + "mks")}\n");
    return Task.CompletedTask;
};
try
{
    _ = await meteringService.Start();
}
catch (SocketException e)
{
    //todo: log here
    Console.WriteLine(e);
    Environment.Exit(e.ErrorCode);
}
catch (DisconnectedPrematurelyException e)
{
    //todo: log here
    Console.WriteLine($"RTT echo server disconnected prematurely:\n{e}");
    Environment.Exit(e.HResult);
}
catch (Exception e)
{
    //todo: log here
    Console.WriteLine($"Unexpected exception:\n{e}");
    Environment.Exit(e.HResult);
}
finally
{
    await meteringService.Stop();
}