Console.WriteLine("Hello, dotnet-archive!");
Console.WriteLine($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"OS:      {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");

if (args.Length > 0)
{
    Console.WriteLine("Arguments:");
    for (int i = 0; i < args.Length; i++)
    {
        Console.WriteLine($"  [{i}] {args[i]}");
    }
}
await Task.Delay(2000);
