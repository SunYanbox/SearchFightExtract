using System;
using System.Collections.Generic;
using System.Linq;

namespace SearchFightExtract
{
    /// <summary>
    /// 地图生成：随机连通区域图 + 撤离点放置 + 守军/事件填充，另含撤离点寻路。
    /// </summary>
    static class MapGenerator
    {
        static readonly string[] ZonePool =
        {
            "废弃仓库", "宿舍楼", "油罐区", "医疗站", "办公楼", "地下车库", "雷达站", "军械库", "码头",
            "水处理厂", "化工厂", "监狱", "机场", "火车站", "购物中心"
        };

        static readonly string[] Events =
        {
            "发现一个陷阱，小心！", "遇到流浪商人，可以交易。", "捡到一张地图，显示附近物资。",
            "空投箱！里面有高级物资。", "辐射区，持续掉血。", "神秘信号，吸引敌人。"
        };

        public static List<Zone> Generate(Random rng, Difficulty d)
        {
            var shuffled = ZonePool.OrderBy(x => rng.Next()).ToList();
            int n = rng.Next(10, 15);
            var zones = new List<Zone>();
            for (int i = 0; i < n; i++) zones.Add(new Zone { Name = shuffled[i] });

            for (int i = 0; i < n - 1; i++)
            {
                zones[i].Neighbors.Add(i + 1);
                zones[i + 1].Neighbors.Add(i);
            }
            int extra = rng.Next(4, 7);
            for (int k = 0; k < extra; k++)
            {
                int a = rng.Next(n), b = rng.Next(n);
                if (a == b || zones[a].Neighbors.Contains(b)) continue;
                zones[a].Neighbors.Add(b);
                zones[b].Neighbors.Add(a);
            }

            zones[0].Explored = true;
            zones[0].Searched = false;

            int extractCount = rng.Next(1, 3);
            var candidates = Enumerable.Range(n / 2, n - n / 2).OrderBy(x => rng.Next()).ToList();
            for (int i = 0; i < extractCount && i < candidates.Count; i++)
            {
                zones[candidates[i]].IsExtract = true;
                zones[candidates[i]].Guard = null;
                zones[candidates[i]].HasEvent = true;
                zones[candidates[i]].Searched = false;
            }
            if (!zones.Any(z => z.IsExtract))
            {
                zones[n - 1].IsExtract = true;
                zones[n - 1].Guard = null;
                zones[n - 1].HasEvent = true;
                zones[n - 1].Searched = false;
            }

            for (int i = 1; i < n; i++)
            {
                if (zones[i].IsExtract) continue;
                int r = rng.Next(100);
                if (r < 65) zones[i].Guard = EnemyFactory.Create(rng, d);
                else if (r < 80) { zones[i].HasEvent = true; zones[i].EventDesc = RollEvent(rng); }
            }
            return zones;
        }

        public static string RollEvent(Random rng) => Events[rng.Next(Events.Length)];

        /// <summary>BFS 最短路径（用于快速前往撤离点）；不可达返回 null。</summary>
        public static List<int> FindPath(List<Zone> zones, int from, int to)
        {
            if (from == to) return new List<int>();
            var prev = new int[zones.Count];
            var visited = new bool[zones.Count];
            for (int i = 0; i < prev.Length; i++) prev[i] = -1;
            var q = new Queue<int>();
            q.Enqueue(from);
            visited[from] = true;
            while (q.Count > 0)
            {
                int c = q.Dequeue();
                foreach (var nb in zones[c].Neighbors)
                {
                    if (!visited[nb]) { visited[nb] = true; prev[nb] = c; q.Enqueue(nb); }
                }
            }
            if (!visited[to]) return null;
            var path = new List<int>();
            for (int c = to; c != from && c != -1; c = prev[c]) path.Add(c);
            path.Reverse();
            return path;
        }
    }
}
