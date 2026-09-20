using System;

namespace SearchFightExtract
{
    /// <summary>
    /// 敌人生成：按难度抽取护甲/子弹等级，据此派生生命、伤害、护甲与掉落。
    /// </summary>
    static class EnemyFactory
    {
        public static Enemy Create(Random rng, Difficulty d)
        {
            var dp = GameData.Params(d);
            int armorTier = rng.Next(dp.MinArmor, dp.MaxArmor + 1);
            int bulletTier = rng.Next(dp.MinBullet, dp.MaxBullet + 1);

            double hp = (60 * armorTier + 30 * bulletTier + 40) * dp.HpMult;
            double dmg = (8 * bulletTier + 8) * dp.DmgMult;
            double armor = (22 * armorTier) * dp.ArmorMult;

            int lootCount = 1 + rng.Next(0, 3);
            lootCount = Math.Max(1, (int)Math.Round(lootCount * dp.LootMult));

            return new Enemy(Name(armorTier, bulletTier), hp, dmg, armor, armorTier, bulletTier,
                             GameData.RollItems(rng, d, lootCount));
        }

        static string Name(int armorTier, int bulletTier)
        {
            int lvl = Math.Max(armorTier, bulletTier);
            switch (lvl)
            {
                case 1: return "拾荒者";
                case 2: return "雇佣兵";
                case 3: return "精锐雇佣兵";
                case 4: return "特遣队员";
                case 5: return "特勤干员";
                default: return "黑鹰指挥官";
            }
        }
    }
}
