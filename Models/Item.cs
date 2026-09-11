namespace SearchFightExtract
{
    class Item
    {
        public string Name { get; set; }
        public ItemType Type { get; set; }
        public int Value { get; set; }
        public int Weight { get; set; }
        public double Power { get; set; } // 改成 double
        public int Tier { get; set; }

        public Item() { }
        public Item(string name, ItemType type, int value, int weight, double power = 0, int tier = 0)
        {
            Name = name; Type = type; Value = value; Weight = weight; Power = power; Tier = tier;
        }

        public string Desc()
        {
            switch (Type)
            {
                case ItemType.Weapon: return $"伤害{Power:F1} 子弹Tier{Tier}";
                case ItemType.Armor: return $"护甲{Power:F1} 护甲Tier{Tier}";
                case ItemType.Medkit: return $"治疗{Power:F1}";
                case ItemType.RepairKit: return $"修复{Power:F1}";
                case ItemType.Stim: return "清除所有技能冷却";
                case ItemType.Rig: return $"胸挂 容量+{Power:F0}";
                case ItemType.Backpack: return $"背包 容量+{Power:F0}";
                default: return "物资";
            }
        }

        public Item Clone() => new Item(Name, Type, Value, Weight, Power, Tier);
        public override string ToString() => $"{Name} ({Desc()}) 价值{Value} 重{Weight}";
    }
}
