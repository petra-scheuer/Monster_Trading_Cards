// Models/User.cs
using System;

namespace MonsterCardTradingGame.Models
{
    public class User
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // Gehashtes Passwort
        public Guid? Token { get; set; }
        public int Coins { get; set; }
        public int ELO { get; set; }
    }
}
