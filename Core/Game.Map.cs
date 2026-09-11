using System;
using System.Collections.Generic;
using System.Linq;

namespace SearchFightExtract
{
    partial class Game
    {
        List<Zone> GenerateMap()
        {
            string[] pool = { "废弃仓库", "宿舍楼", "油罐区", "医疗站", "办公楼", "地下车库", "雷达站", "军械库", "码头", "水处理厂", "化工厂", "监狱", "机场", "火车站", "购物中心" };
            var shuffled = pool.OrderBy(x => rng.Next()).ToList();
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
                if (r < 65) zones[i].Guard = MakeEnemy(i, n);
                else if (r < 80) { zones[i].HasEvent = true; zones[i].EventDesc = RollEvent(); }
            }
            return zones;
        }

        string RollEvent()
        {
            string[] events = {
                "发现一个陷阱，小心！", "遇到流浪商人，可以交易。", "捡到一张地图，显示附近物资。",
                "空投箱！里面有高级物资。", "辐射区，持续掉血。", "神秘信号，吸引敌人。"
            };
            return events[rng.Next(events.Length)];
        }

        Enemy MakeEnemy(int idx, int total)
        {
            var d = GetDifficultyParams(player.Difficulty);
            int armorTier = rng.Next(d.MinArmor, d.MaxArmor + 1);
            int bulletTier = rng.Next(d.MinBullet, d.MaxBullet + 1);

            double hp = (60 * armorTier + 30 * bulletTier + 40) * d.HpMult;
            double dmg = (8 * bulletTier + 8) * d.DmgMult;
            double armor = (22 * armorTier) * d.ArmorMult;

            string name = EnemyName(armorTier, bulletTier);

            int lootCount = 1 + rng.Next(0, 3);
            lootCount = Math.Max(1, (int)Math.Round(lootCount * d.LootMult));

            return new Enemy(name, hp, dmg, armor, armorTier, bulletTier, RollLoot(lootCount));
        }

        string EnemyName(int armorTier, int bulletTier)
        {
            int lvl = Math.Max(armorTier, bulletTier);
            switch (lvl)
            {
                case 1: return "拾荒者";
                case 2: return "雇佣兵";
                case 3: return "精锐雇佣兵";
                case 4: return "特遣队员";
                case 5: return "特勤干员";
                default: return "黑鹰指挥官";
            }
        }

        DifficultyParams GetDifficultyParams(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.D: return new DifficultyParams { MinArmor = 1, MaxArmor = 2, MinBullet = 1, MaxBullet = 1, HpMult = 0.7, DmgMult = 0.8, ArmorMult = 1.0, LootMult = 0.6 };
                case Difficulty.B: return new DifficultyParams { MinArmor = 3, MaxArmor = 5, MinBullet = 2, MaxBullet = 4, HpMult = 1.3, DmgMult = 1.1, ArmorMult = 1.1, LootMult = 1.3 };
                case Difficulty.A: return new DifficultyParams { MinArmor = 4, MaxArmor = 5, MinBullet = 3, MaxBullet = 5, HpMult = 1.6, DmgMult = 1.2, ArmorMult = 1.2, LootMult = 1.7 };
                case Difficulty.S: return new DifficultyParams { MinArmor = 4, MaxArmor = 5, MinBullet = 4, MaxBullet = 5, HpMult = 1.9, DmgMult = 1.3, ArmorMult = 1.3, LootMult = 2.5 };
                case Difficulty.SS: return new DifficultyParams { MinArmor = 4, MaxArmor = 5, MinBullet = 4, MaxBullet = 5, HpMult = 2.3, DmgMult = 1.4, ArmorMult = 1.4, LootMult = 3.5 };
                default: return new DifficultyParams { MinArmor = 1, MaxArmor = 4, MinBullet = 1, MaxBullet = 3, HpMult = 1.0, DmgMult = 1.0, ArmorMult = 1.0, LootMult = 1.0 };
            }
        }

        List<Item> RollLoot(int count)
        {
            var list = new List<Item>();
            for (int i = 0; i < count; i++) list.Add(RollItem());
            return list;
        }

        Item RollItem()
        {
            double mult = GetDifficultyParams(player.Difficulty).LootMult;
            int total = 0;
            var weights = new int[lootTable.Count];
            for (int i = 0; i < lootTable.Count; i++)
            {
                double w = lootTable[i].weight * (1 + (mult - 1) * (lootTable[i].item.Value / 3000.0));
                weights[i] = Math.Max(1, (int)Math.Round(w));
                total += weights[i];
            }
            int r = rng.Next(total);
            for (int i = 0; i < lootTable.Count; i++)
            {
                r -= weights[i];
                if (r < 0) return lootTable[i].item.Clone();
            }
            return lootTable[0].item.Clone();
        }

        // BFS 最短路径（用于快速前往撤离点）
        List<int> FindPath(List<Zone> zones, int from, int to)
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

    class DifficultyParams
    {
        public int MinArmor, MaxArmor, MinBullet, MaxBullet;
        public double HpMult, DmgMult, ArmorMult, LootMult;
    }
}
