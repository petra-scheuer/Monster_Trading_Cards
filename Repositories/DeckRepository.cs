//DeckRepository.cs

using System;
using System.Collections.Generic;
using MonsterCardTradingGame.Models;

namespace MonsterCardTradingGame.Repositories
{
    public static class DeckRepository
    {
        /// <summary>
        /// Erstellt oder aktualisiert ein Deck für einen Benutzer.
        /// </summary>
        /// <param name="username">Der Benutzername.</param>
        /// <param name="cardIds">Liste der Karten-IDs (genau 4).</param>
        /// <returns>True, wenn erfolgreich; sonst False.</returns>
        public static bool CreateOrUpdateDeck(string username, List<int> cardIds)
        {
            if (cardIds.Count != 4)
            {
                throw new ArgumentException("Ein Deck muss genau 4 Karten enthalten.");
            }

            // Überprüfen, ob der Benutzer diese Karten besitzt
            var userCards = CardRepository.GetUserCards(username);
            foreach (var cardId in cardIds)
            {
                if (!userCards.Exists(c => c.Id == cardId))
                {
                    throw new Exception($"Karte mit ID {cardId} gehört nicht zu dem Benutzer.");
                }
            }

            // Löschen vorhandener Decks des Benutzers
            const string deleteSql = @"DELETE FROM decks WHERE owner_username = @u";
            DatabaseManager.ExecuteNonQuery(deleteSql, ("u", username));

            // Einfügen des neuen Decks
            const string insertSql = @"INSERT INTO decks (owner_username, card1_id, card2_id, card3_id, card4_id)
                                       VALUES (@u, @c1, @c2, @c3, @c4)";
            DatabaseManager.ExecuteNonQuery(insertSql,
                ("u", username),
                ("c1", cardIds[0]),
                ("c2", cardIds[1]),
                ("c3", cardIds[2]),
                ("c4", cardIds[3]));

            return true;
        }

        /// <summary>
        /// Holt das Deck eines Benutzers.
        /// </summary>
        /// <param name="username">Der Benutzername.</param>
        /// <returns>Ein Deck-Objekt oder null, wenn kein Deck existiert.</returns>
        public static Deck? GetDeck(string username)
        {
            const string sql = @"SELECT id, card1_id, card2_id, card3_id, card4_id
                                 FROM decks
                                 WHERE owner_username = @u";

            var result = DatabaseManager.ExecuteReader(sql, reader =>
            {
                return new Deck
                {
                    Id = reader.GetInt32(0),
                    Username = username,
                    CardIds = new List<int>
                    {
                        reader.GetInt32(1),
                        reader.GetInt32(2),
                        reader.GetInt32(3),
                        reader.GetInt32(4)
                    }
                };
            }, ("u", username));

            return result.Count > 0 ? result[0] : null;
        }
    }
}
