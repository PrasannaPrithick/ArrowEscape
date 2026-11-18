using System;
using System.IO;
using HueGates.Tests;

internal static class Program
{
    static int Main(string[] args)
    {
        string path = args.Length > 0 ? args[0] : Path.Combine("data", "levels_1_25.json");
        HueCases.RunAll(File.ReadAllText(path));
        Console.WriteLine("All Hue Gates logic tests passed.");
        return 0;
    }
}
