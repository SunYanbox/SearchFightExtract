using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static SearchFightExtract.ConsoleHelper;

namespace SearchFightExtract
{
    partial class Game
    {
        readonly Random rng = new Random();
        Player player = new Player();
        List<Item> shopStock = new List<Item>();
        List<(Item item, int weight)> lootTable = new List<(Item, int)>();

        const string SaveDir = "saves";
        string SaveName = "";
        string SavePath => Path.Combine(SaveDir, SaveName + ".json");

        public Game()
        {
            InitShopAndLoot();
            SelectSave();
        }

        public void Run()
        {
            while (true)
            {
                Clear();
                Console.WriteLine("╔══════════════════════════════════╗");
                Console.WriteLine("║        搜  ·  打  ·  撤          ║");
                Console.WriteLine("║    Search · Fight · Extract      ║");
                Console.WriteLine("╚══════════════════════════════════╝");
                Console.WriteLine($"  资金:{player.Money}  材料:{player.Materials}  武器:{player.Weapon?.Name ?? "空手"}  护甲:{player.ArmorItem?.Name ?? "无"}  急救包:{player.Medkits}  维修套件:{player.RepairKits}  兴奋剂:{player.Stims}");
                Console.WriteLine($"  被动: {string.Join(", ", player.Passives.Select(s => s.Name))}");
                Console.WriteLine($"  主动: {string.Join(", ", player.Actives.Select(s => s.Name))}");
                Console.WriteLine($"  难度: {player.Difficulty}   存档: {SaveName}");
                Console.WriteLine();
                Console.WriteLine("  1. 出击");
                Console.WriteLine("  2. 仓库 / 出售");
                Console.WriteLine("  3. 商店");
                Console.WriteLine("  4. 装备管理");
                Console.WriteLine("  5. 难度设置");
                Console.WriteLine("  6. 技能管理");
                Console.WriteLine("  7. 部门升级");
                Console.WriteLine("  8. 生产");
                Console.WriteLine("  0. 退出");
                Console.Write("\n> ");
                switch (ReadLine())
                {
                    case "1": Raid(); break;
                    case "2": StashMenu(); break;
                    case "3": ShopMenu(); break;
                    case "4": EquipmentMenu(); break;
                    case "5": DifficultyMenu(); break;
                    case "6": SkillMenu(); break;
                    case "7": DepartmentMenu(); break;
                    case "8": ProductionMenu(); break;
                    case "0": SaveGame(); return;
                }
            }
        }

        // 按当前值/上限比例返回颜色：安全绿、警示黄、危险红
        internal static string ColorByRatio(double cur, double max)
        {
            if (max <= 0) return Style.BrightBlack;
            double r = cur / max;
            if (r >= 0.7) return Style.Green;
            if (r >= 0.3) return Style.Yellow;
            return Style.BrightRed;
        }

        void DifficultyMenu()
        {
            Clear();
            Console.WriteLine("════════ 难 度 设 置 ════════\n");
            Console.WriteLine("  D. 最多二套一弹，最低一套一弹；敌人生命-30%、攻击-20%；爆率降低");
            Console.WriteLine("  C. 最多四套三弹，最低一套一弹；标准");
            Console.WriteLine("  B. 最多五套四弹，最低三套二弹；敌人生命+30%、攻击+10%、护甲+10%；爆率提升");
            Console.WriteLine("  A. 最多五套五弹，最低四套三弹；敌人生命+60%、攻击+20%、护甲+20%；爆率大幅提升");
            Console.WriteLine("  S. 最多五套五弹，最低四套四弹；敌人生命+90%、攻击+30%、护甲+30%；爆率巨量提升");
            Console.WriteLine("  SS. 五套五弹；敌人生命+130%、攻击+40%、护甲+40%；爆率极高；本局背包容量+20%");
            Console.WriteLine($"\n当前难度：{player.Difficulty}");
            Console.Write("\n选择难度 (D/C/B/A/S/SS，回车取消) > ");
            var input = ReadLine().ToUpper();
            if (Enum.TryParse<Difficulty>(input, out var d)) { player.Difficulty = d; SaveGame(); }
            else if (input != "") Console.WriteLine("输入无效。");
            Pause();
        }
    }
}
