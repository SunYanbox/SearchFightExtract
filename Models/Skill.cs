using System;
using System.Collections.Generic;

namespace SearchFightExtract
{
    class Skill
    {
        public SkillType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Desc => Description;
        public int Cooldown { get; set; }
        public int CurrentCooldown { get; set; }
        public SkillDefinition Definition { get; }

        public Skill() { }

        /// <summary>
        /// 从数据定义创建技能：移除硬编码 effect 字符串，
        /// 效果计算委托由 SkillData.Effect 统一提供。
        /// </summary>
        public Skill(SkillType type)
        {
            var def = SkillData.Get(type);
            if (def == null) throw new InvalidOperationException($"未知技能类型：{type}");
            Type = type;
            Name = def.Name;
            Description = def.Description;
            Cooldown = def.Cooldown;
            CurrentCooldown = 0;
            Definition = def;
        }

        /// <summary>
        /// 从数据定义计算技能效果（数据驱动）。
        /// 效果计算委托由 SkillData.Effect 提供，
        /// 传入玩家、敌人、玩家状态、敌人状态，返回效果描述文本列表。
        /// </summary>
        public List<string> ComputeEffect(Player p, Enemy e)
        {
            return Definition.Effect(p, e);
        }

        /// <summary>
        /// 判断是否为被动技能（用于乘区计算等）。
        /// </summary>
        public static bool IsPassive(SkillType t) =>
            SkillTypeExtensions.IsPassive(t);
    }
}