using System;
using System.Collections.Generic;
using System.Linq;

namespace SearchFightExtract
{
    class Player
    {
        public double Hp { get; set; } = 100;
        public double MaxHp { get; set; } = 100;
        public double Shield { get; set; } = 0;
        public double Armor { get; set; }

        // 临时护甲（应急护盾技能）：独立护甲池，按指定 Tier 结算，每回合衰减
        public double TempArmor { get; set; } = 0;
        public int TempArmorTier { get; set; } = 6;

        // 铁壁：护甲破碎后的免伤剩余回合
        public int ArmorBreakDRTurns { get; set; } = 0;

        public int Money { get; set; } = 5000;
        public int Medkits { get; set; } = 2;
        public int RepairKits { get; set; } = 1;
        public int Stims { get; set; } = 0;
        public int Materials { get; set; } = 0;

        // 本局携带进场的消耗品（撤离归还剩余，阵亡则丢失）
        public int RaidMedkits { get; set; } = 0;
        public int RaidRepairKits { get; set; } = 0;
        public int RaidStims { get; set; } = 0;

        // ===== RPG 部门等级 =====
        public int TalentLevel { get; set; } = 1;      // 天赋：被动上限
        public int ActiveSkillLevel { get; set; } = 2; // 主动技能：主动上限
        public int CombatLevel { get; set; } = 0;      // 战斗
        public int SurvivalLevel { get; set; } = 0;    // 生存

        public int Capacity { get; set; } = 20;   // 基础容量
        public Item Weapon { get; set; }
        public Item ArmorItem { get; set; }
        public Item Rig { get; set; }              // 胸挂
        public Item Bag { get; set; }              // 背包
        public List<Item> Backpack { get; set; } = new List<Item>();
        public List<Item> Stash { get; set; } = new List<Item>();
        public List<Skill> Passives { get; set; } = new List<Skill>();
        public List<Skill> Actives { get; set; } = new List<Skill>();
        public Difficulty Difficulty { get; set; } = Difficulty.C;
        public double RaidCapacityMult { get; set; } = 1.0;   // 本局容量倍率（SS 难度 +20%）
        public int TurnCounter { get; set; } = 0;

        // ===== 部门派生属性 =====
        public int PassiveCap => Math.Min(4, TalentLevel + 1);
        public int ActiveCap => Math.Min(4, ActiveSkillLevel + 1);

        public double SurvivalHpBonus => SurvivalLevel >= 5 ? 0.25 : SurvivalLevel >= 4 ? 0.20 : SurvivalLevel >= 3 ? 0.15 : SurvivalLevel >= 2 ? 0.10 : SurvivalLevel >= 1 ? 0.05 : 0;
        public double SurvivalArmorRed => SurvivalLevel >= 5 ? 0.15 : SurvivalLevel >= 4 ? 0.10 : SurvivalLevel >= 3 ? 0.05 : 0;

        public double CombatDamageBonus => CombatLevel >= 5 ? 0.25 : CombatLevel >= 4 ? 0.20 : CombatLevel >= 3 ? 0.15 : CombatLevel >= 2 ? 0.10 : CombatLevel >= 1 ? 0.05 : 0;
        public double CombatLifeSteal => CombatLevel >= 4 ? 0.10 : CombatLevel >= 3 ? 0.05 : 0;
        public double CombatArmorRestore => CombatLevel >= 5 ? 0.10 : 0;

        // ===== 乘区（同类加算） =====
        public double HpPercentBonus => SurvivalHpBonus + (HasPassive(SkillType.StormCloud) ? 2.0 : 0);

        // 护甲上限倍率：铁壁 +20%
        public double ArmorMaxMult => 1.0 + (HasPassive(SkillType.IronWall) ? 0.20 : 0);

        // 护甲损耗倍率（基础 1）：降低为负、乌云翻倍为正，同类加算
        public double ArmorLossMult
        {
            get
            {
                double red = SurvivalArmorRed + (HasPassive(SkillType.IronWall) ? 0.40 : 0);
                double mult = 1.0 - red + (HasPassive(SkillType.StormCloud) ? 1.0 : 0);
                return Math.Max(0.1, mult);
            }
        }

        public double MaxArmor => (ArmorItem?.Power ?? 0) * ArmorMaxMult;
        public int MaxCapacity => (int)((Capacity + (Rig?.Power ?? 0) + (Bag?.Power ?? 0)) * RaidCapacityMult);
        public int UsedCap => Backpack.Sum(i => i.Weight);

        public bool HasPassive(SkillType t) => Passives.Any(s => s.Type == t);

        // 重新计算派生数值（生命上限等）
        public void RecomputeStats()
        {
            double newMax = 100 * (1 + HpPercentBonus);
            if (Math.Abs(newMax - MaxHp) > 0.001)
            {
                if (Hp >= MaxHp) Hp = newMax;   // 满血则维持满血
                MaxHp = newMax;
            }
            if (Hp > MaxHp) Hp = MaxHp;
        }
    }
}
