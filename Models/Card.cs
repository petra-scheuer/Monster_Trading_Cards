// Card.cs

namespace MonsterCardTradingGame
{
    public abstract class Card
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // 'spell' or 'monster'
        public int Damage { get; set; }
        public string Element { get; set; } = string.Empty; // 'fire', 'water', 'normal'
    }

    public class SpellCard : Card
    {
        // Additional properties for SpellCard can be added here
    }

    public class MonsterCard : Card
    {
        // Additional properties for MonsterCard can be added here
    }
}