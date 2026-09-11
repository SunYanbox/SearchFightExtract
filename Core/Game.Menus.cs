using System;
using System.Linq;
using static SearchFightExtract.ConsoleHelper;

namespace SearchFightExtract
{
    partial class Game
    {
        void StashMenu()
        {
            while (true)
            {
                Clear();
                Console.WriteLine("════════ 仓 库 ════════");
                Console.WriteLine($"资金：{player.Money}   材料：{player.Materials}\n");
                if (player.Stash.Count == 0) Console.WriteLine("  （空）");
                else for (int i = 0; i < player.Stash.Count; i++) Console.WriteLine($"  {i + 1,2}. {player.Stash[i]}");
                Console.WriteLine("\n  输入编号出售单件");
                Console.WriteLine("  a 全部出售 / e 出售全部装备 / l 出售全部物资 / d 分解全部物资 / 0 返回");
                Console.Write("> ");
                string cmd = ReadLine();
                if (cmd == "0") return;
                if (cmd == "a" || cmd == "A")
                {
                    int sum = player.Stash.Sum(x => x.Value);
                    player.Money += sum;
                    Console.WriteLine($"\n全部出售，获得 {sum} 元。");
                    player.Stash.Clear();
                    SaveGame();
                    Pause();
                    continue;
                }
                if (cmd == "e" || cmd == "E")
                {
                    var equip = player.Stash.Where(x => x.Type == ItemType.Weapon || x.Type == ItemType.Armor || x.Type == ItemType.Rig || x.Type == ItemType.Backpack).ToList();
                    if (equip.Count == 0) { Console.WriteLine("\n没有可出售的装备。"); Pause(); continue; }
                    int sum = equip.Sum(x => x.Value);
                    foreach (var it in equip) player.Stash.Remove(it);
                    player.Money += sum;
                    Console.WriteLine($"\n出售 {equip.Count} 件装备，获得 {sum} 元。");
                    SaveGame();
                    Pause();
                    continue;
                }
                if (cmd == "l" || cmd == "L")
                {
                    var loots = player.Stash.Where(x => x.Type == ItemType.Loot).ToList();
                    if (loots.Count == 0) { Console.WriteLine("\n没有可出售的物资。"); Pause(); continue; }
                    int sum = loots.Sum(x => x.Value);
                    foreach (var it in loots) player.Stash.Remove(it);
                    player.Money += sum;
                    Console.WriteLine($"\n出售 {loots.Count} 件物资，获得 {sum} 元。");
                    SaveGame();
                    Pause();
                    continue;
                }
                if (cmd == "d" || cmd == "D")
                {
                    var loots = player.Stash.Where(x => x.Type == ItemType.Loot).ToList();
                    if (loots.Count == 0) { Console.WriteLine("\n没有可分解的物资（仅能分解 Loot 类）。"); Pause(); continue; }
                    int gained = 0;
                    foreach (var it in loots)
                    {
                        gained += Math.Max(1, it.Value / 50);
                        player.Stash.Remove(it);
                    }
                    player.Materials += gained;
                    Console.WriteLine($"\n分解 {loots.Count} 件物资，获得 {gained} 材料（共 {player.Materials}）。");
                    SaveGame();
                    Pause();
                    continue;
                }
                if (int.TryParse(cmd, out int idx) && idx >= 1 && idx <= player.Stash.Count)
                {
                    var it = player.Stash[idx - 1];
                    player.Stash.RemoveAt(idx - 1);
                    player.Money += it.Value;
                    Console.WriteLine($"\n出售 {it.Name}，获得 {it.Value} 元。");
                    SaveGame();
                    Pause();
                }
            }
        }

        void ShopMenu()
        {
            while (true)
            {
                Clear();
                Console.WriteLine("════════ 商 店 ════════");
                Console.WriteLine($"资金：{player.Money}\n");
                for (int i = 0; i < shopStock.Count; i++)
                    Console.WriteLine($"  {i + 1}. {shopStock[i]}  —— {shopStock[i].Value} 元");
                Console.WriteLine("\n  0. 返回");
                Console.Write("> ");
                string cmd = ReadLine();
                if (cmd == "0") return;
                if (!int.TryParse(cmd, out int idx) || idx < 1 || idx > shopStock.Count) continue;
                var it = shopStock[idx - 1];

                // 消耗品支持批量购买
                bool isConsumable = it.Type == ItemType.Medkit || it.Type == ItemType.RepairKit || it.Type == ItemType.Stim;
                if (isConsumable)
                {
                    int maxAfford = it.Value > 0 ? player.Money / it.Value : 0;
                    Console.Write($"\n购买数量（1~{maxAfford}，回车买1）> ");
                    var qStr = ReadLine();
                    int qty = 1;
                    if (qStr != "" && (!int.TryParse(qStr, out qty) || qty < 1)) { Console.WriteLine("输入无效。"); Pause(); continue; }
                    if (qty > maxAfford) qty = maxAfford;
                    if (qty <= 0) { Console.WriteLine("资金不足！"); Pause(); continue; }
                    player.Money -= it.Value * qty;
                    if (it.Type == ItemType.Medkit) { player.Medkits += qty; Console.WriteLine($"\n购买急救包 ×{qty}，现有 {player.Medkits} 个。"); }
                    else if (it.Type == ItemType.RepairKit) { player.RepairKits += qty; Console.WriteLine($"\n购买维修套件 ×{qty}，现有 {player.RepairKits} 个。"); }
                    else { player.Stims += qty; Console.WriteLine($"\n购买肾上腺素 ×{qty}，现有 {player.Stims} 个。"); }
                    SaveGame();
                    Pause();
                    continue;
                }

                if (player.Money < it.Value) { Console.WriteLine("\n资金不足！"); Pause(); continue; }
                player.Money -= it.Value;
                if (it.Type == ItemType.Weapon)
                {
                    if (player.Weapon != null) player.Stash.Add(player.Weapon);
                    player.Weapon = it.Clone();
                    Console.WriteLine($"\n已装备 {it.Name}。");
                }
                else if (it.Type == ItemType.Armor)
                {
                    if (player.ArmorItem != null) player.Stash.Add(player.ArmorItem);
                    player.ArmorItem = it.Clone();
                    player.Armor = it.Power;
                    Console.WriteLine($"\n已装备 {it.Name}。");
                }
                else if (it.Type == ItemType.Rig)
                {
                    if (player.Rig != null) player.Stash.Add(player.Rig);
                    player.Rig = it.Clone();
                    Console.WriteLine($"\n已装备 {it.Name}。");
                }
                else if (it.Type == ItemType.Backpack)
                {
                    if (player.Bag != null) player.Stash.Add(player.Bag);
                    player.Bag = it.Clone();
                    Console.WriteLine($"\n已装备 {it.Name}。");
                }
                SaveGame();
                Pause();
            }
        }

        void EquipmentMenu()
        {
            while (true)
            {
                Clear();
                Console.WriteLine("════════ 装备管理 ════════");
                Console.WriteLine($"当前武器：{player.Weapon?.Name ?? "空手"}");
                Console.WriteLine($"当前护甲：{player.ArmorItem?.Name ?? "无"} (耐久 {player.Armor:F1}/{player.MaxArmor:F1})");
                Console.WriteLine($"当前胸挂：{player.Rig?.Name ?? "无"}   当前背包：{player.Bag?.Name ?? "无"}   （总容量 {player.MaxCapacity}）");
                Console.WriteLine("\n仓库中的装备：");
                var equipables = player.Stash.Where(i => i.Type == ItemType.Weapon || i.Type == ItemType.Armor || i.Type == ItemType.Rig || i.Type == ItemType.Backpack).ToList();
                if (equipables.Count == 0) Console.WriteLine("  （无）");
                for (int i = 0; i < equipables.Count; i++) Console.WriteLine($"  {i + 1}. {equipables[i]}");
                Console.WriteLine("\n输入编号装备 / 0 返回");
                Console.Write("> ");
                var cmd = ReadLine();
                if (cmd == "0") return;
                if (int.TryParse(cmd, out int idx) && idx >= 1 && idx <= equipables.Count)
                {
                    var it = equipables[idx - 1];
                    if (it.Type == ItemType.Weapon)
                    {
                        if (player.Weapon != null) player.Stash.Add(player.Weapon);
                        player.Stash.Remove(it);
                        player.Weapon = it;
                        Console.WriteLine($"已装备 {it.Name}");
                    }
                    else if (it.Type == ItemType.Armor)
                    {
                        if (player.ArmorItem != null) player.Stash.Add(player.ArmorItem);
                        player.Stash.Remove(it);
                        player.ArmorItem = it;
                        player.Armor = it.Power;
                        Console.WriteLine($"已装备 {it.Name}");
                    }
                    else if (it.Type == ItemType.Rig)
                    {
                        if (player.Rig != null) player.Stash.Add(player.Rig);
                        player.Stash.Remove(it);
                        player.Rig = it;
                        Console.WriteLine($"已装备 {it.Name}（总容量 {player.MaxCapacity}）");
                    }
                    else if (it.Type == ItemType.Backpack)
                    {
                        if (player.Bag != null) player.Stash.Add(player.Bag);
                        player.Stash.Remove(it);
                        player.Bag = it;
                        Console.WriteLine($"已装备 {it.Name}（总容量 {player.MaxCapacity}）");
                    }
                    SaveGame();
                    Pause();
                }
            }
        }

        // 生产配方：名称 / 消耗材料 / 消耗资金 / 产出数量
        (string name, int mat, int money, int qty)[] Recipes => new[]
        {
            ("急救包", 3, 40, 2),
            ("装备维修套件", 3, 60, 2),
            ("肾上腺素", 20, 500, 2),
        };

        void ProductionMenu()
        {
            while (true)
            {
                Clear();
                Console.WriteLine("════════ 生 产 车 间 ════════");
                Console.WriteLine($"资金：{player.Money}   材料：{player.Materials}");
                Console.WriteLine("（材料来自仓库分解物资，与出售相互独立）\n");

                var rec = Recipes;
                for (int i = 0; i < rec.Length; i++)
                    Console.WriteLine($"  {i + 1}. 生产 {rec[i].name} ×{rec[i].qty}   —— 需 {rec[i].mat} 材料 + {rec[i].money} 元");
                Console.WriteLine("\n  0. 返回");
                Console.Write("> ");

                string cmd = ReadLine();
                if (cmd == "0") return;
                if (!int.TryParse(cmd, out int idx) || idx < 1 || idx > rec.Length) continue;

                var r = rec[idx - 1];
                if (player.Materials < r.mat) { Console.WriteLine($"\n材料不足（需 {r.mat}，拥有 {player.Materials}）。"); Pause(); continue; }
                if (player.Money < r.money) { Console.WriteLine($"\n资金不足（需 {r.money}，拥有 {player.Money}）。"); Pause(); continue; }

                player.Materials -= r.mat;
                player.Money -= r.money;
                switch (idx)
                {
                    case 1: player.Medkits += r.qty; break;
                    case 2: player.RepairKits += r.qty; break;
                    case 3: player.Stims += r.qty; break;
                }
                SaveGame();
                Console.WriteLine($"\n生产成功：{r.name} ×{r.qty}。");
                Pause();
            }
        }

        // 升级到 nextLevel = currentLevel + 1 的消耗：
        // nextLevel 1 → 金钱；nextLevel >= 2 → 材料。均按指数增长。
        void UpgradeCost(int currentLevel, out int money, out int material)
        {
            int next = currentLevel + 1;
            if (next <= 1) { money = 500; material = 0; }
            else { money = 0; material = (int)(100 * Math.Pow(2.5, next - 2)); }
        }

        void PrintDeptLine(int num, string name, int lv, int maxLv, string effect)
        {
            string costText;
            if (lv >= maxLv) costText = "已满级";
            else
            {
                UpgradeCost(lv, out int m, out int mat);
                costText = m > 0 ? $"升级需 {m} 元" : $"升级需 {mat} 材料";
            }
            Console.WriteLine($"  {num}. {name}  Lv{lv}  {effect}   [{costText}]");
        }

        void DepartmentMenu()
        {
            while (true)
            {
                Clear();
                Console.WriteLine("════════ 部 门 升 级 ════════");
                Console.WriteLine($"资金：{player.Money}   材料：{player.Materials}\n");

                PrintDeptLine(1, "天赋部门", player.TalentLevel, 3, $"被动上限 {player.PassiveCap}");
                PrintDeptLine(2, "主动技能部门", player.ActiveSkillLevel, 3, $"主动上限 {player.ActiveCap}");
                PrintDeptLine(3, "战斗部门", player.CombatLevel, 5, $"伤害+{player.CombatDamageBonus * 100:F0}%  吸血{player.CombatLifeSteal * 100:F0}%  护甲回复{player.CombatArmorRestore * 100:F0}%");
                PrintDeptLine(4, "生存部门", player.SurvivalLevel, 5, $"生命+{player.SurvivalHpBonus * 100:F0}%  护甲损耗-{player.SurvivalArmorRed * 100:F0}%");

                Console.WriteLine("\n 输入编号升级 / 0 返回");
                Console.Write("> ");
                string cmd = ReadLine();
                if (cmd == "0") return;

                int dept = cmd == "1" ? 0 : cmd == "2" ? 1 : cmd == "3" ? 2 : cmd == "4" ? 3 : -1;
                if (dept < 0) continue;

                int curLevel = dept == 0 ? player.TalentLevel : dept == 1 ? player.ActiveSkillLevel : dept == 2 ? player.CombatLevel : player.SurvivalLevel;
                int maxLevel = dept <= 1 ? 3 : 5;
                if (curLevel >= maxLevel) { Console.WriteLine("\n该部门已满级。"); Pause(); continue; }

                UpgradeCost(curLevel, out int moneyCost, out int matCost);
                if (moneyCost > 0 && player.Money < moneyCost) { Console.WriteLine($"\n资金不足（需要 {moneyCost}，拥有 {player.Money}）。"); Pause(); continue; }
                if (matCost > 0 && player.Materials < matCost) { Console.WriteLine($"\n材料不足（需要 {matCost}，拥有 {player.Materials}）。"); Pause(); continue; }

                player.Money -= moneyCost;
                player.Materials -= matCost;
                if (dept == 0) player.TalentLevel++;
                else if (dept == 1) player.ActiveSkillLevel++;
                else if (dept == 2) player.CombatLevel++;
                else player.SurvivalLevel++;
                player.RecomputeStats();
                SaveGame();
                Console.WriteLine("\n升级成功！");
                Pause();
            }
        }
    }
}
