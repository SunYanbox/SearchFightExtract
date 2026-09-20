using System;
using System.Collections.Generic;

namespace SearchFightExtract
{
    static class ConsoleHelper
    {
        public static void Clear() { try { Console.Clear(); } catch { } }
        public static string ReadLine() => (Console.ReadLine() ?? "").Trim();
        public static void Pause() { Console.WriteLine("\n按任意键继续..."); try { Console.ReadKey(true); } catch { } }

        /// <summary>逐行打印结算层返回的文本行。</summary>
        public static void WriteLines(List<string> lines)
        {
            foreach (var line in lines) Console.WriteLine(line);
        }
    }
}
