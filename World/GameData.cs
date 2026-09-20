using System;
using System.Collections.Generic;

namespace SearchFightExtract
{
    /// <summary>
    /// 静态数据表：商店库存、掉落表、难度参数。
    /// 只读，不含流程；抽取时随机数由调用方传入。
    /// </summary>
    static class GameData
    {
        public static readonly List<Item> ShopStock = BuildShopStock();

        static readonly List<(Item item, int weight)> LootTable = BuildLootTable();

        /// <summary>生产配方：名称 / 消耗材料 / 消耗资金 / 产出数量。索引即菜单编号（1 起）。</summary>
        public static readonly (string name, int mat, int money, int qty)[] Recipes =
        {
            ("急救包", 3, 40, 2),
            ("装备维修套件", 3, 60, 2),
            ("肾上腺素", 20, 500, 2),
        };

        /// <summary>
        /// 部门升级到「下一级」的消耗：Lv1 用资金，Lv2 起用材料，均按指数增长。
        /// </summary>
        public static void UpgradeCost(int currentLevel, out int money, out int material)
        {
            int next = currentLevel + 1;
            if (next <= 1) { money = 500; material = 0; }
            else { money = 0; material = (int)(100 * Math.Pow(2.5, next - 2)); }
        }

        /// <summary>
        /// 按难度加权的掉落抽取：难度越高，高价值物品的权重提升越多。
        /// 返回的是副本，调用方可随意改动。
        /// </summary>
        public static Item RollItem(Random rng, Difficulty d)
        {
            double mult = Params(d).LootMult;
            int total = 0;
            var weights = new int[LootTable.Count];
            for (int i = 0; i < LootTable.Count; i++)
            {
                double w = LootTable[i].weight * (1 + (mult - 1) * (LootTable[i].item.Value / 3000.0));
                weights[i] = Math.Max(1, (int)Math.Round(w));
                total += weights[i];
            }
            int r = rng.Next(total);
            for (int i = 0; i < LootTable.Count; i++)
            {
                r -= weights[i];
                if (r < 0) return LootTable[i].item.Clone();
            }
            return LootTable[0].item.Clone();
        }

        /// <summary>连续抽取 <paramref name="count"/> 件掉落。</summary>
        public static List<Item> RollItems(Random rng, Difficulty d, int count)
        {
            var list = new List<Item>();
            for (int i = 0; i < count; i++) list.Add(RollItem(rng, d));
            return list;
        }

        public static DifficultyParams Params(Difficulty d)
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

        static List<Item> BuildShopStock()
        {
            return new List<Item>
            {
                new Item("手枪", ItemType.Weapon, 300, 2, 10, 1),
                new Item("冲锋枪", ItemType.Weapon, 900, 3, 16, 2),
                new Item("突击步枪", ItemType.Weapon, 2200, 4, 26, 3),
                new Item("狙击步枪", ItemType.Weapon, 4500, 5, 45, 4),
                new Item("精确射手步枪", ItemType.Weapon, 7500, 4, 60, 5),
                new Item("重型机枪", ItemType.Weapon, 6800, 6, 55, 5),
                new Item("反器材狙击枪", ItemType.Weapon, 12000, 6, 85, 6),
                new Item("电磁轨道枪", ItemType.Weapon, 20000, 7, 120, 7),

                new Item("一级防弹衣", ItemType.Armor, 400, 3, 20, 1),
                new Item("二级防弹衣", ItemType.Armor, 900, 3, 35, 2),
                new Item("三级防弹衣", ItemType.Armor, 1600, 4, 45, 3),
                new Item("四级防弹衣", ItemType.Armor, 3000, 5, 70, 4),
                new Item("五级防弹衣", ItemType.Armor, 6000, 5, 100, 5),
                new Item("六级防弹衣", ItemType.Armor, 10000, 6, 140, 6),

                new Item("轻型胸挂", ItemType.Rig, 500, 1, 6),
                new Item("战术胸挂", ItemType.Rig, 1500, 1, 12),
                new Item("小型背包", ItemType.Backpack, 800, 2, 10),
                new Item("中型背包", ItemType.Backpack, 2000, 2, 20),
                new Item("大型背包", ItemType.Backpack, 5000, 3, 35),

                new Item("急救包", ItemType.Medkit, 100, 1, 40),
                new Item("装备维修套件", ItemType.RepairKit, 150, 1, 50),
                new Item("肾上腺素", ItemType.Stim, 800, 1),
            };
        }

        static List<(Item, int)> BuildLootTable()
        {
            return new List<(Item, int)>
            {
                (new Item("螺丝钉", ItemType.Loot, 60, 1), 12),
                (new Item("罐头", ItemType.Loot, 150, 1), 10),
                (new Item("旧手机", ItemType.Loot, 320, 1), 12),
                (new Item("工具箱", ItemType.Loot, 480, 3), 8),
                (new Item("金链子", ItemType.Loot, 900, 1), 5),
                (new Item("军用电池", ItemType.Loot, 1100, 2), 5),
                (new Item("显卡", ItemType.Loot, 1500, 2), 3),
                (new Item("保险箱钥匙", ItemType.Loot, 2600, 1), 1),
                (new Item("黄金骷髅", ItemType.Loot, 5000, 2), 1),
                (new Item("机密文件", ItemType.Loot, 3500, 1), 2),
                (new Item("急救包", ItemType.Medkit, 100, 1, 40), 10),
                (new Item("装备维修套件", ItemType.RepairKit, 150, 1, 50), 5),
                (new Item("肾上腺素", ItemType.Stim, 800, 1), 2),
                (new Item("轻型胸挂", ItemType.Rig, 500, 1, 6), 4),
                (new Item("战术胸挂", ItemType.Rig, 1500, 1, 12), 2),
                (new Item("小型背包", ItemType.Backpack, 800, 2, 10), 3),
                (new Item("中型背包", ItemType.Backpack, 2000, 2, 20), 1),
                (new Item("手枪", ItemType.Weapon, 300, 2, 10, 1), 5),
                (new Item("冲锋枪", ItemType.Weapon, 900, 3, 16, 2), 3),
                (new Item("突击步枪", ItemType.Weapon, 2200, 4, 26, 3), 1),
                (new Item("狙击步枪", ItemType.Weapon, 4500, 5, 45, 4), 1),
                (new Item("精确射手步枪", ItemType.Weapon, 7500, 4, 60, 5), 1),
                (new Item("反器材狙击枪", ItemType.Weapon, 12000, 6, 85, 6), 1),
                (new Item("一级防弹衣", ItemType.Armor, 400, 3, 20, 1), 4),
                (new Item("二级防弹衣", ItemType.Armor, 900, 3, 35, 2), 2),
                (new Item("三级防弹衣", ItemType.Armor, 1600, 4, 45, 3), 1),
                (new Item("四级防弹衣", ItemType.Armor, 3000, 5, 70, 4), 1),
                (new Item("五级防弹衣", ItemType.Armor, 6000, 5, 100, 5), 1),
            };
        }
    }

    class DifficultyParams
    {
        public int MinArmor, MaxArmor, MinBullet, MaxBullet;
        public double HpMult, DmgMult, ArmorMult, LootMult;
    }
}
