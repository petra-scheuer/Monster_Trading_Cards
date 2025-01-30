using System;
using System.Collections.Generic;
using System.Linq;
using MonsterCardTradingGame.Models;
using MonsterCardTradingGame.Repositories;

namespace MonsterCardTradingGame.Logic
{
    public class BattleLogic
    {
        public Battle StartBattle(string playerUsername, List<int> playerCardIds)
        {
            // Neues Battle im Repository anlegen
            var battle = BattleRepository.CreateBattle(playerUsername, playerCardIds);

            // Erster Log-Eintrag
            battle.Logs.Add(new BattleLog
            {
                Action = $"Battle gestartet zwischen {playerUsername} und {battle.OpponentUsername}.",
                Timestamp = DateTime.UtcNow
            });

            BattleRepository.UpdateBattle(battle);
            return battle;
        }

        // Optionales Beispiel für Einzelrunde:
        public Battle PerformBattleTurn(Guid battleId)
        {
            throw new NotImplementedException("Beispiel-Runden-Feature, siehe PerformFullBattle für komplette Logik.");
        }

        /// <summary>
        /// Führt das komplette Battle in bis zu 100 Runden durch
        /// inklusive Element-Berechnung und Spezialregeln.
        /// </summary>
        public Battle PerformFullBattle(Guid battleId)
        {
            var battle = BattleRepository.GetBattle(battleId);
            if (battle == null)
                throw new Exception("Battle nicht gefunden.");

            if (battle.Status != "In Progress")
                throw new Exception("Battle ist nicht mehr aktiv oder bereits abgeschlossen.");

            const int maxRounds = 100;
            int roundCount = 0;
            var random = new Random();

            while (roundCount < maxRounds)
            {
                // 1) Prüfen, ob ein Spieler keine Karten mehr hat => Kampf vorbei
                if (battle.PlayerCardIds.Count == 0)
                {
                    battle.Status = "Completed";
                    var logMsg = $"Der Spieler {battle.PlayerUsername} hat keine Karten mehr. " +
                                 $"Gegner {battle.OpponentUsername} gewinnt!";
                    battle.Logs.Add(new BattleLog
                    {
                        Action = logMsg,
                        Timestamp = DateTime.UtcNow
                    });

                    UpdateEloValues(battle.OpponentUsername, battle.PlayerUsername);
                    BattleRepository.UpdateBattle(battle);
                    return battle;
                }
                if (battle.OpponentCardIds.Count == 0)
                {
                    battle.Status = "Completed";
                    var logMsg = $"Der Gegner {battle.OpponentUsername} hat keine Karten mehr. " +
                                 $"Spieler {battle.PlayerUsername} gewinnt!";
                    battle.Logs.Add(new BattleLog
                    {
                        Action = logMsg,
                        Timestamp = DateTime.UtcNow
                    });

                    UpdateEloValues(battle.PlayerUsername, battle.OpponentUsername);
                    BattleRepository.UpdateBattle(battle);
                    return battle;
                }

                // 2) Pro Runde zufällig je 1 Karte wählen
                int playerIndex = random.Next(battle.PlayerCardIds.Count);
                int oppIndex = random.Next(battle.OpponentCardIds.Count);

                int playerCardId = battle.PlayerCardIds[playerIndex];
                int oppCardId = battle.OpponentCardIds[oppIndex];

                var playerCard = CardRepository.GetCardById(playerCardId);
                var opponentCard = CardRepository.GetCardById(oppCardId);

                if (playerCard == null || opponentCard == null)
                {
                    roundCount++;
                    continue; // sollte eigentlich nicht passieren, zur Sicherheit überspringen wir
                }

                // 3) Schaden berechnen mit Speziallogik & Elementen
                int playerDamage = CalculateBattleDamage(playerCard, opponentCard);
                int opponentDamage = CalculateBattleDamage(opponentCard, playerCard);

                string roundLog =
                    $"Runde {roundCount + 1}: {battle.PlayerUsername}'s {playerCard.Name}({playerDamage}) " +
                    $"vs {battle.OpponentUsername}'s {opponentCard.Name}({opponentDamage}) -> ";

                // 4) Gewinner der Runde ermitteln + Karte übertragen
                if (playerDamage > opponentDamage)
                {
                    //in Memory
                    battle.OpponentCardIds.Remove(oppCardId);
                    battle.PlayerCardIds.Add(oppCardId);

                    // in DB
                    CardRepository.TransferCardOwnership(oppCardId, battle.PlayerUsername);

                    roundLog += $"Spieler {battle.PlayerUsername} gewinnt. Karte {opponentCard.Name} wechselt den Besitzer.";
                }
                else if (opponentDamage > playerDamage)
                {
                    battle.PlayerCardIds.Remove(playerCardId);
                    battle.OpponentCardIds.Add(playerCardId);
                    
                    CardRepository.TransferCardOwnership(playerCardId, battle.OpponentUsername);

                    roundLog += $"Gegner {battle.OpponentUsername} gewinnt. Karte {playerCard.Name} wechselt den Besitzer.";
                }
                else
                {
                    roundLog += "Unentschieden. Keine Karten werden übertragen.";
                }

                // 5) Log-Eintrag für die Runde
                battle.Logs.Add(new BattleLog
                {
                    Action = roundLog,
                    Timestamp = DateTime.UtcNow
                });

                roundCount++;
            }

            // 6) Rundenlimit erreicht => Unentschieden (keine ELO-Änderung)
            battle.Status = "Completed";
            battle.Logs.Add(new BattleLog
            {
                Action = $"Nach {maxRounds} Runden kein Sieger. Battle endet im Unentschieden (keine ELO-Änderung).",
                Timestamp = DateTime.UtcNow
            });
            BattleRepository.UpdateBattle(battle);

            return battle;
        }

        /// <summary>
        /// Berechnet den Schaden einer Karte gegen eine andere Karte (Elemente + Spezialregeln).
        /// </summary>
        private int CalculateBattleDamage(Card attacker, Card defender)
        {
            // Spezialfall: Goblin vs. Dragon
            if (IsGoblin(attacker) && IsDragon(defender))
            {
                return 0;
            }

            // Wizard kontrolliert Ork => Ork macht 0
            if (IsOrk(attacker) && IsWizard(defender))
            {
                return 0;
            }

            // Knight vs WaterSpell => Sofort-KO
            if (IsKnight(defender) && IsSpell(attacker) && attacker.Element.ToLower() == "water")
            {
                return 9999; // Beliebig hoch
            }

            // Kraken immun gegen Spells
            if (IsSpell(attacker) && IsKraken(defender))
            {
                return 0;
            }

            // FireElves entgehen Dragons => Dragon-Schaden = 0 (aber hier attacker=Dragon, defender=FireElve?)
            if (IsDragon(attacker) && IsFireElve(defender))
            {
                return 0;
            }

            // Basis-Damage
            int baseDamage = attacker.Damage;

            // Nur wenn mind. eine Spell-Karte
            if (IsSpell(attacker) || IsSpell(defender))
            {
                // Element-Faktor gut = *2, schelcht = *0.5
                double factor = GetElementFactor(attacker.Element.ToLower(), defender.Element.ToLower());
                baseDamage = (int)(baseDamage * factor);
            }

            return baseDamage;
        }

        // Element-Faktor (Wasser->Feuer => double, Feuer->Wasser => half, usw.)
        private double GetElementFactor(string attackerElement, string defenderElement)
        {
            if (attackerElement == "water" && defenderElement == "fire") return 2.0;
            if (attackerElement == "fire" && defenderElement == "normal") return 2.0;
            if (attackerElement == "normal" && defenderElement == "water") return 2.0;

            if (attackerElement == "fire" && defenderElement == "water") return 0.5;
            if (attackerElement == "normal" && defenderElement == "fire") return 0.5;
            if (attackerElement == "water" && defenderElement == "normal") return 0.5;

            return 1.0;
        }

        // ELO: +3 für Gewinner, -5 für Verlierer
        private void UpdateEloValues(string winner, string loser)
        {
            UserRepository.UpdateELO(winner, +3);
            UserRepository.UpdateELO(loser, -5);
        }

        // Hilfsfunktionen für Namens Checks
        private bool IsSpell(Card c) => c.Type.Equals("spell", StringComparison.OrdinalIgnoreCase);
        private bool IsGoblin(Card c) => c.Name.ToLower().Contains("goblin");
        private bool IsDragon(Card c) => c.Name.ToLower().Contains("dragon");
        private bool IsOrk(Card c) => c.Name.ToLower().Contains("ork");
        private bool IsWizard(Card c) => c.Name.ToLower().Contains("wizzard") || c.Name.ToLower().Contains("wizard");
        private bool IsKnight(Card c) => c.Name.ToLower().Contains("knight");
        private bool IsKraken(Card c) => c.Name.ToLower().Contains("kraken");
        private bool IsFireElve(Card c) => c.Name.ToLower().Contains("fireelve") || c.Name.ToLower().Contains("fireelf");
    }
}
