//DatabaseManager.cs

using System;
using Npgsql;
using System.Data;
using MonsterCardTradingGame.Repositories;


namespace MonsterCardTradingGame
{
    /// <summary>
    /// Stellt grundlegende Datenbank-Funktionen zur Verfügung,
    /// z.B. Verbindung testen, Tabellen erstellen, SQL ausführen.
    /// </summary>
    public static class DatabaseManager
    {
        /// <summary>
        /// Hier trägst du deinen Connection String ein.
        /// Bei Docker-Postgres (localhost:5432, DB=mtcg, user=postgres, pw=postgres).
        /// </summary>
        private static string _connectionString =
            "Host=localhost;Port=5433;Database=mtcg;Username=postgres;Password=postgres";

        /// <summary>
        /// Testet, ob die DB-Verbindung klappt, indem wir kurz "SELECT 1" ausführen.
        /// Gibt true zurück, wenn alles funktioniert.
        /// </summary>
        public static bool TestConnection()
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using var cmd = new NpgsqlCommand("SELECT 1", conn);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DatabaseManager] Connection failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Legt die tabellen an 
        /// </summary>
        ///
        public static void SetupTables()
{
    
    const string createUsersTable = @"
        CREATE TABLE IF NOT EXISTS users (
            username VARCHAR(50) PRIMARY KEY,
            password VARCHAR(255) NOT NULL,
            token VARCHAR(255),
            coins INT NOT NULL DEFAULT 20,
            elo INT NOT NULL DEFAULT 100
        );
    ";

    ExecuteNonQuery(createUsersTable);

    const string createCardsTable = @"
        CREATE TABLE IF NOT EXISTS cards (
            id SERIAL PRIMARY KEY,
            name VARCHAR(100) NOT NULL,
            type VARCHAR(20) NOT NULL, -- 'spell' oder 'monster'
            damage INT NOT NULL,
            element VARCHAR(20) NOT NULL, -- 'fire', 'water', 'normal'
            owner_username VARCHAR(50) REFERENCES users(username) ON DELETE CASCADE
        );
    ";

    ExecuteNonQuery(createCardsTable);

    // Tabelle für Pakete (falls implementiert)
    const string createPackagesTable = @"
        CREATE TABLE IF NOT EXISTS packages (
            id SERIAL PRIMARY KEY,
            card1_id INT REFERENCES cards(id) ON DELETE CASCADE,
            card2_id INT REFERENCES cards(id) ON DELETE CASCADE,
            card3_id INT REFERENCES cards(id) ON DELETE CASCADE,
            card4_id INT REFERENCES cards(id) ON DELETE CASCADE,
            card5_id INT REFERENCES cards(id) ON DELETE CASCADE,
            owner_username VARCHAR(50) REFERENCES users(username) ON DELETE CASCADE
        );
    ";

    ExecuteNonQuery(createPackagesTable);

    // Neue Tabelle für Decks
    const string createDecksTable = @"
        CREATE TABLE IF NOT EXISTS decks (
            id SERIAL PRIMARY KEY,
            owner_username VARCHAR(50) REFERENCES users(username) ON DELETE CASCADE,
            card1_id INT REFERENCES cards(id) ON DELETE CASCADE,
            card2_id INT REFERENCES cards(id) ON DELETE CASCADE,
            card3_id INT REFERENCES cards(id) ON DELETE CASCADE,
            card4_id INT REFERENCES cards(id) ON DELETE CASCADE
        );
    ";

    ExecuteNonQuery(createDecksTable);

    // Tabelle für Battles (falls implementiert)
    const string createBattlesTable = @"
        CREATE TABLE IF NOT EXISTS battles (
            id SERIAL PRIMARY KEY,
            player1_username VARCHAR(50) REFERENCES users(username) ON DELETE CASCADE,
            player2_username VARCHAR(50) REFERENCES users(username) ON DELETE CASCADE,
            battle_log TEXT,
            winner_username VARCHAR(50) REFERENCES users(username),
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );
    ";

    ExecuteNonQuery(createBattlesTable);
    // Tabelle für Trades
    const string createTradesTable = @"
        CREATE TABLE IF NOT EXISTS trades (
            id SERIAL PRIMARY KEY,
            offered_by_username VARCHAR(50) REFERENCES users(username) ON DELETE CASCADE,
            offered_card_id INT REFERENCES cards(id) ON DELETE CASCADE,
            requirement_type VARCHAR(20) NOT NULL, -- 'spell' oder 'monster'
            requirement_element VARCHAR(20),       -- Optional: 'fire', 'water', 'normal'
            requirement_min_damage INT,             -- Optional: Mindestschaden
            is_active BOOLEAN DEFAULT TRUE,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );
    ";

    ExecuteNonQuery(createTradesTable);
    
    const string createPowerUpsTable = @"
    CREATE TABLE IF NOT EXISTS powerups (
        id SERIAL PRIMARY KEY,
        username VARCHAR(50) REFERENCES users(username) ON DELETE CASCADE,
        powerup_type VARCHAR(50) NOT NULL, -- z.B. 'double_damage'
        is_used BOOLEAN DEFAULT FALSE,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    );
";
    ExecuteNonQuery(createPowerUpsTable);


    // **Stelle sicher, dass der "system" Benutzer existiert**
    bool systemUserCreated = UserRepository.EnsureSystemUserExists();
    if (systemUserCreated)
    {
        Console.WriteLine("[DB] 'system' Benutzer existiert oder wurde erstellt.");
    }
    else
    {
        Console.WriteLine("[DB] Fehler beim Erstellen des 'system' Benutzers.");
    }

    // Seed-Karten hinzufügen
    SeedCards();
}
        
        /// <summary>
        /// Führt ein das übergebene SQL-Statement aus, das 
        /// keine Rückgabe erfordert (z.B. CREATE TABLE, INSERT, UPDATE, DELETE).
        /// </summary>
        public static void ExecuteNonQuery(string sql)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Führt das übergebene SQL Statment mit Parametern aus ( Vorbeugung von SQL-Injection).
        /// </summary>
        public static void ExecuteNonQuery(string sql, params (string paramName, object paramValue)[] parameters)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);

            foreach (var (paramName, paramValue) in parameters)
            {
                cmd.Parameters.AddWithValue(paramName, paramValue);
            }

            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Führt ein übergebenes SQL-Statement aus, das genau einen Wert zurückliefert
        /// (z.B. SELECT count(*) FROM users) und gibt den wert als object zurück.
        /// </summary>
        public static object? ExecuteScalar(string sql, params (string paramName, object paramValue)[] parameters)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);

            foreach (var (paramName, paramValue) in parameters)
            {
                cmd.Parameters.AddWithValue(paramName, paramValue);
            }

            return cmd.ExecuteScalar();
        }

        /// <summary>
        /// Auslesen mehrerer Zeilen.
        /// Du übergibst einen Mapper, der aus dem IDataReader das gewünschte Objekt baut.
        /// </summary>
        public static List<T> ExecuteReader<T>(
            string sql,
            Func<IDataReader, T> mapFunction,
            params (string paramName, object paramValue)[] parameters
        )
        {
            var result = new List<T>();

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);

            foreach (var (paramName, paramValue) in parameters)
            {
                cmd.Parameters.AddWithValue(paramName, paramValue);
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(mapFunction(reader));
            }

            return result;
        }
        public static void SeedCards()
        {
            // Beispielkarten werden erstellt
            var cards = new List<Card>
            {
                new SpellCard { Name = "Fireball", Type = "spell", Damage = 50, Element = "fire" },
                new SpellCard { Name = "WaterBlast", Type = "spell", Damage = 60, Element = "water" },
                new SpellCard { Name = "NormalStrike", Type = "spell", Damage = 30, Element = "normal" },
                new MonsterCard { Name = "EarthGiant", Type = "monster", Damage = 70, Element = "normal" },
                new MonsterCard { Name = "WaterGoblin", Type = "monster", Damage = 40, Element = "water" },
                new MonsterCard { Name = "FireDragon", Type = "monster", Damage = 80, Element = "fire" },
                new MonsterCard { Name = "NormalOrc", Type = "monster", Damage = 35, Element = "normal" },
            };

            foreach (var card in cards)
            {
                // Hier fügst du die Karten einem speziellen Benutzer hinzu, z.B. "system"
                CardRepository.AddCard("system", card);
            }
        }

    }
}
