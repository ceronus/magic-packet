using System.Diagnostics;
using System.Globalization;

namespace MagicPacket.ConsoleApp;

public static class ArgumentsParser
{
    public const char ArgumentPrefix = '-';

    public static T? GetValue<T>(string[] args, string argumentName)
    {
        Argument? argument = ParseArgument(args, argumentName);
        if (argument is null) return default;
        Debug.Assert(argument.Name == argumentName);
        if (string.IsNullOrWhiteSpace(argument.Value)) return default;

        try
        {
            Type type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(argument.Value, type, CultureInfo.InvariantCulture);
        }
        catch
        {
            return default;
        }
    }

    public record Argument(string Name, string Value);

    public static Argument? ParseArgument(string[] args, string argumentName)
    {
        if (args.Length < 2) return null;
        Debug.Assert(!string.IsNullOrWhiteSpace(argumentName));

        Argument? argument = null;

        // Get the last valid argument pair
        for (int i = 0; i < args.Length - 1; i++)
        {
            _ = TryParseArgument(args[i], args[i + 1], argumentName, out argument);
        }

        return argument;
    }

    private static bool TryParseArgument(string arg, string nextArg, string argumentName, out Argument? argument)
    {
        argument = null;

        if (!arg.StartsWith($"{ArgumentPrefix}", StringComparison.OrdinalIgnoreCase)) return false;
        if (nextArg.StartsWith($"{ArgumentPrefix}", StringComparison.OrdinalIgnoreCase)) return false;

        arg = RemoveArgumentPrefix(arg); ;
        if (arg.Equals(argumentName, StringComparison.OrdinalIgnoreCase))
        {
            argument = new(argumentName, nextArg.Replace("\"", string.Empty));
            return true;
        }

        return false;
    }

    private static string RemoveArgumentPrefix(ReadOnlySpan<char> value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] != ArgumentPrefix) return value[i..].ToString();
        }

        throw new InvalidOperationException("Impossible scenario.");
    }
}