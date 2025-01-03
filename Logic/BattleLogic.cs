// Logic/BattleLogic.cs
using System;
using System.Collections.Generic;
using MonsterCardTradingGame.Models;
using MonsterCardTradingGame.Repositories;

namespace MonsterCardTradingGame.Logic
{
    public class BattleLogic
    {
        public Battle StartBattle(string playerUsername, List<int> playerCardIds)
        {
            // Optionale Logik zur Validierung der Karten des Spielers
            var battle = BattleRepository.CreateBattle(playerUsername, playerCardIds);
            
            // Optionale Logik zur Auswahl der Karten des Gegners
            // Zum Beispiel zufällige Auswahl aus den Karten des Gegners
            
            // Initialen Battle-Log hinzufügen
            battle.Logs.Add(new BattleLog { Action = "Battle gestartet zwischen " + playerUsername + " und AI." });
            BattleRepository.UpdateBattle(battle);
            
            return battle;
        }

        public Battle PerformBattleTurn(Guid battleId)
        {
            var battle = BattleRepository.GetBattle(battleId);
            if (battle == null || battle.Status != "In Progress")
                throw new Exception("Battle nicht gefunden oder bereits abgeschlossen.");

            // Beispielhafte einfache Logik:
            // Spieler greift zuerst an
            var playerDamage = CalculateDamage(battle.PlayerCardIds);
            battle.OpponentHealth -= playerDamage;
            battle.Logs.Add(new BattleLog { Action = $"Spieler greift für {playerDamage} Schaden an." });

            if (battle.OpponentHealth <= 0)
            {
                battle.Status = "Completed";
                battle.Logs.Add(new BattleLog { Action = "Spieler gewinnt den Kampf!" });
                BattleRepository.UpdateBattle(battle);
                return battle;
            }

            // Gegner (AI) greift an
            var opponentDamage = CalculateDamage(battle.OpponentCardIds);
            battle.PlayerHealth -= opponentDamage;
            battle.Logs.Add(new BattleLog { Action = $"AI greift für {opponentDamage} Schaden an." });

            if (battle.PlayerHealth <= 0)
            {
                battle.Status = "Completed";
                battle.Logs.Add(new BattleLog { Action = "AI gewinnt den Kampf!" });
            }

            BattleRepository.UpdateBattle(battle);
            return battle;
        }

        private int CalculateDamage(List<int> cardIds)
        {
            // Beispielhafte Schadensberechnung:
            // Summe der Schadenswerte aller Karten
            int damage = 0;
            foreach (var id in cardIds)
            {
                var card = CardRepository.GetCardById(id);
                if (card != null)
                {
                    damage += card.Damage;
                }
            }
            return damage;
        }
    }
}
