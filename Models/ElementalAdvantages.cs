// Models/ElementalAdvantages.cs

using System.Collections.Generic;

namespace MonsterCardTradingGame.Models
{
    public static class ElementalAdvantages
    {
        // Schlüssel: Element, das einen Vorteil hat
        // Wert: Element, gegenüber dem es einen Vorteil hat
        public static readonly Dictionary<string, string> Advantages = new Dictionary<string, string>
        {
            { "fire", "water" },   // Feuer hat Vorteil gegenüber Wasser
            { "water", "earth" },  // Wasser hat Vorteil gegenüber Erde
            { "earth", "fire" },   // Erde hat Vorteil gegenüber Feuer
            // Weitere Elemente können hier hinzugefügt werden
        };
    }
}