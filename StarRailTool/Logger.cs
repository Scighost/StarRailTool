namespace StarRailTool;

internal abstract class Logger
{

    public static void Trace(string message, bool noTime = false)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        if (noTime)
        {
            Console.WriteLine(message);
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }
        Console.ForegroundColor = ConsoleColor.White;
    }



    public static void Info(string message, bool noTime = false)
    {
        Console.ForegroundColor = ConsoleColor.White;
        if (noTime)
        {
            Console.WriteLine(message);
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }
        Console.ForegroundColor = ConsoleColor.White;
    }



    public static void Debug(string message, bool noTime = false)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        if (noTime)
        {
            Console.WriteLine(message);
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }
        Console.ForegroundColor = ConsoleColor.White;
    }

    public static void Success(string message, bool noTime = false)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        if (noTime)
        {
            Console.WriteLine(message);
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }
        Console.ForegroundColor = ConsoleColor.White;
    }



    public static void Warn(string message, bool noTime = false)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        if (noTime)
        {
            Console.WriteLine(message);
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }
        Console.ForegroundColor = ConsoleColor.White;
    }


    public static void Error(string message, bool noTime = false)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        if (noTime)
        {
            Console.WriteLine(message);
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }
        Console.ForegroundColor = ConsoleColor.White;
    }

}
