using System;
using System.Collections.Generic;
using System.Linq;
using static SearchFightExtract.ConsoleHelper;

namespace SearchFightExtract
{
    partial class Game
    {
        void Raid()
        {
            Clear();
            SelectLoadout();

            player.Hp = player.MaxHp;
            player.RaidCapacityMult = player.Difficulty == Difficulty.SS ? 1.2 : 1.0;   // SS 难度背包容量 +20%
            player.Armor = player.MaxArmor;
            player.Shield = 0;
            player.TempArmor = 0;
            player.ArmorBreakDRTurns = 0;
            player.FirstAidDRTurns = 0;
            player.Backpack.Clear();
            player.TurnCounter = 0;
            foreach (var s in player.Actives) s.CurrentCooldown = 0;

            var zones = GenerateMap();
            int cur = 0, prev = -1;

            Clear();
            Console.WriteLine("════════ 进 入 战 区 ════════");
            Console.WriteLine($"你从【{zones[0].Name}】潜入。");
            Console.WriteLine("找到撤离点并撤离，才能带出战利品。");
            Pause();

            while (true)
            {
                Clear();
                ShowHud(zones, cur);
                var z = zones[cur];

                if (z.Guard != null && z.Guard.Hp > 0 && !z.Searched)
                {
                    var res = Combat(z.Guard, prev);
                    if (res == CombatResult.Dead) { OnDeath(); return; }
                    if (res == CombatResult.Fled) { cur = prev; continue; }
                    Console.WriteLine($"\n你击败了 {z.Guard.Name}！开始搜刮尸体...");
                    foreach (var it in z.Guard.Loot) TryPickup(it);
                    z.Guard = null;
                    Pause();
                    continue;
                }

                Console.WriteLine();
                if (z.IsExtract) Console.WriteLine("  ★ 这里有撤离通道，可以撤离！");
                Console.WriteLine("  1. 移动");
                Console.WriteLine("  2. 搜索");
                Console.WriteLine($"  3. 使用急救包（携带 {player.RaidMedkits}）");
                Console.WriteLine($"  4. 使用维修套件（携带 {player.RaidRepairKits}）");
                Console.WriteLine($"  5. 使用兴奋剂（携带 {player.RaidStims}）");
                Console.WriteLine("  6. 查看背包/切换装备");
                Console.WriteLine("  7. 撤离");
                Console.Write("\n> ");

                switch (ReadLine())
                {
                    case "1": MoveMenu(zones, ref cur, ref prev); break;
                    case "2": SearchZone(z); Pause(); break;
                    case "3": UseMedkit(); Pause(); break;
                    case "4": UseRepairKit(); Pause(); break;
                    case "5": UseStim(); Pause(); break;
                    case "6": ShowBackpack(); Pause(); break;
                    case "7":
                        if (z.IsExtract) { ExtractResult(); return; }
                        Console.WriteLine("这里不是撤离点。"); Pause(); break;
                }
            }
        }

        void MoveMenu(List<Zone> zones, ref int cur, ref int prev)
        {
            var z = zones[cur];
            while (true)
            {
                int curNow = cur;
                Console.WriteLine("\n可前往：");
                for (int i = 0; i < z.Neighbors.Count; i++)
                {
                    var nz = zones[z.Neighbors[i]];
                    string mark = nz.Explored ? nz.Name : "*";
                    string ex = (nz.IsExtract && nz.Explored) ? " [已知撤离点]" : "";
                    Console.WriteLine($"  {i + 1}. {mark}{ex}");
                }

                // 探索超过 70% 区域后，即使未发现撤离点，也能直接定位到撤离点路径
                int exploredCount = zones.Count(x => x.Explored);
                bool mapMostlyExplored = exploredCount >= zones.Count * 0.7;
                var known = Enumerable.Range(0, zones.Count)
                    .Where(i => zones[i].IsExtract && (zones[i].Explored || mapMostlyExplored) && i != curNow).ToList();
                int fastIdx = z.Neighbors.Count + 1;
                if (known.Count > 0)
                    Console.WriteLine($"  {fastIdx}. 快速前往{(mapMostlyExplored ? "撤离点（已探明区域≥70%）" : "已知撤离点")}");
                Console.WriteLine("  0. 取消");
                Console.Write("> ");

                var input = ReadLine();
                if (input == "0") return;
                if (!int.TryParse(input, out int sel)) continue;

                if (sel >= 1 && sel <= z.Neighbors.Count)
                {
                    prev = cur;
                    cur = z.Neighbors[sel - 1];
                    zones[cur].Explored = true;
                    OnEnterZone(zones, cur);
                    Pause();
                    return;
                }
                else if (known.Count > 0 && sel == fastIdx)
                {
                    Console.WriteLine("\n可选撤离点：");
                    for (int i = 0; i < known.Count; i++)
                        Console.WriteLine($"  {i + 1}. {zones[known[i]].Name}");
                    Console.WriteLine("  0. 取消");
                    Console.Write("> ");
                    if (!int.TryParse(ReadLine(), out int exSel) || exSel < 1 || exSel > known.Count) continue;

                    int target = known[exSel - 1];
                    var path = FindPath(zones, cur, target);
                    if (path == null || path.Count == 0) { Console.WriteLine("无法到达该撤离点。"); Pause(); return; }

                    bool stopped = false;
                    foreach (var step in path)
                    {
                        prev = cur;
                        cur = step;
                        zones[cur].Explored = true;
                        Console.WriteLine($"\n→ 进入【{zones[cur].Name}】");
                        if (OnEnterZone(zones, cur)) { stopped = true; break; }
                    }
                    if (!stopped) Console.WriteLine("\n已抵达撤离点，准备撤离。");
                    Pause();
                    return;
                }
            }
        }

        // 返回 true 表示中途遭遇敌人，需要中断连续移动
        bool OnEnterZone(List<Zone> zones, int cur)
        {
            foreach (var s in player.Actives) if (s.CurrentCooldown > 0) s.CurrentCooldown--;

            if (player.HasPassive(SkillType.BeeMedic))
            {
                player.Hp = Math.Min(player.MaxHp, player.Hp + 30);
                double layer = player.MaxHp * 0.45;
                double cap = layer * 3;   // 至多三格
                player.Shield = Math.Min(cap, player.Shield + layer);
                Console.WriteLine(Style.Paint($"蜂医：恢复30生命，获得护盾（{player.Shield:F1}/{cap:F1}）。", Style.Green));
            }

            var nz = zones[cur];
            if (!nz.IsExtract && nz.Guard == null && !nz.Searched)
            {
                int chance = player.HasPassive(SkillType.Nimble) ? 18 : 35;
                if (rng.Next(100) < chance)
                {
                    nz.Guard = MakeEnemy(cur, zones.Count);
                    Console.WriteLine("你惊动了此地的敌人！");
                    return true;
                }
            }
            return false;
        }

        void SelectLoadout()
        {
            Console.WriteLine("════════ 出 击 准 备 ════════");
            Console.WriteLine("分配本次携带的消耗品（撤离可带回剩余，阵亡则丢失携带部分）：\n");
            Console.WriteLine($"库存：急救包 {player.Medkits}  维修套件 {player.RepairKits}  兴奋剂 {player.Stims}\n");

            player.RaidMedkits = AskCount("携带急救包", player.Medkits);
            player.RaidRepairKits = AskCount("携带维修套件", player.RepairKits);
            player.RaidStims = AskCount("携带兴奋剂", player.Stims);

            player.Medkits -= player.RaidMedkits;
            player.RepairKits -= player.RaidRepairKits;
            player.Stims -= player.RaidStims;
        }

        int AskCount(string label, int max)
        {
            Console.Write($"{label}（0~{max}，回车默认全带）> ");
            var s = ReadLine();
            if (s == "") return max;
            if (int.TryParse(s, out int v) && v >= 0 && v <= max) return v;
            Console.WriteLine("输入无效，按 0 处理。");
            return 0;
        }

        void UseMedkit()
        {
            if (player.RaidMedkits <= 0) Console.WriteLine("没有携带急救包。");
            else if (player.Hp >= player.MaxHp) Console.WriteLine("状态很好。");
            else
            {
                player.RaidMedkits--;
                player.Hp = Math.Min(player.MaxHp, player.Hp + 40);
                Console.WriteLine(Style.Paint("恢复40生命。", Style.Green));
            }
        }

        void UseRepairKit()
        {
            if (player.RaidRepairKits <= 0) Console.WriteLine("没有携带维修套件。");
            else if (player.ArmorItem == null) Console.WriteLine("没有护甲可修。");
            else
            {
                player.RaidRepairKits--;
                player.Armor = Math.Min(player.MaxArmor, player.Armor + 50);
                Console.WriteLine(Style.Paint("护甲恢复50。", Style.Magenta));
            }
        }

        void UseStim()
        {
            if (player.RaidStims <= 0) { Console.WriteLine("没有携带兴奋剂。"); return; }
            player.RaidStims--;
            foreach (var s in player.Actives) s.CurrentCooldown = 0;
            Console.WriteLine("肾上腺素：所有主动技能冷却已清除！");
        }

        void ShowHud(List<Zone> zones, int cur)
        {
            Console.WriteLine("──────────────────────────────────");
            string hp = Style.Paint($"{player.Hp:F1}/{player.MaxHp:F1}", ColorByRatio(player.Hp, player.MaxHp));
            string ar = Style.Paint($"{player.Armor:F1}/{player.MaxArmor:F1}", ColorByRatio(player.Armor, player.MaxArmor));
            string sh = Style.Paint($"{player.Shield:F1}", Style.BrightBlack);
            string ta = player.TempArmor > 0 ? Style.Paint($"  临时护甲 {player.TempArmor:F1}", Style.Cyan) : "";
            string iw = player.ArmorBreakDRTurns > 0 ? Style.Paint($"  [铁壁免伤 {player.ArmorBreakDRTurns}回合]", Style.BrightYellow) : "";
            Console.WriteLine($" HP {hp}  护甲 {ar}  护盾 {sh}{ta}{iw}  背包 {player.UsedCap}/{player.MaxCapacity}");
            Console.WriteLine($" 武器 {player.Weapon?.Name ?? "空手"}  胸挂 {player.Rig?.Name ?? "无"}  背包 {player.Bag?.Name ?? "无"}  资金 {player.Money}  难度 {player.Difficulty}");
            Console.WriteLine($" 携带：急救包 {player.RaidMedkits}  维修套件 {player.RaidRepairKits}  兴奋剂 {player.RaidStims}");
            Console.WriteLine("──────────────────────────────────");
            Console.WriteLine($" 当前位置：【{zones[cur].Name}】 {(zones[cur].Searched ? "(已搜)" : "")} {(zones[cur].IsExtract ? "[撤离点]" : "")}");
        }

        void ShowBackpack()
        {
            Console.WriteLine($"\n背包 ({player.UsedCap}/{player.MaxCapacity})：");
            if (player.Backpack.Count == 0) { Console.WriteLine("  （空）"); return; }
            for (int i = 0; i < player.Backpack.Count; i++)
                Console.WriteLine($"  {i + 1}. {player.Backpack[i]}");
            Console.WriteLine("\n输入编号装备/使用，或 0 返回");
            Console.Write("> ");
            var cmd = ReadLine();
            if (cmd == "0") return;
            if (int.TryParse(cmd, out int idx) && idx >= 1 && idx <= player.Backpack.Count)
            {
                var it = player.Backpack[idx - 1];
                if (it.Type == ItemType.Weapon)
                {
                    if (player.Weapon != null) player.Backpack.Add(player.Weapon);
                    player.Backpack.Remove(it);
                    player.Weapon = it;
                    Console.WriteLine($"已装备 {it.Name}");
                }
                else if (it.Type == ItemType.Armor)
                {
                    if (player.ArmorItem != null) player.Backpack.Add(player.ArmorItem);
                    player.Backpack.Remove(it);
                    player.ArmorItem = it;
                    player.Armor = it.Power;
                    Console.WriteLine($"已装备 {it.Name}");
                }
                else if (it.Type == ItemType.Rig)
                {
                    if (player.Rig != null) player.Backpack.Add(player.Rig);
                    player.Backpack.Remove(it);
                    player.Rig = it;
                    Console.WriteLine($"已装备 {it.Name}（容量 {player.UsedCap}/{player.MaxCapacity}）");
                }
                else if (it.Type == ItemType.Backpack)
                {
                    if (player.Bag != null) player.Backpack.Add(player.Bag);
                    player.Backpack.Remove(it);
                    player.Bag = it;
                    Console.WriteLine($"已装备 {it.Name}（容量 {player.UsedCap}/{player.MaxCapacity}）");
                }
                else if (it.Type == ItemType.Medkit)
                {
                    player.Backpack.Remove(it);
                    player.RaidMedkits++;
                    Console.WriteLine($"急救包已放入医疗包（携带 {player.RaidMedkits}）。");
                }
                else if (it.Type == ItemType.RepairKit)
                {
                    player.Backpack.Remove(it);
                    player.RaidRepairKits++;
                    Console.WriteLine($"维修套件已放入工具包（携带 {player.RaidRepairKits}）。");
                }
                else if (it.Type == ItemType.Stim)
                {
                    player.Backpack.Remove(it);
                    player.RaidStims++;
                    Console.WriteLine($"兴奋剂已放入医疗包（携带 {player.RaidStims}）。");
                }
            }
        }

        void SearchZone(Zone z)
        {
            if (z.Searched) { Console.WriteLine("这里已经搜过了。"); return; }
            if (z.Guard != null && z.Guard.Hp > 0) { Console.WriteLine("先解决敌人！"); return; }
            z.Searched = true;
            Console.WriteLine("你开始搜索...");
            int cnt = rng.Next(1, 4);
            if (player.HasPassive(SkillType.Scavenger)) cnt++;
            for (int i = 0; i < cnt; i++) TryPickup(RollItem());
            if (z.HasEvent)
            {
                Console.WriteLine($"\n事件：{z.EventDesc}");
                HandleEvent(z);
                z.HasEvent = false;
            }
            else if (rng.Next(100) < 20)
            {
                string ev = RollEvent();
                Console.WriteLine($"\n突发事件：{ev}");
                HandleEvent(new Zone { EventDesc = ev });
            }
            foreach (var s in player.Actives) if (s.CurrentCooldown > 0) s.CurrentCooldown--;
        }

        void HandleEvent(Zone z)
        {
            switch (z.EventDesc)
            {
                case "发现一个陷阱，小心！":
                    double dmg = rng.Next(10, 30);
                    player.Hp -= dmg;
                    Console.WriteLine($"你触发了陷阱，损失 {Style.Paint($"{dmg:F1}", Style.BrightRed)} 生命。");
                    break;
                case "遇到流浪商人，可以交易。":
                    if (player.Money >= 200)
                    {
                        player.Money -= 200;
                        var it = RollItem();
                        TryPickup(it);
                        Console.WriteLine($"你花了200元购买了 {it.Name}。");
                    }
                    else Console.WriteLine("但你钱不够。");
                    break;
                case "捡到一张地图，显示附近物资。":
                    for (int i = 0; i < 2; i++) TryPickup(RollItem());
                    break;
                case "空投箱！里面有高级物资。":
                    TryPickup(new Item("黄金骷髅", ItemType.Loot, 5000, 2));
                    TryPickup(new Item("五级防弹衣", ItemType.Armor, 6000, 5, 100, 5));
                    break;
                case "辐射区，持续掉血。":
                    double rad = rng.Next(5, 15);
                    player.Hp -= rad;
                    Console.WriteLine($"你受到辐射，损失 {Style.Paint($"{rad:F1}", Style.BrightRed)} 生命。");
                    break;
                case "神秘信号，吸引敌人。":
                    Console.WriteLine("你被敌人发现了！");
                    var e = MakeEnemy(1, 2);
                    var res = Combat(e, -1);
                    if (res == CombatResult.Dead) { OnDeath(); }
                    break;
            }
            if (player.Hp <= 0) OnDeath();
        }

        void TryPickup(Item it)
        {
            switch (it.Type)
            {
                case ItemType.Medkit:
                    player.RaidMedkits++;
                    Console.WriteLine($"  + 急救包 ×1（携带 {player.RaidMedkits}）");
                    return;
                case ItemType.RepairKit:
                    player.RaidRepairKits++;
                    Console.WriteLine($"  + 维修套件 ×1（携带 {player.RaidRepairKits}）");
                    return;
                case ItemType.Stim:
                    player.RaidStims++;
                    Console.WriteLine($"  + 肾上腺素 ×1（携带 {player.RaidStims}）");
                    return;
                case ItemType.Weapon:
                    if (player.Weapon == null || it.Power > player.Weapon.Power)
                    {
                        var old = player.Weapon;
                        player.Weapon = it;
                        Console.WriteLine($"  + 换上武器 {it.Name}（伤害{it.Power:F1}）");
                        if (old != null) AddToBackpack(old);
                        return;
                    }
                    break;
                case ItemType.Armor:
                    if (player.ArmorItem == null || it.Power > player.ArmorItem.Power)
                    {
                        var old = player.ArmorItem;
                        player.ArmorItem = it;
                        player.Armor = it.Power;
                        Console.WriteLine($"  + 换上护甲 {it.Name}（护甲{it.Power:F1}）");
                        if (old != null) AddToBackpack(old);
                        return;
                    }
                    break;
                case ItemType.Rig:
                    if (player.Rig == null || it.Power > player.Rig.Power)
                    {
                        var old = player.Rig;
                        player.Rig = it;
                        Console.WriteLine($"  + 换上胸挂 {it.Name}（容量+{it.Power:F0}，当前 {player.UsedCap}/{player.MaxCapacity}）");
                        if (old != null) AddToBackpack(old);
                        return;
                    }
                    break;
                case ItemType.Backpack:
                    if (player.Bag == null || it.Power > player.Bag.Power)
                    {
                        var old = player.Bag;
                        player.Bag = it;
                        Console.WriteLine($"  + 换上背包 {it.Name}（容量+{it.Power:F0}，当前 {player.UsedCap}/{player.MaxCapacity}）");
                        if (old != null) AddToBackpack(old);
                        return;
                    }
                    break;
            }
            AddToBackpack(it);
        }

        void AddToBackpack(Item newItem)
        {
            if (player.UsedCap + newItem.Weight <= player.MaxCapacity)
            {
                player.Backpack.Add(newItem);
                Console.WriteLine($"  + {newItem.Name}（价值{newItem.Value} 重{newItem.Weight}）");
                return;
            }

            int weightNeeded = player.UsedCap + newItem.Weight - player.MaxCapacity;
            var sortedBackpack = player.Backpack
                .OrderBy(x => (double)x.Value / x.Weight)
                .ThenBy(x => x.Value)
                .ToList();

            var itemsToDrop = new List<Item>();
            int freedWeight = 0;
            int droppedValue = 0;
            double maxDroppedRatio = 0;

            foreach (var item in sortedBackpack)
            {
                itemsToDrop.Add(item);
                freedWeight += item.Weight;
                droppedValue += item.Value;
                maxDroppedRatio = Math.Max(maxDroppedRatio, (double)item.Value / item.Weight);

                if (freedWeight >= weightNeeded) break;
            }

            if (freedWeight >= weightNeeded)
            {
                double newRatio = (double)newItem.Value / newItem.Weight;
                if (newRatio > maxDroppedRatio && newItem.Value >= droppedValue * 0.6)
                {
                    foreach (var item in itemsToDrop)
                    {
                        player.Backpack.Remove(item);
                    }
                    player.Backpack.Add(newItem);

                    string droppedNames = string.Join("、", itemsToDrop.Select(i => $"{i.Name}(价值{i.Value} 重{i.Weight})"));
                    Console.WriteLine($"  ⚠ 背包已满，自动替换丢弃：{droppedNames}");
                    Console.WriteLine($"  + 拾取 {newItem.Name}（价值{newItem.Value} 重{newItem.Weight}）");
                    return;
                }
                else
                {
                    string reason = newRatio <= maxDroppedRatio ? "新物品性价比不高于被替换物品" : "新物品价值低于丢弃物品总价值的60%";
                    Console.WriteLine($"  × 背包已满，放弃拾取 {newItem.Name}（原因：{reason}）");
                    return;
                }
            }

            Console.WriteLine($"  × 背包已满，丢弃 {newItem.Name}（无法腾出足够空间）");
        }

        void ExtractResult()
        {
            Clear();
            Console.WriteLine("════════ 撤 离 成 功 ════════\n");
            int total = 0;
            foreach (var it in player.Backpack) { player.Stash.Add(it); total += it.Value; }
            int count = player.Backpack.Count;
            player.Backpack.Clear();
            Console.WriteLine($"安全带出 {count} 件物资，总价值 {total}。");

            // 归还携带的剩余消耗品
            player.Medkits += player.RaidMedkits;
            player.RepairKits += player.RaidRepairKits;
            player.Stims += player.RaidStims;
            player.RaidMedkits = 0;
            player.RaidRepairKits = 0;
            player.RaidStims = 0;

            Console.WriteLine($"当前资金：{player.Money}");
            SaveGame();
            Pause();
        }

        void OnDeath()
        {
            Clear();
            Console.WriteLine("════════ 你 阵 亡 了 ════════\n");
            Console.WriteLine($"丢失背包物资 {player.Backpack.Count} 件");
            player.Backpack.Clear();
            if (player.Weapon != null) Console.WriteLine($"丢失武器：{player.Weapon.Name}");
            player.Weapon = null;
            if (player.ArmorItem != null) Console.WriteLine($"丢失护甲：{player.ArmorItem.Name}");
            player.ArmorItem = null;
            player.Armor = 0;
            if (player.Rig != null) Console.WriteLine($"丢失胸挂：{player.Rig.Name}");
            player.Rig = null;
            if (player.Bag != null) Console.WriteLine($"丢失背包：{player.Bag.Name}");
            player.Bag = null;
            if (player.RaidMedkits > 0) Console.WriteLine($"丢失急救包 ×{player.RaidMedkits}");
            player.RaidMedkits = 0;
            if (player.RaidRepairKits > 0) Console.WriteLine($"丢失维修套件 ×{player.RaidRepairKits}");
            player.RaidRepairKits = 0;
            if (player.RaidStims > 0) Console.WriteLine($"丢失兴奋剂 ×{player.RaidStims}");
            player.RaidStims = 0;
            Console.WriteLine("\n仓库中的物品不受影响。");
            SaveGame();
            Pause();
        }
    }
}
