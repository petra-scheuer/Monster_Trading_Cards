// Models/BattleLog.cs
using System;

namespace MonsterCardTradingGame.Models
{
    public class BattleLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid BattleId { get; set; }
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}