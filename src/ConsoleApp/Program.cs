using Microsoft.Extensions.Configuration;

namespace MagicPacket.ConsoleApp;

public class Program
{
    private const string ConfigurationFileName = "configuration.json";

    public static async Task<int> Main(string[] args)
    {
        try
        {
            using MagicPacketClient client = new();

            // Get the values from the configuration file.
            Configuration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(ConfigurationFileName, optional: true)
#if DEBUG
                .AddJsonFile($"debug.{ConfigurationFileName}", optional: true)
#endif
                .Build()
                .Get<Configuration>();

            // Apply the values from the configuration file.
            string target = config.Target ?? string.Empty;
            string broadcast = config.Broadcast ?? string.Empty;
            string password = config.Password ?? string.Empty;
            int timeout = config.Timeout ?? 0;

            // Override the values with the arguments injected in (if any).
            target = ArgumentsParser.GetValue<string?>(args, nameof(target)) ?? target;
            broadcast = ArgumentsParser.GetValue<string?>(args, nameof(broadcast)) ?? broadcast;
            password = ArgumentsParser.GetValue<string?>(args, nameof(password)) ?? password;
            timeout = ArgumentsParser.GetValue<ushort?>(args, nameof(timeout)) ?? timeout;

            CancellationToken cancellationToken = timeout == 0
                ? default
                : new CancellationTokenSource(timeout).Token;

            if (string.IsNullOrWhiteSpace(broadcast))
            {
                await client.BroadcastOnAllInterfacesAsync(target, password, cancellationToken).ConfigureAwait(true);
            }
            else
            {
                await client.BroadcastOnSingleInterfaceAsync(target, broadcast, password, cancellationToken).ConfigureAwait(true);
            }
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Invalid configuration.{Environment.NewLine}" +
                $"{ex.Message}{Environment.NewLine}");
            return (int)ExitCode.InvalidConfiguration;
        }
        catch (InvalidOperationException ex) when (ex.InnerException is ArgumentException)
        {
            Console.WriteLine($"Invalid configuration.{Environment.NewLine}" +
                $"{ex.Message}{Environment.NewLine}" +
                $"{ex.InnerException.Message} ");
            return (int)ExitCode.InvalidConfiguration;
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine($"Timeout occured.");
            return (int)ExitCode.Timeout;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled exception.{Environment.NewLine}" +
                $"{ex.Message}");
            return (int)ExitCode.UnhandledException;
        }

        return (int)ExitCode.Success;
    }

    private class Configuration
    {
        public string? Target { get; set; }
        public string? Broadcast { get; set; }
        public string? Password { get; set; }
        public ushort? Timeout { get; set; }
    }

    private enum ExitCode
    {
        Success = 0,
        UnhandledException = 1,
        InvalidConfiguration = 2,
        Timeout = 3
    }
}