namespace SearchFightExtract
{
    /// <summary>
    /// 通用 ANSI 转义样式库。
    /// 只提供颜色常量与通用包装方法，不含任何业务语义（如 Damage / Heal 之类），
    /// 由调用方自行决定为哪段文本、哪种语义选用哪个颜色。
    /// </summary>
    static class Style
    {
        public const string Reset = "\u001b[0m";

        // ===== 标准前景色 =====
        public const string Black = "\u001b[30m";
        public const string Red = "\u001b[31m";
        public const string Green = "\u001b[32m";
        public const string Yellow = "\u001b[33m";
        public const string Blue = "\u001b[34m";
        public const string Magenta = "\u001b[35m";   // 紫
        public const string Cyan = "\u001b[36m";
        public const string White = "\u001b[37m";

        // ===== 亮色前景 =====
        public const string BrightBlack = "\u001b[90m"; // 灰
        public const string BrightRed = "\u001b[91m";
        public const string BrightGreen = "\u001b[92m";
        public const string BrightYellow = "\u001b[93m";
        public const string BrightBlue = "\u001b[94m";
        public const string BrightMagenta = "\u001b[95m";
        public const string BrightCyan = "\u001b[96m";
        public const string BrightWhite = "\u001b[97m";

        // ===== 样式修饰 =====
        public const string Bold = "\u001b[1m";
        public const string Dim = "\u001b[2m";
        public const string Underline = "\u001b[4m";

        /// <summary>用指定转义码包装文本；code 为空则原样返回。</summary>
        public static string Paint(string text, string code)
            => string.IsNullOrEmpty(code) ? text : code + text + Reset;

        /// <summary>用指定转义码包装任意值（自动 ToString）。</summary>
        public static string Paint(object value, string code)
            => Paint(value?.ToString() ?? "", code);

        /// <summary>拼接多个转义码（如 Bold + Red）。</summary>
        public static string Combine(params string[] codes)
            => string.Concat(codes);
    }
}
