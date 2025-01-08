using System;

namespace MonsterCardTradingGame.Models
{
    public class PowerUp
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PowerUpType { get; set; } = "double_damage";
        public bool IsUsed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}