// Logic/BattleLogic.cs
using System;
using System.Collections.Generic;
using MonsterCardTradingGame.Models;

namespace MonsterCardTradingGame.Logic
{
    public class BattleResult
    {
        public string? Winner { get; set; }
        public string Log { get; set; } = string.Empty;
    }

    public static class BattleLogic
    {
        private static readonly Random Random = new Random();

        public static BattleResult PerformBattle(List<Card> player1Cards, List<Card> player2Cards)
        {
            var result = new BattleResult();
            var logBuilder = new System.Text.StringBuilder();

            int round = 0;
            int maxRounds = 100;
            string? winner = null;

            // Kopien der Decks, um Karten zu entfernen
            var p1Deck = new List<Card>(player1Cards);
            var p2Deck = new List<Card>(player2Cards);

            while (round < maxRounds && p1Deck.Count > 0 && p2Deck.Count > 0)
            {
                round++;
                logBuilder.AppendLine($"--- Runde {round} ---");

                // Zufällige Karten auswählen
                var p1Card = p1Deck[Random.Next(p1Deck.Count)];
                var p2Card = p2Deck[Random.Next(p2Deck.Count)];

                logBuilder.AppendLine($"{p1Card.Name} ({p1Card.Type}, {p1Card.Element}) vs {p2Card.Name} ({p2Card.Type}, {p2Card.Element})");

                // Berechnung des Schadens
                int p1Damage = CalculateDamage(p1Card, p2Card);
                int p2Damage = CalculateDamage(p2Card, p1Card);

                logBuilder.AppendLine($"Schaden: {p1Card.Name} verursacht {p1Damage} Schaden, {p2Card.Name} verursacht {p2Damage} Schaden");

                if (p1Damage > p2Damage)
                {
                    logBuilder.AppendLine($"{p1Card.Name} gewinnt die Runde.");
                    // p2Card wird dem p1Deck hinzugefügt
                    p1Deck.Add(p2Card);
                    p2Deck.Remove(p2Card);
                }
                else if (p2Damage > p1Damage)
                {
                    logBuilder.AppendLine($"{p2Card.Name} gewinnt die Runde.");
                    // p1Card wird dem p2Deck hinzugefügt
                    p2Deck.Add(p1Card);
                    p1Deck.Remove(p1Card);
                }
                else
                {
                    logBuilder.AppendLine("Unentschieden. Keine Karten werden verschoben.");
                }
            }

            // Bestimmen des Gewinners
            if (p1Deck.Count > p2Deck.Count)
            {
                winner = "player1";
                logBuilder.AppendLine($"Spieler 1 gewinnt das Spiel nach {round} Runden.");
            }
            else if (p2Deck.Count > p1Deck.Count)
            {
                winner = "player2";
                logBuilder.AppendLine($"Spieler 2 gewinnt das Spiel nach {round} Runden.");
            }
            else
            {
                logBuilder.AppendLine($"Das Spiel endet unentschieden nach {round} Runden.");
            }

            result.Winner = winner;
            result.Log = logBuilder.ToString();
            return result;
        }

        private static int CalculateDamage(Card attacker, Card defender)
        {
            int damage = attacker.Damage;

            if (attacker.Type.ToLower() == "spell")
            {
                // Element-Effectiveness
                damage = ApplyElementEffectiveness(attacker.Element.ToLower(), defender.Element.ToLower(), damage);
            }

            return damage;
        }

        private static int ApplyElementEffectiveness(string attackerElement, string defenderElement, int damage)
        {
            // Effektivität: Wasser > Feuer, Feuer > Normal, Normal > Wasser
            if (attackerElement == "water" && defenderElement == "fire")
            {
                return damage * 2;
            }
            if (attackerElement == "fire" && defenderElement == "normal")
            {
                return damage * 2;
            }
            if (attackerElement == "normal" && defenderElement == "water")
            {
                return damage * 2;
            }

            if (attackerElement == "fire" && defenderElement == "water")
            {
                return damage / 2;
            }
            if (attackerElement == "normal" && defenderElement == "fire")
            {
                return damage / 2;
            }
            if (attackerElement == "water" && defenderElement == "normal")
            {
                return damage / 2;
            }

            // Keine Effektivität
            return damage;
        }
    }
}
