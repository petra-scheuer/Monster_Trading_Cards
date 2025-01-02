// Models/Battle.cs
using System;
using System.Collections.Generic;

namespace MonsterCardTradingGame.Models
{
    public class Battle
    {
        public int Id { get; set; }
        public string Player1Username { get; set; } = string.Empty;
        public string Player2Username { get; set; } = string.Empty;
        public string BattleLog { get; set; } = string.Empty;
        public string? WinnerUsername { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}