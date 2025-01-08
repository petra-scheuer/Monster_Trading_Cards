using System;
using System.Collections.Generic;
using System.Linq;
using MonsterCardTradingGame.Models;

namespace MonsterCardTradingGame.Repositories
{
    public static class PowerUpRepository
    {
        public static bool CreatePowerUp(string username, string powerUpType = "double_damage")
        {
            const string sql = @"
                INSERT INTO powerups (username, powerup_type, is_used)
                VALUES (@u, @t, FALSE);
            ";
            DatabaseManager.ExecuteNonQuery(sql,
                ("u", username),
                ("t", powerUpType));
            return true;
        }

        /// <summary>
        /// Holt alle PowerUps eines Users, optional gefiltert ob is_used oder nicht.
        /// </summary>
        public static List<PowerUp> GetPowerUps(string username, bool onlyUnused = false)
        {
            string sql = @"SELECT id, username, powerup_type, is_used, created_at
                           FROM powerups
                           WHERE username = @u";
            if (onlyUnused)
            {
                sql += " AND is_used = FALSE";
            }

            return DatabaseManager.ExecuteReader<PowerUp>(sql, reader =>
            {
                return new PowerUp
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    PowerUpType = reader.GetString(2),
                    IsUsed = reader.GetBoolean(3),
                    CreatedAt = reader.GetDateTime(4)
                };
            }, ("u", username));
        }

        /// <summary>
        /// Markiert einen PowerUp als verbraucht (is_used = TRUE).
        /// </summary>
        public static bool MarkPowerUpAsUsed(int powerUpId)
        {
            const string sql = @"UPDATE powerups SET is_used = TRUE WHERE id = @id";
            DatabaseManager.ExecuteNonQuery(sql, ("id", powerUpId));
            return true;
        }
    }
}