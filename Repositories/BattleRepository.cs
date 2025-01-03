// Repositories/BattleRepository.cs
using System;
using System.Collections.Generic;
using System.Linq;
using MonsterCardTradingGame.Models;

namespace MonsterCardTradingGame.Repositories
{
    public static class BattleRepository
    {
        private static List<Battle> Battles = new List<Battle>();

        public static Battle CreateBattle(string playerUsername, List<int> playerCardIds, string opponentUsername = "AI")
        {
            var battle = new Battle
            {
                PlayerUsername = playerUsername,
                OpponentUsername = opponentUsername,
                PlayerCardIds = playerCardIds
                // OpponentCardIds können zufällig generiert oder aus vordefinierten Decks ausgewählt werden
            };
            Battles.Add(battle);
            return battle;
        }

        public static Battle? GetBattle(Guid battleId)
        {
            return Battles.FirstOrDefault(b => b.Id == battleId);
        }

        public static void UpdateBattle(Battle battle)
        {
            var index = Battles.FindIndex(b => b.Id == battle.Id);
            if (index != -1)
            {
                Battles[index] = battle;
            }
        }

        public static List<Battle> GetBattlesByPlayer(string playerUsername)
        {
            return Battles.Where(b => b.PlayerUsername == playerUsername || b.OpponentUsername == playerUsername).ToList();
        }
    }
}