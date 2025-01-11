using System;
using System.Text.Json;
using Xunit;
using MonsterCardTradingGame.Controllers;
using MonsterCardTradingGame.Repositories;
using MonsterCardTradingGame.Models;
using System.Collections.Generic;

namespace MonsterCardTradingGame.Tests
{
    public class TradesControllerTests : IDisposable
    {
        private readonly string _testUsername = "testuser_trades_" + Guid.NewGuid().ToString("N");
        private readonly string _testPassword = "TestPassword123";
        private string? _token;
        private List<int> _createdTradeIds = new List<int>();
        private List<int> _createdCardIds = new List<int>();

        public TradesControllerTests()
        {
            // Registrieren und anmelden eines Testbenutzers
            var registerDto = new RegisterUserDto
            {
                Username = _testUsername,
                Password = _testPassword
            };
            string registerBody = JsonSerializer.Serialize(registerDto);
            var registerRequest = new HttpRequest
            {
                Method = "POST",
                Path = "/users",
                Body = registerBody
            };

            var registerResponse = UsersController.Handle(registerRequest);
            Assert.Equal(201, registerResponse.StatusCode);

            var loginDto = new LoginUserDto
            {
                Username = _testUsername,
                Password = _testPassword
            };
            string loginBody = JsonSerializer.Serialize(loginDto);
            var loginRequest = new HttpRequest
            {
                Method = "POST",
                Path = "/sessions",
                Body = loginBody
            };

            var loginResponse = SessionsController.Handle(loginRequest);
            Assert.Equal(200, loginResponse.StatusCode);

            var loginResponseData = JsonSerializer.Deserialize<LoginResponseDto>(loginResponse.Body);
            Assert.NotNull(loginResponseData);
            _token = loginResponseData.Token;

            // Erstellen von Testkarten, die für Trades verwendet werden
            CreateTestCard("Offered Monster", "monster", 50, "fire");
            CreateTestCard("Requested Spell", "spell", 30, "water");
        }

        private void CreateTestCard(string name, string type, int damage, string element)
        {
            var addCardDto = new CardsController.AddCardDto
            {
                Name = name,
                Type = type,
                Damage = damage,
                Element = element
            };
            string addCardBody = JsonSerializer.Serialize(addCardDto);
            var addCardRequest = new HttpRequest
            {
                Method = "POST",
                Path = "/cards",
                Body = addCardBody,
                Authorization = $"Bearer {_token}"
            };

            var addCardResponse = CardsController.Handle(addCardRequest);
            Assert.Equal(201, addCardResponse.StatusCode);

            var userCards = CardRepository.GetUserCards(_testUsername);
            var addedCard = userCards.Find(c => c.Name == name && c.Type == type && c.Damage == damage && c.Element == element);
            Assert.NotNull(addedCard);
            _createdCardIds.Add(addedCard.Id);
        }

        /// <summary>
        /// Testet das Erstellen eines Handelsangebots mit gültigen Daten.
        /// </summary>
        [Fact]
        public void CreateTrade_ValidData_Returns201()
        {
            // Arrange
            var createTradeDto = new TradesController.CreateTradeDto
            {
                OfferedCardId = _createdCardIds[0], // Offered Monster
                RequirementType = "spell",
                RequirementElement = "water",
                RequirementMinDamage = 25
            };
            string createTradeBody = JsonSerializer.Serialize(createTradeDto);
            var createTradeRequest = new HttpRequest
            {
                Method = "POST",
                Path = "/trades",
                Body = createTradeBody,
                Authorization = $"Bearer {_token}"
            };

            // Act
            var createTradeResponse = TradesController.Handle(createTradeRequest);

            // Assert
            Assert.Equal(201, createTradeResponse.StatusCode);
            Assert.Equal("text/plain", createTradeResponse.ContentType);
            Assert.Equal("Trade offer created successfully.", createTradeResponse.Body);

            // Optional: Überprüfen, ob das Handelsangebot tatsächlich in der Datenbank existiert
            var activeTrades = TradeRepository.GetActiveTrades();
            var createdTrade = activeTrades.Find(t => t.OfferedCardId == _createdCardIds[0]);
            Assert.NotNull(createdTrade);
            _createdTradeIds.Add(createdTrade.Id);
        }

        /// <summary>
        /// Testet das Erstellen eines Handelsangebots mit ungültigen Daten (fehlende RequiredType).
        /// </summary>
        [Fact]
        public void CreateTrade_InvalidData_Returns400()
        {
            // Arrange: Fehlende RequirementType
            var createTradeDto = new TradesController.CreateTradeDto
            {
                OfferedCardId = _createdCardIds[0], // Offered Monster
                RequirementType = "", // Ungültig
                RequirementElement = "water",
                RequirementMinDamage = 25
            };
            string createTradeBody = JsonSerializer.Serialize(createTradeDto);
            var createTradeRequest = new HttpRequest
            {
                Method = "POST",
                Path = "/trades",
                Body = createTradeBody,
                Authorization = $"Bearer {_token}"
            };

            // Act
            var createTradeResponse = TradesController.Handle(createTradeRequest);

            // Assert
            Assert.Equal(400, createTradeResponse.StatusCode);
            Assert.Equal("text/plain", createTradeResponse.ContentType);
            Assert.Equal("Invalid trade data. Ensure OfferedCardId and RequirementType are provided.", createTradeResponse.Body);
        }

        /// <summary>
        /// Testet das Erstellen eines Handelsangebots mit einem ungültigen RequirementType.
        /// </summary>
        [Fact]
        public void CreateTrade_InvalidRequirementType_Returns400()
        {
            // Arrange: Ungültiger RequirementType
            var createTradeDto = new TradesController.CreateTradeDto
            {
                OfferedCardId = _createdCardIds[0], // Offered Monster
                RequirementType = "invalid_type",
                RequirementElement = "water",
                RequirementMinDamage = 25
            };
            string createTradeBody = JsonSerializer.Serialize(createTradeDto);
            var createTradeRequest = new HttpRequest
            {
                Method = "POST",
                Path = "/trades",
                Body = createTradeBody,
                Authorization = $"Bearer {_token}"
            };

            // Act
            var createTradeResponse = TradesController.Handle(createTradeRequest);

            // Assert
            Assert.Equal(400, createTradeResponse.StatusCode);
            Assert.Equal("text/plain", createTradeResponse.ContentType);
            Assert.Equal("Invalid RequirementType. Must be 'spell' or 'monster'.", createTradeResponse.Body);
        }

        /// <summary>
        /// Testet das Erstellen eines Handelsangebots mit einer Karte, die nicht dem Benutzer gehört.
        /// </summary>
        [Fact]
        public void CreateTrade_OfferedCardNotOwned_Returns404()
        {
            // Arrange: Verwendung einer nicht existierenden Karten-ID
            var createTradeDto = new TradesController.CreateTradeDto
            {
                OfferedCardId = 999999, // Nicht existierende Karte
                RequirementType = "spell",
                RequirementElement = "water",
                RequirementMinDamage = 25
            };
            string createTradeBody = JsonSerializer.Serialize(createTradeDto);
            var createTradeRequest = new HttpRequest
            {
                Method = "POST",
                Path = "/trades",
                Body = createTradeBody,
                Authorization = $"Bearer {_token}"
            };

            // Act
            var createTradeResponse = TradesController.Handle(createTradeRequest);

            // Assert
            Assert.Equal(404, createTradeResponse.StatusCode);
            Assert.Equal("text/plain", createTradeResponse.ContentType);
            Assert.Equal("Offered card not found or does not belong to the user.", createTradeResponse.Body);
        }

        /// <summary>
        /// Testet das Erstellen eines Handelsangebots mit einer Karte, die bereits in einem aktiven Trade ist.
        /// </summary>
        [Fact]
        public void CreateTrade_OfferedCardAlreadyInTrade_Returns400()
        {
            // Arrange: Erstes Handelsangebot erstellen
            var createTradeDto1 = new TradesController.CreateTradeDto
            {
                OfferedCardId = _createdCardIds[0], // Offered Monster
                RequirementType = "spell",
                RequirementElement = "water",
                RequirementMinDamage = 25
            };
            string createTradeBody1 = JsonSerializer.Serialize(createTradeDto1);
            var createTradeRequest1 = new HttpRequest
            {
                Method = "POST",
                Path = "/trades",
                Body = createTradeBody1,
                Authorization = $"Bearer {_token}"
            };

            var createTradeResponse1 = TradesController.Handle(createTradeRequest1);
            Assert.Equal(201, createTradeResponse1.StatusCode);

            // Aktives Handelsangebot finden
            var activeTrades = TradeRepository.GetActiveTrades();
            var createdTrade = activeTrades.Find(t => t.OfferedCardId == _createdCardIds[0]);
            Assert.NotNull(createdTrade);
            _createdTradeIds.Add(createdTrade.Id);

            // Versuch, ein weiteres Handelsangebot mit derselben Karte zu erstellen
            var createTradeDto2 = new TradesController.CreateTradeDto
            {
                OfferedCardId = _createdCardIds[0], // Bereits in einem Trade
                RequirementType = "monster",
                RequirementElement = "fire",
                RequirementMinDamage = 30
            };
            string createTradeBody2 = JsonSerializer.Serialize(createTradeDto2);
            var createTradeRequest2 = new HttpRequest
            {
                Method = "POST",
                Path = "/trades",
                Body = createTradeBody2,
                Authorization = $"Bearer {_token}"
            };

            var createTradeResponse2 = TradesController.Handle(createTradeRequest2);

            // Assert
            Assert.Equal(400, createTradeResponse2.StatusCode);
            Assert.Equal("text/plain", createTradeResponse2.ContentType);
            Assert.Equal("The offered card is already in an active trade.", createTradeResponse2.Body);
        }

        /// <summary>
        /// Testet das Akzeptieren eines nicht existierenden Handelsangebots.
        /// </summary>
        [Fact]
        public void AcceptTrade_NonExistingTrade_Returns404()
        {
            // Arrange: Verwendung einer nicht existierenden Handels-ID
            int nonExistingTradeId = 999999;
            var acceptTradeDto = new TradesController.AcceptTradeDto
            {
                RequestedCardId = _createdCardIds[1] // "Requested Spell"
            };
            string acceptTradeBody = JsonSerializer.Serialize(acceptTradeDto);
            var acceptTradeRequest = new HttpRequest
            {
                Method = "POST",
                Path = $"/trades/{nonExistingTradeId}/accept",
                Body = acceptTradeBody,
                Authorization = $"Bearer {_token}"
            };

            // Act
            var acceptTradeResponse = TradesController.Handle(acceptTradeRequest);

            // Assert
            Assert.Equal(404, acceptTradeResponse.StatusCode);
            Assert.Equal("text/plain", acceptTradeResponse.ContentType);
            Assert.Equal("Trade not found or already completed.", acceptTradeResponse.Body);
        }

        /// <summary>
        /// Testet das Abrufen aller aktiven Handelsangebote.
        /// </summary>
        [Fact]
        public void GetActiveTrades_Returns200()
        {
            // Arrange: Sicherstellen, dass mindestens ein aktives Handelsangebot vorhanden ist
            var createTradeDto = new TradesController.CreateTradeDto
            {
                OfferedCardId = _createdCardIds[0], // Offered Monster
                RequirementType = "spell",
                RequirementElement = "water",
                RequirementMinDamage = 25
            };
            string createTradeBody = JsonSerializer.Serialize(createTradeDto);
            var createTradeRequest = new HttpRequest
            {
                Method = "POST",
                Path = "/trades",
                Body = createTradeBody,
                Authorization = $"Bearer {_token}"
            };

            var createTradeResponse = TradesController.Handle(createTradeRequest);
            Assert.Equal(201, createTradeResponse.StatusCode);

            var activeTradesBefore = TradeRepository.GetActiveTrades();
            Assert.NotEmpty(activeTradesBefore);
            var trade = activeTradesBefore.Find(t => t.OfferedCardId == _createdCardIds[0]);
            Assert.NotNull(trade);
            _createdTradeIds.Add(trade.Id);

            // Erstellen der Anfrage zum Abrufen aktiver Trades
            var getTradesRequest = new HttpRequest
            {
                Method = "GET",
                Path = "/trades",
                Authorization = $"Bearer {_token}"
            };

            // Act
            var getTradesResponse = TradesController.Handle(getTradesRequest);

            // Assert
            Assert.Equal(200, getTradesResponse.StatusCode);
            Assert.Equal("application/json", getTradesResponse.ContentType);

            var returnedTrades = JsonSerializer.Deserialize<List<Trade>>(getTradesResponse.Body);
            Assert.NotNull(returnedTrades);
            Assert.Contains(returnedTrades, t => t.Id == trade.Id);
        }

        /// <summary>
        /// Testet das Abrufen aktiver Handelsangebote ohne Authentifizierung.
        /// </summary>
        [Fact]
        public void GetActiveTrades_Unauthenticated_Returns401()
        {
            // Arrange: Anfrage ohne Authorization Header
            var getTradesRequest = new HttpRequest
            {
                Method = "GET",
                Path = "/trades"
            };

            // Act
            var getTradesResponse = TradesController.Handle(getTradesRequest);

            // Assert
            Assert.Equal(401, getTradesResponse.StatusCode);
            Assert.Equal("text/plain", getTradesResponse.ContentType);
            Assert.Equal("Unauthorized: Invalid or missing token.", getTradesResponse.Body);
        }

        /// <summary>
        /// Bereinigt nach den Tests: Entfernt erstellte Handelsangebote, Karten und den Testbenutzer.
        /// </summary>
        public void Dispose()
        {
            // Deaktivieren und Entfernen der erstellten Handelsangebote
            foreach (var tradeId in _createdTradeIds)
            {
                TradeRepository.DeactivateTrade(tradeId);
            }

            // Entfernen der erstellten Karten
            foreach (var cardId in _createdCardIds)
            {
                const string deleteCardSql = @"DELETE FROM cards WHERE id = @id";
                DatabaseManager.ExecuteNonQuery(deleteCardSql, ("id", cardId));
            }

            // Entfernen des Testbenutzers
            const string deleteUserSql = @"DELETE FROM users WHERE username = @u";
            DatabaseManager.ExecuteNonQuery(deleteUserSql, ("u", _testUsername));
        }
    }
}
