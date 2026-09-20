using System;
using System.Collections.Generic;
using System.Linq;

namespace SearchFightExtract
{
    /// <summary>
    /// 拾取、背包与消耗品规则：只改玩家状态，并把要显示的内容作为文本行返回。
    /// 与战斗结算同样不直接输出，便于单独验证。
    /// </summary>
    static class Inventory
    {
        /// <summary>
        /// 拾取一件物品：装备类若优于当前装备则自动换上（换下的旧装备进背包），
        /// 消耗品直接计入携带数，其余进背包。
        /// </summary>
        public static List<string> Pickup(Player p, Item it)
        {
            switch (it.Type)
            {
                case ItemType.Medkit:
                    p.RaidMedkits++;
                    return One($"  + 急救包 ×1（携带 {p.RaidMedkits}）");

                case ItemType.RepairKit:
                    p.RaidRepairKits++;
                    return One($"  + 维修套件 ×1（携带 {p.RaidRepairKits}）");

                case ItemType.Stim:
                    p.RaidStims++;
                    return One($"  + 肾上腺素 ×1（携带 {p.RaidStims}）");

                case ItemType.Weapon:
                    if (p.Weapon == null || it.Power > p.Weapon.Power)
                    {
                        var old = p.Weapon;
                        p.Weapon = it;
                        var log = One($"  + 换上武器 {it.Name}（伤害{it.Power:F1}）");
                        if (old != null) log.AddRange(AddToBackpack(p, old));
                        return log;
                    }
                    break;

                case ItemType.Armor:
                    if (p.ArmorItem == null || it.Power > p.ArmorItem.Power)
                    {
                        var old = p.ArmorItem;
                        p.ArmorItem = it;
                        p.Armor = it.Power;
                        var log = One($"  + 换上护甲 {it.Name}（护甲{it.Power:F1}）");
                        if (old != null) log.AddRange(AddToBackpack(p, old));
                        return log;
                    }
                    break;

                case ItemType.Rig:
                    if (p.Rig == null || it.Power > p.Rig.Power)
                    {
                        var old = p.Rig;
                        p.Rig = it;
                        var log = One($"  + 换上胸挂 {it.Name}（容量+{it.Power:F0}，当前 {p.UsedCap}/{p.MaxCapacity}）");
                        if (old != null) log.AddRange(AddToBackpack(p, old));
                        return log;
                    }
                    break;

                case ItemType.Backpack:
                    if (p.Bag == null || it.Power > p.Bag.Power)
                    {
                        var old = p.Bag;
                        p.Bag = it;
                        var log = One($"  + 换上背包 {it.Name}（容量+{it.Power:F0}，当前 {p.UsedCap}/{p.MaxCapacity}）");
                        if (old != null) log.AddRange(AddToBackpack(p, old));
                        return log;
                    }
                    break;
            }
            return AddToBackpack(p, it);
        }

        /// <summary>
        /// 放进背包。空间不足时按「性价比从低到高」试着腾位置：
        /// 只有新物品性价比更高、且总价值不低于被丢弃物的 60% 才替换，否则放弃拾取。
        /// </summary>
        public static List<string> AddToBackpack(Player p, Item newItem)
        {
            var log = new List<string>();

            if (p.UsedCap + newItem.Weight <= p.MaxCapacity)
            {
                p.Backpack.Add(newItem);
                log.Add($"  + {newItem.Name}（价值{newItem.Value} 重{newItem.Weight}）");
                return log;
            }

            int weightNeeded = p.UsedCap + newItem.Weight - p.MaxCapacity;
            var sortedBackpack = p.Backpack
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
                    foreach (var item in itemsToDrop) p.Backpack.Remove(item);
                    p.Backpack.Add(newItem);

                    string droppedNames = string.Join("、", itemsToDrop.Select(i => $"{i.Name}(价值{i.Value} 重{i.Weight})"));
                    log.Add($"  ⚠ 背包已满，自动替换丢弃：{droppedNames}");
                    log.Add($"  + 拾取 {newItem.Name}（价值{newItem.Value} 重{newItem.Weight}）");
                    return log;
                }

                string reason = newRatio <= maxDroppedRatio ? "新物品性价比不高于被替换物品" : "新物品价值低于丢弃物品总价值的60%";
                log.Add($"  × 背包已满，放弃拾取 {newItem.Name}（原因：{reason}）");
                return log;
            }

            log.Add($"  × 背包已满，丢弃 {newItem.Name}（无法腾出足够空间）");
            return log;
        }

        // ===== 局内使用消耗品 =====

        public static List<string> UseMedkit(Player p)
        {
            if (p.RaidMedkits <= 0) return One("没有携带急救包。");
            if (p.Hp >= p.MaxHp) return One("状态很好。");

            p.RaidMedkits--;
            p.Hp = Math.Min(p.MaxHp, p.Hp + 40);
            return One(Style.Paint("恢复40生命。", Style.Green));
        }

        public static List<string> UseRepairKit(Player p)
        {
            if (p.RaidRepairKits <= 0) return One("没有携带维修套件。");
            if (p.ArmorItem == null) return One("没有护甲可修。");

            p.RaidRepairKits--;
            p.Armor = Math.Min(p.MaxArmor, p.Armor + 50);
            return One(Style.Paint("护甲恢复50。", Style.Magenta));
        }

        public static List<string> UseStim(Player p)
        {
            if (p.RaidStims <= 0) return One("没有携带兴奋剂。");

            p.RaidStims--;
            foreach (var s in p.Actives) s.CurrentCooldown = 0;
            return One("肾上腺素：所有主动技能冷却已清除！");
        }

        static List<string> One(string line) => new List<string> { line };
    }
}
