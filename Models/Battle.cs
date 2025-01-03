// Models/Battle.cs
using System;
using System.Collections.Generic;

namespace MonsterCardTradingGame.Models
{
    public class Battle
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string PlayerUsername { get; set; } = string.Empty;
        public string OpponentUsername { get; set; } = "AI"; // Oder ein anderer Spieler
        public List<int> PlayerCardIds { get; set; } = new List<int>();
        public List<int> OpponentCardIds { get; set; } = new List<int>();
        public int PlayerHealth { get; set; } = 100;
        public int OpponentHealth { get; set; } = 100;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "In Progress"; // "In Progress", "Completed"
        public List<BattleLog> Logs { get; set; } = new List<BattleLog>();
    }
}