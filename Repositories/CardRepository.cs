// CardRepository.cs

using System;
using System.Collections.Generic;
using MonsterCardTradingGame.Models; // Stelle sicher, dass du den korrekten Namespace verwendest

namespace MonsterCardTradingGame.Repositories
{
    public static class CardRepository
    {
        // Adds a new card to the database
        public static bool AddCard(string username, Card card)
        {
            const string sql = @"INSERT INTO cards (name, type, damage, element, owner_username)
                                 VALUES (@name, @type, @damage, @element, @owner_username)";
            DatabaseManager.ExecuteNonQuery(sql,
                ("name", card.Name),
                ("type", card.Type),
                ("damage", card.Damage),
                ("element", card.Element),
                ("owner_username", username));
            return true;
        }

        // Retrieves all cards owned by a user
        public static List<Card> GetUserCards(string username)
        {
            const string sql = @"SELECT id, name, type, damage, element
                                 FROM cards
                                 WHERE owner_username = @owner_username"; // Ändere @username zu @owner_username
            return DatabaseManager.ExecuteReader<Card>(sql, reader =>
            {
                return reader.GetString(2).ToLower() switch
                {
                    "spell" => new SpellCard
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Type = reader.GetString(2),
                        Damage = reader.GetInt32(3),
                        Element = reader.GetString(4)
                    },
                    "monster" => new MonsterCard
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Type = reader.GetString(2),
                        Damage = reader.GetInt32(3),
                        Element = reader.GetString(4)
                    },
                    _ => throw new Exception("Unknown card type.")
                };
            }, ("owner_username", username)); // Ändere ("owner", username) zu ("owner_username", username)
        }

        // Removes a card from the database
        public static bool RemoveCard(string username, int cardId)
        {
            // Ensure the card belongs to the user
            const string sqlCheck = @"SELECT COUNT(*) FROM cards WHERE id = @id AND owner_username = @owner_username"; // Ändere @owner zu @owner_username
            object? result = DatabaseManager.ExecuteScalar(sqlCheck, ("id", cardId), ("owner_username", username)); // Ändere ("owner", username) zu ("owner_username", username)
            long count = result != null ? Convert.ToInt64(result) : 0;
            if (count == 0)
            {
                return false;
            }

            // Delete the card
            const string sqlDelete = @"DELETE FROM cards WHERE id = @id";
            DatabaseManager.ExecuteNonQuery(sqlDelete, ("id", cardId));
            return true;
        }

        public static List<Card> GetAllCards()
        {
            const string sql = @"SELECT id, name, type, damage, element FROM cards";
            return DatabaseManager.ExecuteReader<Card>(sql, reader =>
            {
                return reader.GetString(2).ToLower() switch
                {
                    "spell" => new SpellCard
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Type = reader.GetString(2),
                        Damage = reader.GetInt32(3),
                        Element = reader.GetString(4)
                    },
                    "monster" => new MonsterCard
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Type = reader.GetString(2),
                        Damage = reader.GetInt32(3),
                        Element = reader.GetString(4)
                    },
                    _ => throw new Exception("Unknown card type.")
                };
            });
        }

        // Repositories/CardRepository.cs
        public static List<Card> GetCardsByIds(List<int> cardIds)
        {
            if (cardIds == null || cardIds.Count == 0)
                return new List<Card>();

            // Erstelle einen Parameter-String für IN-Clause
            var parameters = new List<string>();
            var sqlParams = new List<(string paramName, object paramValue)>();
            for (int i = 0; i < cardIds.Count; i++)
            {
                string param = $"@id{i}";
                parameters.Add(param);
                sqlParams.Add((param, cardIds[i]));
            }

            string inClause = string.Join(", ", parameters);
            string sql = $"SELECT id, name, type, damage, element FROM cards WHERE id IN ({inClause})";

            return DatabaseManager.ExecuteReader<Card>(sql, reader =>
            {
                return reader.GetString(2).ToLower() switch
                {
                    "spell" => new SpellCard
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Type = reader.GetString(2),
                        Damage = reader.GetInt32(3),
                        Element = reader.GetString(4)
                    },
                    "monster" => new MonsterCard
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Type = reader.GetString(2),
                        Damage = reader.GetInt32(3),
                        Element = reader.GetString(4)
                    },
                    _ => throw new Exception("Unknown card type.")
                };
            }, sqlParams.ToArray());
        }

        public static Card? GetCardById(int cardId)
        {
            const string sql = @"SELECT id, name, type, damage, element FROM cards WHERE id = @id";
            var cards = DatabaseManager.ExecuteReader<Card>(sql, reader =>
            {
                return reader.GetString(2).ToLower() switch
                {
                    "spell" => new SpellCard
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Type = reader.GetString(2),
                        Damage = reader.GetInt32(3),
                        Element = reader.GetString(4)
                    },
                    "monster" => new MonsterCard
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Type = reader.GetString(2),
                        Damage = reader.GetInt32(3),
                        Element = reader.GetString(4)
                    },
                    _ => throw new Exception("Unknown card type.")
                };
            }, ("id", cardId));

            return cards.FirstOrDefault();
        }
        
// Füge diese Methode zu CardRepository hinzu
        public static bool TransferCardOwnership(int cardId, string newOwnerUsername)
        {
            const string sql = @"UPDATE cards SET owner_username = @newOwner WHERE id = @id";
            DatabaseManager.ExecuteNonQuery(sql, ("newOwner", newOwnerUsername), ("id", cardId));
            return true;
        }

    }
}
