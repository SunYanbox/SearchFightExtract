using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SearchFightExtract
{
    /// <summary>
    /// 战区内地图的纯渲染层：只把区域连通图格式化成文本行，不做 I/O、不改游戏状态。
    ///
    /// 布局依据：主干是 <see cref="MapGenerator.Generate"/> 按索引顺序生成的链 i —— i+1，
    /// 所以把索引按蛇形铺进网格后，同一行内相邻的两格、以及行末的换行转折，都必然是
    /// 链上相邻节点——主干连线全部是确定的直角短线，不需要任何图布局算法。
    /// 随机的额外通道（非链边）画不进网格，单独列在图下方。
    /// </summary>
    static class MapView
    {
        const int Indent = 3;
        const int MarkerCols = 1;                                        // ▶ / ★ / 空格
        const int LabelCols = 4;                                         // "[NN]"
        const int NameCols = 8;                                          // 最长区域名 4 个汉字 = 8 列
        const int CellCols = MarkerCols + LabelCols + 1 + NameCols;      // 14
        const int SepCols = 4;                                           // " ── "
        const int Pitch = CellCols + SepCols;                            // 18
        const int ConnectorOffset = MarkerCols + 2;                      // │ 落在"[NN]"的数字那一列
        const int ChannelsPerLine = 3;

        public static List<string> Render(List<Zone> zones, int cur)
        {
            int n = zones.Count;
            int cols = n <= 15 ? 3 : 4;      // n 变大时自动加宽，控制在 4 行以内
            int rows = (n + cols - 1) / cols;
            int explored = zones.Count(z => z.Explored);

            var lines = new List<string>
            {
                $"已探索 {explored}/{n}（{explored * 100.0 / n:F1}%）   当前位置 {Style.Paint("▶", Style.BrightCyan)}[{cur,2}]",
                ""
            };

            for (int r = 0; r < rows; r++)
            {
                lines.Add(RowLine(zones, cur, r, cols));
                if (r < rows - 1) lines.Add(ConnectorLine(r, cols));
            }

            lines.Add("");
            lines.AddRange(ChannelLines(zones));
            lines.Add("   图例：▶ 你所在　★ 已知撤离点　* 未探索　（只连一条线即为死胡同）");
            lines.Add("   输入目标编号自动导航 / 回车返回");
            return lines;
        }

        /// <summary>蛇形摆放：偶数行左→右，奇数行右→左。</summary>
        static int NodeAt(int r, int c, int cols) => r * cols + (r % 2 == 0 ? c : cols - 1 - c);

        static string RowLine(List<Zone> zones, int cur, int r, int cols)
        {
            int n = zones.Count;
            var sb = new StringBuilder(new string(' ', Indent));
            bool prevReal = false;
            for (int c = 0; c < cols; c++)
            {
                int i = NodeAt(r, c, cols);
                bool real = i < n;
                // 空白格也要占满一格，否则后面所有格子的列号都会左移、和 │ 对不上
                if (c > 0) sb.Append(prevReal && real ? " ── " : new string(' ', SepCols));
                sb.Append(Cell(zones, cur, i, n));
                prevReal = real;
            }
            return sb.ToString();
        }

        static string ConnectorLine(int r, int cols)
        {
            // 换行转折只可能发生在行末：偶数行在最右一格，奇数行在最左一格。
            // 链按序铺排保证了这两处的邻居关系成立，末行不满时也一样。
            int c = r % 2 == 0 ? cols - 1 : 0;
            return new string(' ', Indent + c * Pitch + ConnectorOffset) + Style.Paint("│", Style.BrightBlack);
        }

        static string Cell(List<Zone> zones, int cur, int i, int n)
        {
            if (i >= n) return new string(' ', CellCols);
            var z = zones[i];
            // 名称只跟随 Explored：探明 ≥70% 后未探索的撤离点会渲染成 "★[ 9] *"
            string body = $"[{i,2}] " + PadCols(z.Explored ? z.Name : "*", NameCols);
            if (!z.Explored) body = Style.Paint(body, Style.BrightBlack);
            return MarkerFor(zones, cur, i) + body;
        }

        static string MarkerFor(List<Zone> zones, int cur, int i)
        {
            if (i == cur) return Style.Paint("▶", Style.BrightCyan);
            if (MapGenerator.IsExtractKnown(zones, i)) return Style.Paint("★", Style.BrightYellow);
            return " ";
        }

        static List<string> ChannelLines(List<Zone> zones)
        {
            // 链边恰好是 i —— i+1，且生成期已排除重复边，所以 (j > i && j != i+1) 正好是全部额外通道
            var pairs = new List<(int a, int b)>();
            for (int i = 0; i < zones.Count; i++)
                foreach (var j in zones[i].Neighbors)
                    if (j > i && j != i + 1) pairs.Add((i, j));
            pairs.Sort((p, q) => p.a != q.a ? p.a.CompareTo(q.a) : p.b.CompareTo(q.b));

            var lines = new List<string>();
            if (pairs.Count == 0) { lines.Add("   额外通道：无"); return lines; }

            for (int k = 0; k < pairs.Count; k += ChannelsPerLine)
            {
                string part = string.Join("   ", pairs.Skip(k).Take(ChannelsPerLine).Select(p => $"[{p.a,2}]—[{p.b,2}]"));
                lines.Add((k == 0 ? "   额外通道：" : "             ") + part);
            }
            return lines;
        }

        // ===== 显示宽度 =====
        // string.PadRight 数的是 UTF-16 字符：区域名有 2/3/4 字三种长度，
        // "码头".PadRight(8) 是 10 显示列而"废弃仓库".PadRight(8) 是 8 列，对不齐。

        static string PadCols(string s, int cols)
        {
            int w = DisplayWidth(s);
            return w >= cols ? s : s + new string(' ', cols - w);
        }

        static int DisplayWidth(string s)
        {
            int w = 0;
            foreach (char ch in s) w += IsWide(ch) ? 2 : 1;
            return w;
        }

        // 只把 CJK 全角区段算作 2 列。─ ★ ▶ ═ │ 属 East Asian Ambiguous，一律按 1 列处理，
        // 与「标记格固定 1 列」的假设自洽。
        static bool IsWide(char c) =>
            (c >= 0x1100 && c <= 0x115F) ||
            (c >= 0x2E80 && c <= 0xA4CF) ||
            (c >= 0xAC00 && c <= 0xD7A3) ||
            (c >= 0xF900 && c <= 0xFAFF) ||
            (c >= 0xFE30 && c <= 0xFE6F) ||
            (c >= 0xFF00 && c <= 0xFF60) ||
            (c >= 0xFFE0 && c <= 0xFFE6);
    }
}
