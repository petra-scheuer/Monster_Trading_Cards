// Models/Trade.cs
using System;

namespace MonsterCardTradingGame.Models
{
    public class Trade
    {
        public int Id { get; set; }
        public string OfferedByUsername { get; set; } = string.Empty;
        public int OfferedCardId { get; set; }
        public string RequirementType { get; set; } = string.Empty; // 'spell' oder 'monster'
        public string? RequirementElement { get; set; } // Optional: z.B. 'fire'
        public int? RequirementMinDamage { get; set; } // Optional: Mindestschaden
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}