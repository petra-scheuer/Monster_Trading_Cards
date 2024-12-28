using System;
using Npgsql;
using System.Collections.Generic;

namespace MonsterCardTradingGame
{
    /// <summary>
    /// Stellt Methoden für den Datenbankzugriff bezüglich Benutzer bereit.
    /// </summary>
    public static class UserRepository
    {
        /// <summary>
        /// Erstellt einen neuen Benutzer in der Datenbank.
        /// </summary>
        /// <param name="username">Der Benutzername.</param>
        /// <param name="password">Das Passwort.</param>
        /// <returns>True, wenn der Benutzer erfolgreich erstellt wurde; sonst False.</returns>
        public static bool CreateUser(string username, string password)
        {
            // Prüfen, ob der Benutzer bereits existiert
            if (GetUser(username) != null)
            {
                return false;
            }

            // Benutzer anlegen
            const string sql = @"INSERT INTO users (username, password, token, coins, elo)
                                 VALUES (@u, @p, '', 20, 100)";
            DatabaseManager.ExecuteNonQuery(sql, ("u", username), ("p", password));
            return true;
        }

        /// <summary>
        /// Holt einen Benutzer aus der Datenbank.
        /// </summary>
        /// <param name="username">Der Benutzername.</param>
        /// <returns>Ein UserData-Objekt oder null, wenn der Benutzer nicht gefunden wurde.</returns>
        public static UserData? GetUser(string username)
        {
            const string sql = @"SELECT username, password, token, coins, elo 
                                 FROM users
                                 WHERE username = @u";

            var result = DatabaseManager.ExecuteReader(sql, reader =>
            {
                return new UserData
                {
                    Username = reader.GetString(0),
                    Password = reader.GetString(1),
                    Token = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Coins = reader.GetInt32(3),
                    ELO = reader.GetInt32(4)
                };
            }, ("u", username));

            return result.Count > 0 ? result[0] : null;
        }

        /// <summary>
        /// Aktualisiert den Token eines Benutzers.
        /// </summary>
        /// <param name="username">Der Benutzername.</param>
        /// <param name="token">Der neue Token.</param>
        /// <returns>True, wenn die Aktualisierung erfolgreich war; sonst False.</returns>
        public static bool UpdateToken(string username, string token)
        {
            const string sql = @"UPDATE users
                                 SET token = @t
                                 WHERE username = @u";
            DatabaseManager.ExecuteNonQuery(sql, ("t", token), ("u", username));
            return true;
        }

        /// <summary>
        /// Holt den Token eines Benutzers.
        /// </summary>
        /// <param name="username">Der Benutzername.</param>
        /// <returns>Der Token oder null, falls keiner gesetzt ist.</returns>
        public static string? GetToken(string username)
        {
            var user = GetUser(username);
            return user?.Token;
        }

        /// <summary>
        /// Validiert die Anmeldeinformationen eines Benutzers.
        /// </summary>
        /// <param name="username">Der Benutzername.</param>
        /// <param name="password">Das Passwort.</param>
        /// <returns>True, wenn die Anmeldeinformationen gültig sind; sonst False.</returns>
        public static bool ValidateCredentials(string username, string password)
        {
            var user = GetUser(username);
            return user != null && user.Password == password;
        }

        /// <summary>
        /// Holt alle registrierten Benutzernamen.
        /// </summary>
        /// <returns>Eine Liste von Benutzernamen.</returns>
        public static List<string> GetAllUsernames()
        {
            const string sql = @"SELECT username FROM users";
            return DatabaseManager.ExecuteReader(sql, reader => reader.GetString(0));
        }
    }

    /// <summary>
    /// Datenmodell für einen Benutzer.
    /// </summary>
    public class UserData
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public int Coins { get; set; }
        public int ELO { get; set; }
    }
}
