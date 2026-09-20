namespace SearchFightExtract
{
    /// <summary>
    /// 单场战斗的会话状态：跨回合累计的临时数据，战斗结束即丢弃。
    /// 与 <see cref="Player"/> 身上的持久状态分开，避免战斗细节污染存档模型。
    /// </summary>
    class CombatState
    {
        /// <summary>已开始的回合数（从 1 起）。</summary>
        public int Turn { get; set; }

        /// <summary>【再现】下回合待插入的额外行动数。</summary>
        public int PendingExtraTurns { get; set; }

        /// <summary>【再现】累计受伤 = 血量伤害 + 0.3 × 护甲伤害。</summary>
        public double DamageTakenAccum { get; set; }
    }
}
