namespace SearchFightExtract
{
    class Skill
    {
        public SkillType Type { get; set; }
        public string Name { get; set; }
        public string Desc { get; set; }
        public int Cooldown { get; set; }
        public int CurrentCooldown { get; set; }

        public Skill() { }
        public Skill(SkillType type)
        {
            Type = type;
            switch (type)
            {
                // ===== 被动 =====
                case SkillType.Overload:
                    Name = "超载"; Desc = "受伤-10%，每2回合额外攻击一次且伤害+25%"; Cooldown = 0; break;
                case SkillType.BeeMedic:
                    Name = "蜂医"; Desc = "移动恢复30生命，并获得相当于生命上限45%的护盾（可叠加，至多3层）"; Cooldown = 0; break;
                case SkillType.DeepBlue:
                    Name = "深蓝"; Desc = "受到的所有伤害-30%"; Cooldown = 0; break;
                case SkillType.IronWall:
                    Name = "铁壁"; Desc = "护甲耐久消耗-40%，护甲上限+20%；护甲破碎时获得80%免伤，持续2回合"; Cooldown = 0; break;
                case SkillType.Scavenger:
                    Name = "拾荒者"; Desc = "搜索时额外获得1件物资"; Cooldown = 0; break;
                case SkillType.Nimble:
                    Name = "疾步"; Desc = "移动遭遇敌人的概率减半"; Cooldown = 0; break;
                case SkillType.StormCloud:
                    Name = "乌云乌云快走开"; Desc = "生命上限+200%，但护甲消耗速率翻倍"; Cooldown = 0; break;
                case SkillType.Reappear:
                    Name = "再现"; Desc = "累计（生命伤害 + 30%×护甲伤害）每满50，下回合获得一个插入式额外行动（不消耗回合）"; Cooldown = 0; break;

                // ===== 主动 =====
                case SkillType.Shield:
                    Name = "应急护盾"; Desc = "获得120点护盾 + 80点临时护甲（按六级甲结算，每回合衰减20）"; Cooldown = 6; break;
                case SkillType.Dragon:
                    Name = "威龙"; Desc = "60真实伤害 + 60伤害（按7级弹穿透）；敌人生命<70%时伤害+75%"; Cooldown = 4; break;
                case SkillType.Net:
                    Name = "深蓝刺网"; Desc = "60伤害（按7级弹穿透）并束缚敌人；敌人【受伤】3回合，受到伤害+20%（不叠加）"; Cooldown = 5; break;
                case SkillType.RapidFire:
                    Name = "快速射击"; Desc = "立即打出三段攻击（100% + 70% + 70%）"; Cooldown = 3; break;
                case SkillType.FirstAid:
                    Name = "应急治疗"; Desc = "恢复60生命，获得80%免伤持续3回合，并获得等同于最大生命值80%的护盾"; Cooldown = 6; break;
                case SkillType.Adrenaline:
                    Name = "肾上腺素"; Desc = "清除所有主动技能冷却（不消耗回合）"; Cooldown = 10; break;
                case SkillType.Incendiary:
                    Name = "燃烧弹"; Desc = "75伤害（按7级弹穿透）+ 每回合15燃烧（可被护甲抵御）+ 敌人灭火跳过一回合"; Cooldown = 4; break;
            }
            CurrentCooldown = 0;
        }

        public static bool IsPassive(SkillType t)
        {
            switch (t)
            {
                case SkillType.Overload:
                case SkillType.BeeMedic:
                case SkillType.DeepBlue:
                case SkillType.IronWall:
                case SkillType.Scavenger:
                case SkillType.Nimble:
                case SkillType.StormCloud:
                case SkillType.Reappear:
                    return true;
                default:
                    return false;
            }
        }
    }
}
