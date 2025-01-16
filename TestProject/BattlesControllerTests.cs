using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using MonsterCardTradingGame.Controllers;
using MonsterCardTradingGame.Repositories;
using MonsterCardTradingGame.Models;

namespace MonsterCardTradingGame.Tests
{
    public class BattlesControllerTests : IDisposable
    {
        private readonly string _testUsername = "testuser_battle_" + Guid.NewGuid().ToString("N");
        private readonly string _testPassword = "TestPassword123";
        private string? _token;

        private Guid? _createdBattleId; // für Battle-Tests
        private readonly List<int> _createdCardIds = new List<int>();

        public BattlesControllerTests()
        {
            // 1) Testbenutzer registrieren
            var registerDto = new RegisterUserDto
            {
                Username = _testUsername,
                Password = _testPassword
            };
            var registerBody = JsonSerializer.Serialize(registerDto);
            var registerRequest = new HttpRequest
            {
                Method = "POST",
                Path = "/users",
                Body = registerBody
            };
            var registerResponse = UsersController.Handle(registerRequest);
            Assert.Equal(201, registerResponse.StatusCode);

            // 2) Login: Token abholen
            var loginDto = new LoginUserDto
            {
                Username = _testUsername,
                Password = _testPassword
            };
            var loginBody = JsonSerializer.Serialize(loginDto);
            var loginRequest = new HttpRequest
            {
                Method = "POST",
                Path = "/sessions",
                Body = loginBody
            };
            var loginResponse = SessionsController.Handle(loginRequest);
            Assert.Equal(200, loginResponse.StatusCode);

            var loginData = JsonSerializer.Deserialize<LoginResponseDto>(loginResponse.Body);
            Assert.NotNull(loginData);
            _token = loginData!.Token;

            // 3) Vier Karten anlegen (damit wir ein gültiges Deck bauen können)
            CreateTestCard("FireSpell", "spell", 40, "fire");
            CreateTestCard("WaterGoblin", "monster", 30, "water");
            CreateTestCard("NormalOrk", "monster", 35, "normal");
            CreateTestCard("FireDragon", "monster", 50, "fire");

            // 4) Deck anlegen
            var createDeckDto = new
            {
                CardIds = _createdCardIds
            };
            var deckBody = JsonSerializer.Serialize(createDeckDto);
            var deckRequest = new HttpRequest
            {
                Method = "POST",
                Path = "/decks",
                Body = deckBody,
                Authorization = $"Bearer {_token}"
            };
            var deckResponse = DecksController.Handle(deckRequest);
            Assert.Equal(200, deckResponse.StatusCode);
        }

        /// <summary>
        /// Hilfsfunktion zum Erstellen einer Karte.
        /// </summary>
        private void CreateTestCard(string name, string type, int damage, string element)
        {
            var addCardDto = new CardsController.AddCardDto
            {
                Name = name,
                Type = type,
                Damage = damage,
                Element = element
            };
            var addCardBody = JsonSerializer.Serialize(addCardDto);
            var addCardRequest = new HttpRequest
            {
                Method = "POST",
                Path = "/cards",
                Body = addCardBody,
                Authorization = $"Bearer {_token}"
            };
            var addCardResponse = CardsController.Handle(addCardRequest);
            Assert.Equal(201, addCardResponse.StatusCode);

            // Karte in DB nachschlagen
            var cards = CardRepository.GetUserCards(_testUsername);
            var createdCard = cards.FirstOrDefault(c => c.Name == name && c.Type == type && c.Element == element);
            Assert.NotNull(createdCard);

            _createdCardIds.Add(createdCard.Id);
        }

        // --- TEST 1: StartBattle ohne Token => 401 Unauthorized ---
        [Fact]
        public void StartBattle_NoToken_Returns401()
        {
            // Arrange
            var battleRequestDto = new StartBattleRequestDto
            {
                Username = _testUsername
            };
            var body = JsonSerializer.Serialize(battleRequestDto);

            var request = new HttpRequest
            {
                Method = "POST",
                Path = "/battles",    // StartBattle
                Body = body
                // Keine Authorization!
            };

            // Act
            var response = BattlesController.Handle(request);

            // Assert
            Assert.Equal(401, response.StatusCode);
            Assert.Contains("Unauthorized", response.Body, StringComparison.OrdinalIgnoreCase);
        }
        

        // --- TEST 4: UsePowerUpInBattle ohne vorhandene PowerUps => 400 ---
        //    (Vorausgesetzt, du hast im Code, dass ein User erst ein PowerUp "claimen" muss)
        [Fact]
        public void UsePowerUpInBattle_NoPowerUps_Returns400()
        {
            // Arrange: Erstellen zunächst eines Battles (damit wir eine BattleId haben)
            var startBattleDto = new StartBattleRequestDto { Username = _testUsername };
            var startBody = JsonSerializer.Serialize(startBattleDto);
            var startRequest = new HttpRequest
            {
                Method = "POST",
                Path = "/battles",
                Body = startBody,
                Authorization = $"Bearer {_token}"
            };
            var startResp = BattlesController.Handle(startRequest);
            Assert.Equal(201, startResp.StatusCode);

            var battleResp = JsonSerializer.Deserialize<BattleCreatedResponse>(startResp.Body);
            Assert.NotNull(battleResp);
            _createdBattleId = battleResp!.Id;

            // Nun /battles/{id}/usepowerup aufrufen
            var request = new HttpRequest
            {
                Method = "POST",
                Path = $"/battles/{_createdBattleId}/usepowerup",
                Authorization = $"Bearer {_token}"
            };

            // Act
            var response = BattlesController.Handle(request);

            // Assert
            Assert.Equal(400, response.StatusCode);
            Assert.Contains("Keine ungenutzten PowerUps vorhanden", response.Body);
        }

        // --- TEST 5: BattleStatus für nicht existierende BattleID => 404 ---
        [Fact]
        public void GetBattleStatus_NonExistingBattle_Returns404()
        {
            // Arrange: irgendein random GUID, der nicht angelegt wurde
            Guid randomBattleId = Guid.NewGuid();
            var request = new HttpRequest
            {
                Method = "GET",
                Path = $"/battles/{randomBattleId}",
                Authorization = $"Bearer {_token}"
            };

            // Act
            var response = BattlesController.Handle(request);

            // Assert
            Assert.Equal(404, response.StatusCode);
            Assert.Contains("Battle nicht gefunden", response.Body);
        }

        // Hilfsklasse: zum Deserialisieren der StartBattle-Antwort
        private class BattleCreatedResponse
        {
            public Guid Id { get; set; }
            public string Status { get; set; } = string.Empty;
            public int PlayerHealth { get; set; }
            public int OpponentHealth { get; set; }
        }

        public void Dispose()
        {
            // Battle-Daten (falls du ein In-Memory-BattleRepository nutzt, evtl. nicht nötig)
            // Falls du in der DB für Battles was speicherst, könntest du es hier löschen.

            // PowerUps / Deck / Cards entfernen
            foreach (var cardId in _createdCardIds)
            {
                const string deleteCardSql = @"DELETE FROM cards WHERE id = @id";
                DatabaseManager.ExecuteNonQuery(deleteCardSql, ("id", cardId));
            }

            // User löschen
            const string deleteUserSql = @"DELETE FROM users WHERE username = @u";
            DatabaseManager.ExecuteNonQuery(deleteUserSql, ("u", _testUsername));
        }
    }
    
    // Hilfsklasse für das Login-Response DTO
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
    }
}
