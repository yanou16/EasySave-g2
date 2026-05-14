using System.Threading;

namespace CryptoSoft;

public static class Program
{
    private static Mutex? _mutex;

    public static void Main(string[] args)
    {
        const string mutexName = "CryptoSoft_Unique_Instance";

        _mutex = new Mutex(true, mutexName, out bool createdNew);

        if (!createdNew)
        {
            Console.WriteLine("CryptoSoft is already running.");
            Environment.Exit(-1);
            return;
        }

        try
        {
            foreach (var arg in args)
            {
                Console.WriteLine(arg);
            }

            var fileManager = new FileManager(args[0], args[1]);
            int elapsedTime = fileManager.TransformFile();
            Environment.Exit(elapsedTime);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Environment.Exit(-99);
        }
        finally
        {
            _mutex.ReleaseMutex();
        }
    }
}