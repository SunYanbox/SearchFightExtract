using System;

namespace SearchFightExtract
{
    /// <summary>
    /// 纯战斗数值计算：只做算术，不读写任何游戏状态、不产生 I/O。
    /// 所有随机性都由调用方把 <see cref="Random"/> 传进来，便于单独验证与复用。
    /// </summary>
    static class CombatMath
    {
        /// <summary>
        /// 攻击骰点：在 [attack/2, attack] 之间取整随机。
        /// 武器用 <c>Weapon.Power</c>、敌人用 <c>Enemy.Damage</c>，此处不关心来源。
        /// </summary>
        public static double RollAttack(Random rng, double attack)
            => rng.Next((int)(attack / 2), (int)(attack + 1));

        /// <summary>
        /// 子弹与护甲的 Tier 对抗结算。
        /// 按 Tier 差决定伤害系数与护甲吸收率，扣减 <paramref name="armor"/> 引用的护甲池，
        /// 并通过 <paramref name="armorLoss"/> 返回实际损耗。返回值至少为 1。
        /// </summary>
        public static double CalculateDamage(double baseDmg, int bulletTier, int armorTier, ref double armor, out double armorLoss, double armorLossMult = 1.0)
        {
            armorLoss = 0;
            if (armor <= 0) return baseDmg;

            double armorBefore = armor;

            double dmgMultiplier;
            double absorbRate;

            if (bulletTier > armorTier) { dmgMultiplier = 1.0; absorbRate = 0.3; }
            else if (bulletTier == armorTier) { dmgMultiplier = 0.75; absorbRate = 0.5; }
            else { dmgMultiplier = 0.5; absorbRate = 0.8; }

            double theoretical = baseDmg * dmgMultiplier;
            double absorbed = theoretical * absorbRate;

            armor = Math.Max(0, armor - absorbed * armorLossMult);
            armorLoss = armorBefore - armor;

            double final = theoretical - absorbed;
            return Math.Max(1, final);
        }

        /// <summary>
        /// 我方输出增伤乘区：战斗部门加成 + 敌人【受伤】20%（同类加算）。
        /// </summary>
        public static double DamageDealtMult(double combatDamageBonus, bool enemyWeakened)
            => 1 + combatDamageBonus + (enemyWeakened ? 0.2 : 0);

        /// <summary>
        /// 我方受击免伤乘区：深蓝 -30% / 超载 -10% / 铁壁破甲 -80% / 应急治疗 -80%，同类相乘。
        /// 按原顺序逐个施加，保证与拆分前逐位一致。
        /// </summary>
        public static double ApplyDamageTakenReduction(double dmg, bool deepBlue, bool overload, bool ironWallDR, bool firstAidDR)
        {
            if (deepBlue) dmg *= 0.7;
            if (overload) dmg *= 0.9;
            if (ironWallDR) dmg *= 0.2;
            if (firstAidDR) dmg *= 0.2;
            return dmg;
        }
    }
}
