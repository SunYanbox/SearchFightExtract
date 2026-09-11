using System;

namespace SearchFightExtract
{
    static class ConsoleHelper
    {
        public static void Clear() { try { Console.Clear(); } catch { } }
        public static string ReadLine() => (Console.ReadLine() ?? "").Trim();
        public static void Pause() { Console.WriteLine("\n按任意键继续..."); try { Console.ReadKey(true); } catch { } }
    }
}
