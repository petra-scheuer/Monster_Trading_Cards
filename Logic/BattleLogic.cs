using System;
using System.Collections.Generic;
using System.Linq;
using MonsterCardTradingGame.Models;
using MonsterCardTradingGame.Repositories;

namespace MonsterCardTradingGame.Logic
{
    public class BattleLogic
    {
        // StartBattle und PerformBattleTurn bleiben weitgehend gleich wie vorher.
        // Die Änderungen konzentrieren sich auf PerformFullBattle und eine neue Damage-Berechnung.

        public Battle StartBattle(string playerUsername, List<int> playerCardIds)
        {
            // Neues Battle im Repository anlegen
            var battle = BattleRepository.CreateBattle(playerUsername, playerCardIds);

            // Log
            battle.Logs.Add(new BattleLog
            {
                Action = $"Battle gestartet zwischen {playerUsername} und {battle.OpponentUsername}.",
                Timestamp = DateTime.UtcNow
            });

            BattleRepository.UpdateBattle(battle);
            return battle;
        }

        public Battle PerformBattleTurn(Guid battleId)
        {
            // … (unverändert, falls du das beibehalten möchtest)
            throw new NotImplementedException("Bitte weiterhin oder alternativ FULL-Battle verwenden.");
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
                    var logMsg = $"Der Spieler {battle.PlayerUsername} hat keine Karten mehr. Gegner {battle.OpponentUsername} gewinnt!";
                    battle.Logs.Add(new BattleLog { Action = logMsg, Timestamp = DateTime.UtcNow });
                    
                    // ELO anpassen: Opponent +3, Player -5
                    UpdateEloValues(battle.OpponentUsername, battle.PlayerUsername);

                    BattleRepository.UpdateBattle(battle);
                    return battle;
                }
                if (battle.OpponentCardIds.Count == 0)
                {
                    // Spieler gewinnt
                    battle.Status = "Completed";
                    var logMsg = $"Der Gegner {battle.OpponentUsername} hat keine Karten mehr. Spieler {battle.PlayerUsername} gewinnt!";
                    battle.Logs.Add(new BattleLog { Action = logMsg, Timestamp = DateTime.UtcNow });
                    
                    // ELO anpassen: Player +3, Opponent -5
                    UpdateEloValues(battle.PlayerUsername, battle.OpponentUsername);

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

                // 2) Schaden berechnen – jetzt mit Elementen & Spezialregeln
                int playerDamage = CalculateBattleDamage(playerCard, opponentCard);
                int opponentDamage = CalculateBattleDamage(opponentCard, playerCard);

                string roundLog = 
                    $"Runde {roundCount + 1}: {battle.PlayerUsername}'s {playerCard.Name}({playerDamage}) " +
                    $"vs {battle.OpponentUsername}'s {opponentCard.Name}({opponentDamage}) -> ";

                if (playerDamage > opponentDamage)
                {
                    // Spieler gewinnt die Runde
                    battle.OpponentCardIds.Remove(oppCardId);
                    battle.PlayerCardIds.Add(oppCardId);

                    roundLog += $"Spieler {battle.PlayerUsername} gewinnt. Karte {opponentCard.Name} wechselt den Besitzer.";
                }
                else if (opponentDamage > playerDamage)
                {
                    // Gegner gewinnt die Runde
                    battle.PlayerCardIds.Remove(playerCardId);
                    battle.OpponentCardIds.Add(playerCardId);

                    roundLog += $"Gegner {battle.OpponentUsername} gewinnt. Karte {playerCard.Name} wechselt den Besitzer.";
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
                Action = $"Nach {maxRounds} Runden kein Sieger. Battle endet im Unentschieden (keine ELO-Änderung).",
                Timestamp = DateTime.UtcNow
            });
            BattleRepository.UpdateBattle(battle);

            return battle;
        }

        /// <summary>
        /// Berechnet den Schaden einer Karte gegen eine andere Karte unter Berücksichtigung von
        /// 1) Element-Effekten,
        /// 2) Spezialregeln (Goblin vs Dragon, Wizard vs Ork, Knight vs WaterSpell, Kraken immun usw.).
        /// </summary>
        private int CalculateBattleDamage(Card attacker, Card defender)
        {
            // 0) Spezialfälle (z. B. Goblin vs Dragon)
            //    Wenn ein MonsterName "Goblin" auf "Dragon" trifft, Goblin-Damage = 0
            //    -> Wir schauen uns an, ob attacker = Goblin + defender = Dragon
            if (IsGoblin(attacker) && IsDragon(defender))
            {
                return 0; // Goblin greift Dragon nicht an
            }

            // Wizard kontrolliert Ork => Ork macht 0 Schaden gegen Wizard
            //    -> das hier greift, wenn attacker=Ork und defender=Wizard
            if (IsOrk(attacker) && IsWizard(defender))
            {
                return 0; 
            }

            // Knights ertrinken bei WaterSpells => Knight instant KO => Damage = sehr hoch
            //    -> wenn (attacker Spell(Element=water)) vs Knight, dann 999 z.B.
            if (IsKnight(defender) && IsSpell(attacker) && attacker.Element.ToLower() == "water")
            {
                // Knight wird sofort ertränkt
                return 999;
            }

            // Kraken ist immun gegen Spells => wenn attacker=Spell und defender=Kraken => 0
            if (IsSpell(attacker) && IsKraken(defender))
            {
                return 0;
            }

            // FireElves entgehen Dragons => Dragon-Schaden gegen FireElve = 0
            //    -> wenn attacker=Dragon und defender=FireElve => attacker=0
            if (IsDragon(attacker) && IsFireElve(defender))
            {
                return 0;
            }

            // 1) Basis-Damage (aus dem Cardobjekt)
            int baseDamage = attacker.Damage;

            // 2) Prüfen, ob wir mindestens eine Spell-Karte im Fight haben
            bool attackerIsSpell = IsSpell(attacker);
            bool defenderIsSpell = IsSpell(defender);

            // Nur wenn mind. ein Spell dabei ist, kommen die Element-Vor-/Nachteile
            if (attackerIsSpell || defenderIsSpell)
            {
                // attacker.Element vs defender.Element:
                // water -> fire => double
                // fire -> normal => double
                // normal -> water => double
                // Falls "umgekehrt", => half
                double factor = GetElementFactor(attacker.Element.ToLower(), defender.Element.ToLower());
                baseDamage = (int)(baseDamage * factor);
            }

            return baseDamage;
        }

        /// <summary>
        /// Liefert den Element-Faktor (2.0 = double, 0.5 = half, 1.0 = normal).
        /// </summary>
        private double GetElementFactor(string attackerElement, string defenderElement)
        {
            // Spezifikation: 
            // water -> fire => double
            // fire -> normal => double
            // normal -> water => double
            // und das Umgekehrte => half
            if (attackerElement == "water" && defenderElement == "fire") return 2.0;
            if (attackerElement == "fire" && defenderElement == "normal") return 2.0;
            if (attackerElement == "normal" && defenderElement == "water") return 2.0;

            // Das Umgekehrte => 0.5
            if (attackerElement == "fire" && defenderElement == "water") return 0.5;
            if (attackerElement == "normal" && defenderElement == "fire") return 0.5;
            if (attackerElement == "water" && defenderElement == "normal") return 0.5;

            // Alles andere = 1.0 (keine Änderung)
            return 1.0;
        }

        /// <summary>
        /// Aktualisiert die ELO-Werte: winner +3, loser -5
        /// </summary>
        private void UpdateEloValues(string winner, string loser)
        {
            UserRepository.UpdateELO(winner, +3);
            UserRepository.UpdateELO(loser, -5);
        }

        // ==== Hilfsfunktionen zur Erkennung der Monster-Typen ====
        // Hier sehr vereinfacht durch Name-Checks.
        private bool IsSpell(Card c) => c.Type.Equals("spell", StringComparison.OrdinalIgnoreCase);

        private bool IsGoblin(Card c) => c.Name.ToLower().Contains("goblin");
        private bool IsDragon(Card c) => c.Name.ToLower().Contains("dragon");
        private bool IsOrk(Card c) => c.Name.ToLower().Contains("ork");
        private bool IsWizard(Card c) => c.Name.ToLower().Contains("wizzard") || c.Name.ToLower().Contains("wizard");
        private bool IsKnight(Card c) => c.Name.ToLower().Contains("knight");
        private bool IsKraken(Card c) => c.Name.ToLower().Contains("kraken");
        private bool IsFireElve(Card c) => c.Name.ToLower().Contains("fireelve") || c.Name.ToLower().Contains("fireelve");

        // Evtl. dein Summen-Schaden für performBattleTurn
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
