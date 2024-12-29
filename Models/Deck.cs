namespace MonsterCardTradingGame.Models
{
    public class Deck
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public List<int> CardIds { get; set; } = new List<int>(); // Max 4 Karten
    }
}