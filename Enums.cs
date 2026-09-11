namespace SearchFightExtract
{
    enum ItemType { Weapon, Armor, Medkit, Loot, RepairKit, Stim, Rig, Backpack }

    enum SkillType
    {
        // ===== 被动 =====
        Overload,   // 超载：受伤-25%，每2回合额外攻击
        BeeMedic,   // 蜂医：移动回20血
        DeepBlue,   // 深蓝：受伤-50%
        IronWall,   // 铁壁：护甲损耗-40%
        Scavenger,  // 拾荒者：搜索额外+1件
        Nimble,     // 疾步：移动遭遇概率减半
        StormCloud, // 乌云乌云快走开：生命上限+200%，护甲消耗翻倍
        Reappear,   // 再现：累计受伤50（血+甲）获得额外回合

        // ===== 主动 =====
        Shield,     // 应急护盾
        Dragon,     // 磁吸炸弹
        Net,        // 防爆刺网
        RapidFire,  // 快速射击
        FirstAid,   // 应急治疗
        Adrenaline, // 肾上腺素：清冷却
        Incendiary  // 燃烧弹
    }

    enum CombatResult { Win, Fled, Dead }

    enum Difficulty { D, C, B, A, S, SS }
}
