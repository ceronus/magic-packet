using Microsoft.Extensions.Configuration;

namespace MagicPacket.ConsoleApp;

public class Program
{
    private const string ConfigurationFileName = "configuration.json";

    public static async Task Main(string[] args)
    {
        try
        {
            using MagicPacketClient client = new();

            Configuration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(ConfigurationFileName, optional: true)
#if DEBUG
                .AddJsonFile($"debug.{ConfigurationFileName}", optional: true)
#endif
                .Build()
                .Get<Configuration>();

            string target = config.Target ?? string.Empty;
            string broadcast = config.Broadcast ?? string.Empty;
            string password = config.Password ?? string.Empty;

            target = ArgumentsParser.GetValue<string>(args, nameof(target)) ?? target;
            broadcast = ArgumentsParser.GetValue<string>(args, nameof(broadcast)) ?? broadcast;
            password = ArgumentsParser.GetValue<string>(args, nameof(password)) ?? password;

            Console.WriteLine($"Target: {target}");

            int timeout = ArgumentsParser.GetValue<ushort>(args, nameof(timeout));
            CancellationToken cancellationToken = timeout == 0
                ? default
                : new CancellationTokenSource(timeout).Token;

            if (string.IsNullOrWhiteSpace(broadcast))
                await client.BroadcastOnAllInterfacesAsync(target, password, cancellationToken).ConfigureAwait(true);
            else
            {
                await client.BroadcastOnSingleInterfaceAsync(target, broadcast, password, cancellationToken).ConfigureAwait(true);
            }
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"{ex.Message}");
        }
    }

    private class Configuration
    {
        public string? Target { get; set; }
        public string? Broadcast { get; set; }
        public string? Password { get; set; }
    }
}