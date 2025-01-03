using System;
using System.Collections.Generic;
using System.Linq;
using MonsterCardTradingGame.Models;

namespace MonsterCardTradingGame.Repositories
{
    public static class TradeRepository
    {
        /// <summary>
        /// Erstellt ein neues Handelsangebot.
        /// </summary>
        public static bool CreateTrade(string offeredByUsername, int offeredCardId, string requirementType, string? requirementElement, int? requirementMinDamage)
        {
            const string sql = @"INSERT INTO trades (offered_by_username, offered_card_id, requirement_type, requirement_element, requirement_min_damage)
                                 VALUES (@u, @c, @t, @e, @m)";
            DatabaseManager.ExecuteNonQuery(sql,
                ("u", offeredByUsername),
                ("c", offeredCardId),
                ("t", requirementType),
                ("e", (object?)requirementElement ?? DBNull.Value),
                ("m", (object?)requirementMinDamage ?? DBNull.Value));
            return true;
        }

        /// <summary>
        /// Holt alle aktiven Handelsangebote.
        /// </summary>
        public static List<Trade> GetActiveTrades()
        {
            const string sql = @"SELECT id, offered_by_username, offered_card_id, requirement_type, requirement_element, requirement_min_damage, is_active, created_at
                                 FROM trades
                                 WHERE is_active = TRUE";
            return DatabaseManager.ExecuteReader(sql, reader =>
            {
                return new Trade
                {
                    Id = reader.GetInt32(0),
                    OfferedByUsername = reader.GetString(1),
                    OfferedCardId = reader.GetInt32(2),
                    RequirementType = reader.GetString(3),
                    RequirementElement = reader.IsDBNull(4) ? null : reader.GetString(4),
                    RequirementMinDamage = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    IsActive = reader.GetBoolean(6),
                    CreatedAt = reader.GetDateTime(7)
                };
            });
        }

        /// <summary>
        /// Holt ein spezifisches Handelsangebot nach ID.
        /// </summary>
        public static Trade? GetTradeById(int tradeId)
        {
            const string sql = @"SELECT id, offered_by_username, offered_card_id, requirement_type, requirement_element, requirement_min_damage, is_active, created_at
                                 FROM trades
                                 WHERE id = @id AND is_active = TRUE";
            var trades = DatabaseManager.ExecuteReader(sql, reader =>
            {
                return new Trade
                {
                    Id = reader.GetInt32(0),
                    OfferedByUsername = reader.GetString(1),
                    OfferedCardId = reader.GetInt32(2),
                    RequirementType = reader.GetString(3),
                    RequirementElement = reader.IsDBNull(4) ? null : reader.GetString(4),
                    RequirementMinDamage = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    IsActive = reader.GetBoolean(6),
                    CreatedAt = reader.GetDateTime(7)
                };
            }, ("id", tradeId));

            return trades.FirstOrDefault();
        }

        /// <summary>
        /// Deaktiviert ein Handelsangebot nach ID.
        /// </summary>
        public static bool DeactivateTrade(int tradeId)
        {
            const string sql = @"UPDATE trades SET is_active = FALSE WHERE id = @id";
            DatabaseManager.ExecuteNonQuery(sql, ("id", tradeId));
            return true;
        }
    }
}
