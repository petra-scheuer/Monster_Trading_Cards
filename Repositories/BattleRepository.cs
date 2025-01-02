// Repositories/BattleRepository.cs
using System;
using MonsterCardTradingGame.Models;
using System.Collections.Generic;

namespace MonsterCardTradingGame.Repositories
{
    public static class BattleRepository
    {
        public static bool AddBattle(Battle battle)
        {
            const string sql = @"INSERT INTO battles (player1_username, player2_username, battle_log, winner_username, created_at)
                                 VALUES (@p1, @p2, @log, @winner, @created_at)";
            DatabaseManager.ExecuteNonQuery(sql,
                ("p1", battle.Player1Username),
                ("p2", battle.Player2Username),
                ("log", battle.BattleLog),
                ("winner", (object?)battle.WinnerUsername ?? DBNull.Value),
                ("created_at", battle.CreatedAt));
            return true;
        }

        public static List<Battle> GetAllBattles()
        {
            const string sql = @"SELECT id, player1_username, player2_username, battle_log, winner_username, created_at FROM battles";
            return DatabaseManager.ExecuteReader(sql, reader =>
            {
                return new Battle
                {
                    Id = reader.GetInt32(0),
                    Player1Username = reader.GetString(1),
                    Player2Username = reader.GetString(2),
                    BattleLog = reader.GetString(3),
                    WinnerUsername = reader.IsDBNull(4) ? null : reader.GetString(4),
                    CreatedAt = reader.GetDateTime(5)
                };
            });
        }

    }
}