using System;
using System.Runtime.InteropServices;

namespace SearchFightExtract
{
    static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        const int STD_OUTPUT_HANDLE = -11;
        const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        // 启用 Windows 控制台的 ANSI 转义处理，否则颜色码会显示为乱码
        static void EnableAnsi()
        {
            try
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
                IntPtr handle = GetStdHandle(STD_OUTPUT_HANDLE);
                if (handle == IntPtr.Zero || handle == new IntPtr(-1)) return;
                if (!GetConsoleMode(handle, out uint mode)) return;
                SetConsoleMode(handle, mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
            }
            catch { }
        }

        static void Main()
        {
            try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }
            EnableAnsi();
            new Game().Run();
        }
    }
}
