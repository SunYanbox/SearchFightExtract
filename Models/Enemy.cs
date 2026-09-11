using System.Collections.Generic;

namespace SearchFightExtract
{
    class Enemy
    {
        public string Name { get; set; }
        public double Hp { get; set; }
        public double MaxHp { get; set; }
        public double Damage { get; set; }
        public double Armor { get; set; }
        public double MaxArmor { get; set; }
        public int ArmorTier { get; set; }
        public int BulletTier { get; set; }
        public List<Item> Loot { get; set; } = new List<Item>();
        public bool SkipNextTurn { get; set; }
        public int BurnTurns { get; set; } = 0;
        public double BurnDamage { get; set; } = 0;
        public int WeakenTurns { get; set; } = 0;   // 深蓝刺网：受伤状态，受到伤害+20%

        public Enemy() { }
        public Enemy(string name, double hp, double damage, double armor, int armorTier, int bulletTier, List<Item> loot)
        {
            Name = name; Hp = hp; MaxHp = hp; Damage = damage; Armor = armor; MaxArmor = armor;
            ArmorTier = armorTier; BulletTier = bulletTier; Loot = loot;
        }
    }
}
