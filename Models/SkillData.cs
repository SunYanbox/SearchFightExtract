using System;
using System.Collections.Generic;
using System.Linq;
using SearchFightExtract;

namespace SearchFightExtract
{
    /// <summary>
    /// 数据驱动的技能定义表：将技能效果从硬编码的 Skill.cs 迁移为数据驱动。
    /// 每项技能定义包含：类型、名称、描述、冷却、效果计算函数。
    /// 效果计算函数接收玩家与敌方状态，返回效果描述文本列表。
    /// </summary>
    /// <remarks>
    /// 技能结算切换为数据驱动后，技能定义集中管理，
    /// 效果计算由数据驱动的函数完成，而非硬编码在 Skill 构造器中。
    /// </remarks>
    static class SkillData
    {
        /// <summary>按 SkillType 查找技能定义，若不存在则返回 null。</summary>
        public static SkillDefinition Get(SkillType type) =>
            _definitions.FirstOrDefault(d => d.Type == type);

        /// <summary>获取所有技能定义的只读集合。</summary>
        public static IReadOnlyList<SkillDefinition> Definitions => _definitions;

        /// <summary>按类型分组（缓存）。</summary>
        public static Dictionary<SkillType, List<SkillDefinition>> ByType { get; } =
            _definitions.GroupBy(d => d.Type).ToDictionary(g => g.Key, g => g.ToList());

        /// <summary>被动技能列表。</summary>
        public static readonly List<SkillDefinition> Passives = _definitions.Where(d => d.Type.IsPassive()).ToList();

        /// <summary>主动技能列表。</summary>
        public static readonly List<SkillDefinition> Actives = _definitions.Where(d => !d.Type.IsPassive()).ToList();

        /// <summary>所有定义的静态列表。</summary>
        private static readonly List<SkillDefinition> _definitions =
        [
            // ===== 被动技能 =====
            new SkillDefinition
            {
                Type = SkillType.Overload,
                Name = "超载",
                Description = "受伤 -10%，每 2 回合额外攻击一次且伤害 +25%",
                Cooldown = 0,
                Effect = (p, e) =>
                {
                    var log = new List<string> { "【超载】受伤 -10%，每 2 回合额外攻击一次且伤害 +25%" };
                    return log;
                }
            },
            new SkillDefinition
            {
                Type = SkillType.BeeMedic,
                Name = "蜂医",
                Description = "移动恢复 30 生命，并获得相当于生命上限 45% 的护盾（可叠加，至多 3 层）",
                Cooldown = 0,
                Effect = (p, e) =>
                {
                    var log = new List<string>();
                    double layer = p.MaxHp * 0.45;
                    double cap = layer * 3;
                    log.Add(string.Format("蜂医：恢复 30 生命，获得护盾（{0:F1}/{1:F1}）。", p.Hp, cap));
                    return log;
                }
            },
            new SkillDefinition
            {
                Type = SkillType.DeepBlue,
                Name = "深蓝",
                Description = "受到的所有伤害 -30%",
                Cooldown = 0,
                Effect = (p, e) =>
                {
                    var log = new List<string> { "【深蓝】受到的所有伤害 -30%" };
                    return log;
                }
            },
            new SkillDefinition
            {
                Type = SkillType.IronWall,
                Name = "铁壁",
                Description = "护甲耐久消耗 -40%，护甲上限 +20%；护甲破碎时获得 80% 免伤，持续 2 回合",
                Cooldown = 0,
                Effect = (p, e) =>
                {
                    var log = new List<string> { "【铁壁】护甲耐久消耗 -40%，护甲上限 +20%；护甲破碎时获得 80% 免伤，持续 2 回合" };
                    return log;
                }
            },
            new SkillDefinition
            {
                Type = SkillType.Scavenger,
                Name = "拾荒者",
                Description = "搜索时额外获得 1 件物资",
                Cooldown = 0,
                Effect = (p, e) =>
                {
                    var log = new List<string> { "【拾荒者】搜索时额外获得 1 件物资" };
                    return log;
                }
            },
            new SkillDefinition
            {
                Type = SkillType.Nimble,
                Name = "疾步",
                Description = "移动遭遇敌人的概率减半",
                Cooldown = 0,
                Effect = (p, e) =>
                {
                    var log = new List<string> { "【疾步】移动遭遇敌人的概率减半" };
                    return log;
                }
            },
            new SkillDefinition
            {
                Type = SkillType.StormCloud,
                Name = "乌云乌云快走开",
                Description = "生命上限 +200%，但护甲消耗速率翻倍",
                Cooldown = 0,
                Effect = (p, e) =>
                {
                    var log = new List<string> { "【乌云乌云快走开】生命上限 +200%，但护甲消耗速率翻倍" };
                    return log;
                }
            },
            new SkillDefinition
            {
                Type = SkillType.Reappear,
                Name = "再现",
                Description = "累计（生命伤害 + 30%×护甲伤害）每满 50，下回合获得一个插入式额外行动（不消耗回合）",
                Cooldown = 0,
                Effect = (p, e) =>
                {
                    var log = new List<string> { "【再现】累计受伤达到 50，下回合获得额外行动" };
                    return log;
                }
            },
            // ===== 主动技能 =====
            new SkillDefinition
            {
                Type = SkillType.Shield,
                Name = "应急护盾",
                Description = "获得 120 点护盾 + 80 点临时护甲（按六级甲结算，每回合衰减 20）",
                Cooldown = 6,
                Effect = (p, e) =>
                {
                    var log = new List<string> { "应急护盾：获得 120 点护盾 + 80 点临时护甲（六级，每回合衰减 20）。" };
                    return log;
                }
            },
            new SkillDefinition
            {
                Type = SkillType.Dragon,
                Name = "威龙",
                Description = "60 真实伤害 + 60 伤害（按 7 级弹穿透）；敌人生命 <70% 时伤害 +75%",
                Cooldown = 4,
                Effect = (p, e) =>
                {
                    var log = new List<string>();
                    double bonus = p.CombatDamageBonus + (e.WeakenTurns > 0 ? 0.2 : 0);
                    if (e.Hp < e.MaxHp * 0.7) bonus += 0.75;
                    double mult = 1 + bonus;
                    log.Add("威龙：真实伤害 +60，伤害 +60（按 7 级弹穿透）；敌人生命 <70% 时伤害 +75%");
                    log.Add(string.Format("伤害倍率：{0:F1} 倍", mult));
                    return log;
                }
            },
            new SkillDefinition
            {
                Type = SkillType.Net,
                Name = "深蓝刺网",
                Description = "60 伤害（按 7 级弹穿透）并束缚敌人；敌人【受伤】3 回合，受到伤害 +20%（不叠加）",
                Cooldown = 5,
                Effect = (p, e) =>
                {
                    var log = new List<string> { "深蓝刺网：造成 60 伤害（按 7 级弹穿透）并束缚敌人；敌人【受伤】3 回合，受到伤害 +20%（不叠加）" };
                    return log;
                }
            },
            new SkillDefinition
            {
                Type = SkillType.RapidFire,
                Name = "快速射击",
                Description = "立即打出三段攻击（100% + 70% + 70%）",
                Cooldown = 3,
                Effect = (p, e) =>
                {
                    var log = new List<string> { "快速射击：立即打出三段攻击（100% + 70% + 70%）。" };
                    return log;
                }
            },
            new SkillDefinition
            {
                Type = SkillType.FirstAid,
                Name = "应急治疗",
                Description = "恢复 60 生命，获得 80% 免伤持续 3 回合，并获得等同于最大生命值 80% 的护盾",
                Cooldown = 6,
                Effect = (p, e) =>
                {
                    var log = new List<string> { "应急治疗：恢复 60 生命，获得 80% 免伤（3 回合），并获得等同于最大生命值 80% 的护盾。" };
                    return log;
                }
            },
            new SkillDefinition
            {
                Type = SkillType.Adrenaline,
                Name = "肾上腺素",
                Description = "清除所有主动技能冷却（不消耗回合）",
                Cooldown = 10,
                Effect = (p, e) =>
                {
                    var log = new List<string> { "肾上腺素：清除所有主动技能冷却（不消耗行动）" };
                    return log;
                }
            },
            new SkillDefinition
            {
                Type = SkillType.Incendiary,
                Name = "燃烧弹",
                Description = "75 伤害（按 7 级弹穿透）+ 每回合 15 燃烧（可被护甲抵御）+ 敌人灭火跳过一回合",
                Cooldown = 4,
                Effect = (p, e) =>
                {
                    var log = new List<string> { "燃烧弹：造成 75 伤害（按 7 级弹穿透）+ 每回合 15 燃烧，敌人灭火跳过一回合" };
                    return log;
                }
            },
        ];
    }

    /// <summary>
    /// 技能定义数据类。
    /// 效果计算委托接收玩家与敌方状态，返回效果描述文本列表。
    /// </summary>
    sealed class SkillDefinition
    {
        public SkillType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Cooldown { get; set; }
        public Func<Player, Enemy, List<string>> Effect { get; set; }
    }
}
