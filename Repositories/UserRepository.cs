// UserRepository.cs
using System;
using Npgsql;
using System.Collections.Generic;

namespace MonsterCardTradingGame.Repositories
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

            // Passwort-Hashing (Wichtig für Sicherheit!)
            string hashedPassword = HashPassword(password);

            // Benutzer anlegen
            const string sql = @"INSERT INTO users (username, password, token, coins, elo)
                                 VALUES (@u, @p, '', 20, 100)";
            DatabaseManager.ExecuteNonQuery(sql, ("u", username), ("p", hashedPassword));
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
            return user != null && VerifyPassword(password, user.Password);
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

        /// <summary>
        /// Holt den Benutzernamen anhand des Tokens.
        /// </summary>
        /// <param name="token">Das Authentifizierungstoken.</param>
        /// <returns>Der Benutzername oder null, wenn das Token ungültig ist.</returns>
        public static string? GetUsernameByToken(string token)
        {
            const string sql = @"SELECT username FROM users WHERE token = @t";
            var result = DatabaseManager.ExecuteReader(sql, reader => reader.GetString(0), ("t", token));
            return result.Count > 0 ? result[0] : null;
        }

        /// <summary>
        /// Hashes the password using a secure algorithm.
        /// (Implementiere eine sichere Hash-Funktion hier, z.B. bcrypt)
        /// </summary>
        /// <param name="password">Das zu hashende Passwort.</param>
        /// <returns>Der gehashte Passwort-String.</returns>
        private static string HashPassword(string password)
        {
            // Implementiere hier ein sicheres Hashing, z.B. mit BCrypt
            // Placeholder:
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
        }

        /// <summary>
        /// Verifiziert das Passwort gegen den gehashten Wert.
        /// </summary>
        /// <param name="password">Das eingegebene Passwort.</param>
        /// <param name="hashedPassword">Das gespeicherte gehashte Passwort.</param>
        /// <returns>True, wenn das Passwort übereinstimmt; sonst False.</returns>
        private static bool VerifyPassword(string password, string hashedPassword)
        {
            // Implementiere hier die Passwort-Verifizierung entsprechend dem Hashing-Algorithmus
            // Placeholder:
            return hashedPassword == Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
        }
        public static bool EnsureSystemUserExists()
        {
            var systemUser = GetUser("system");
            if (systemUser != null)
            {
                return true; // Benutzer existiert bereits
            }

            // Erstelle den "system" Benutzer mit einem sicheren Passwort
            // Du kannst ein starkes Passwort oder ein zufällig generiertes Token verwenden
            string systemPassword = "systempassword"; // Ändere dies zu einem sicheren Wert

            return CreateUser("system", systemPassword);
        }
        
        public static bool UpdateELO(string username, int eloChange)
        {
            const string sql = @"UPDATE users SET elo = elo + @change WHERE username = @u";
            DatabaseManager.ExecuteNonQuery(sql, ("change", eloChange), ("u", username));
            return true;
        }

        // Repositories/UserRepository.cs

        public static List<UserData> GetAllUsersOrderedByELO()
        {
            const string sql = @"SELECT username, password, token, coins, elo 
                         FROM users
                         ORDER BY elo DESC";
            return DatabaseManager.ExecuteReader<UserData>(sql, reader =>
            {
                return new UserData
                {
                    Username = reader.GetString(0),
                    Password = reader.GetString(1),
                    Token = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Coins = reader.GetInt32(3),
                    ELO = reader.GetInt32(4)
                };
            });
        }

    }

    /// <summary>
    /// Datenmodell für einen Benutzer.
    /// </summary>
    public class UserData
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // Gespeichertes gehashtes Passwort
        public string Token { get; set; } = string.Empty;
        public int Coins { get; set; }
        public int ELO { get; set; }
    }
    
}
