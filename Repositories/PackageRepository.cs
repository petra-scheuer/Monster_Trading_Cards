//PackageRepository.cs
using System;
using System.Collections.Generic;
using MonsterCardTradingGame.Models;

namespace MonsterCardTradingGame.Repositories
{
    public static class PackageRepository
    {
        // Methode zum Erstellen eines neuen Pakets mit neuen Karten
        public static bool CreatePackage(string username)
        {
            // Erstelle 5 neue zufällige Karten
            var newCards = GenerateRandomCards(5);
            
            // Füge die neuen Karten dem Benutzer hinzu
            foreach (var card in newCards)
            {
                CardRepository.AddCard(username, card);
            }
            
            return true;
        }

        private static List<Card> GenerateRandomCards(int count)
        {
            var cards = new List<Card>();
            var random = new Random();
            var elements = new[] { "fire", "water", "normal" };
            var types = new[] { "spell", "monster" };

            for (int i = 0; i < count; i++)
            {
                string type = types[random.Next(types.Length)];
                string element = elements[random.Next(elements.Length)];
                string name = type == "spell" ? $"Spell_{Guid.NewGuid().ToString().Substring(0, 8)}" : $"Monster_{Guid.NewGuid().ToString().Substring(0, 8)}";
                int damage = random.Next(10, 100); // Schaden zwischen 10 und 100

                Card card = type switch
                {
                    "spell" => new SpellCard { Name = name, Type = "spell", Damage = damage, Element = element },
                    "monster" => new MonsterCard { Name = name, Type = "monster", Damage = damage, Element = element },
                    _ => throw new Exception("Unbekannter Kartentyp.")
                };

                cards.Add(card);
            }

            return cards;
        }
    }
    
}