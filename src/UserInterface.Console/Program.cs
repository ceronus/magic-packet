namespace MagicPacket.UserInterface;

public class Program
{
    public static async Task Main()
    {
        string address = "88-88-88-88-87-88";
        await WakeOnLan.SendWakeOnLanAsync(address).ConfigureAwait(true);
    }
}