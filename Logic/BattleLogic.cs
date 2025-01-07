using System;
using System.Collections.Generic;
using System.Linq;
using MonsterCardTradingGame.Models;
using MonsterCardTradingGame.Repositories;

namespace MonsterCardTradingGame.Logic
{
    public class BattleLogic
    {
        /// <summary>
        /// Startet ein neues Battle (aktuell nur minimal).
        /// </summary>
        public Battle StartBattle(string playerUsername, List<int> playerCardIds)
        {
            // Optional: Kartenvalidierung

            // Neues Battle im Repository anlegen
            var battle = BattleRepository.CreateBattle(playerUsername, playerCardIds);

            // Initialen Log hinzufügen
            battle.Logs.Add(new BattleLog
            {
                Action = $"Battle gestartet zwischen {playerUsername} und {battle.OpponentUsername}.",
                Timestamp = DateTime.UtcNow
            });

            // Speichern
            BattleRepository.UpdateBattle(battle);

            return battle;
        }

        /// <summary>
        /// Beispielhafter "1 Turn" – war bei dir schon vorhanden.
        /// </summary>
        public Battle PerformBattleTurn(Guid battleId)
        {
            var battle = BattleRepository.GetBattle(battleId);
            if (battle == null || battle.Status != "In Progress")
                throw new Exception("Battle nicht gefunden oder bereits abgeschlossen.");

            // Beispiel: simpler Schlagabtausch (Summe aller Karten)
            var playerDamage = CalculateDamage(battle.PlayerCardIds);
            var opponentDamage = CalculateDamage(battle.OpponentCardIds);

            battle.OpponentHealth -= playerDamage;
            battle.Logs.Add(new BattleLog
            {
                Action = $"Spieler greift für {playerDamage} Schaden an.",
                Timestamp = DateTime.UtcNow
            });

            if (battle.OpponentHealth <= 0)
            {
                battle.Status = "Completed";
                battle.Logs.Add(new BattleLog
                {
                    Action = "Spieler gewinnt den Kampf!",
                    Timestamp = DateTime.UtcNow
                });
                BattleRepository.UpdateBattle(battle);
                return battle;
            }

            battle.PlayerHealth -= opponentDamage;
            battle.Logs.Add(new BattleLog
            {
                Action = $"AI greift für {opponentDamage} Schaden an.",
                Timestamp = DateTime.UtcNow
            });

            if (battle.PlayerHealth <= 0)
            {
                battle.Status = "Completed";
                battle.Logs.Add(new BattleLog
                {
                    Action = "AI gewinnt den Kampf!",
                    Timestamp = DateTime.UtcNow
                });
            }

            BattleRepository.UpdateBattle(battle);
            return battle;
        }

        /// <summary>
        /// NEU: Führt das komplette Battle in bis zu 100 Runden durch
        /// und wendet (noch ohne Spezialregeln) ein 1-gegen-1-Kartenprinzip an.
        /// </summary>
        public Battle PerformFullBattle(Guid battleId)
        {
            // Battle laden
            var battle = BattleRepository.GetBattle(battleId);
            if (battle == null)
                throw new Exception("Battle nicht gefunden.");

            if (battle.Status != "In Progress")
                throw new Exception("Battle ist nicht mehr aktiv oder bereits abgeschlossen.");

            int maxRounds = 100;
            int roundCount = 0;
            var random = new Random();

            while (roundCount < maxRounds)
            {
                // Check: Hat jemand keine Karten mehr?
                if (battle.PlayerCardIds.Count == 0)
                {
                    // Gegner gewinnt
                    battle.Status = "Completed";
                    battle.Logs.Add(new BattleLog
                    {
                        Action = $"Der Spieler {battle.PlayerUsername} hat keine Karten mehr. " +
                                 $"Gegner {battle.OpponentUsername} gewinnt!",
                        Timestamp = DateTime.UtcNow
                    });
                    BattleRepository.UpdateBattle(battle);
                    return battle;
                }
                if (battle.OpponentCardIds.Count == 0)
                {
                    // Spieler gewinnt
                    battle.Status = "Completed";
                    battle.Logs.Add(new BattleLog
                    {
                        Action = $"Der Gegner {battle.OpponentUsername} hat keine Karten mehr. " +
                                 $"Spieler {battle.PlayerUsername} gewinnt!",
                        Timestamp = DateTime.UtcNow
                    });
                    BattleRepository.UpdateBattle(battle);
                    return battle;
                }

                // 1) Zufällige Karten ziehen
                int playerIndex = random.Next(battle.PlayerCardIds.Count);
                int oppIndex = random.Next(battle.OpponentCardIds.Count);

                int playerCardId = battle.PlayerCardIds[playerIndex];
                int oppCardId = battle.OpponentCardIds[oppIndex];

                var playerCard = CardRepository.GetCardById(playerCardId);
                var opponentCard = CardRepository.GetCardById(oppCardId);

                if (playerCard == null || opponentCard == null)
                {
                    // Falls aus der DB nichts zurückkommt, überspringen wir die Runde
                    roundCount++;
                    continue;
                }

                // 2) Schaden vergleichen (noch ohne Element-Faktor)
                int playerDamage = playerCard.Damage;
                int opponentDamage = opponentCard.Damage;

                string roundLog = 
                    $"Runde {roundCount + 1}: {battle.PlayerUsername}'s {playerCard.Name}({playerDamage}) " +
                    $"vs {battle.OpponentUsername}'s {opponentCard.Name}({opponentDamage}) -> ";

                if (playerDamage > opponentDamage)
                {
                    // Spieler gewinnt die Runde
                    battle.OpponentCardIds.Remove(oppCardId);
                    battle.PlayerCardIds.Add(oppCardId);

                    roundLog += 
                        $"Spieler {battle.PlayerUsername} gewinnt. " +
                        $"Karte {opponentCard.Name} wechselt den Besitzer.";
                }
                else if (opponentDamage > playerDamage)
                {
                    // Gegner gewinnt die Runde
                    battle.PlayerCardIds.Remove(playerCardId);
                    battle.OpponentCardIds.Add(playerCardId);

                    roundLog += 
                        $"Gegner {battle.OpponentUsername} gewinnt. " +
                        $"Karte {playerCard.Name} wechselt den Besitzer.";
                }
                else
                {
                    // Draw
                    roundLog += "Unentschieden. Keine Karten werden übertragen.";
                }

                battle.Logs.Add(new BattleLog
                {
                    Action = roundLog,
                    Timestamp = DateTime.UtcNow
                });

                roundCount++;
            }

            // Falls wir hier rauskommen: 100 Runden vorbei => Draw
            battle.Status = "Completed";
            battle.Logs.Add(new BattleLog
            {
                Action = $"Nach {maxRounds} Runden kein Sieger. Battle endet im Unentschieden.",
                Timestamp = DateTime.UtcNow
            });
            BattleRepository.UpdateBattle(battle);

            return battle;
        }

        // Beispiel-Methode für Summen-Schaden (vorher schon in deinem Code)
        private int CalculateDamage(List<int> cardIds)
        {
            int totalDamage = 0;
            foreach (var id in cardIds)
            {
                var card = CardRepository.GetCardById(id);
                if (card != null)
                    totalDamage += card.Damage;
            }
            return totalDamage;
        }
    }
}
