using System;
using System.Text.Json;
using Xunit;
using MonsterCardTradingGame.Controllers;
using MonsterCardTradingGame.Repositories;
using MonsterCardTradingGame.Models;
using System.Collections.Generic;

namespace MonsterCardTradingGame.Tests
{
    public class CardsControllerTests : IDisposable
    {
        private readonly string _testUsername = "testuser_cards_" + Guid.NewGuid().ToString("N");
        private readonly string _testPassword = "TestPassword123";
        private string? _token;
        private List<int> _createdCardIds = new List<int>();

        public CardsControllerTests()
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
        }

        /// <summary>
        /// Testet das Hinzufügen einer Karte mit gültigen Daten.
        /// </summary>
        [Fact]
        public void AddCard_ValidData_Returns201()
        {
            // Arrange
            var addCardDto = new CardsController.AddCardDto
            {
                Name = "Test Spell",
                Type = "spell",
                Damage = 40,
                Element = "fire"
            };
            string addCardBody = JsonSerializer.Serialize(addCardDto);
            var addCardRequest = new HttpRequest
            {
                Method = "POST",
                Path = "/cards",
                Body = addCardBody,
                Authorization = $"Bearer {_token}"
            };

            // Act
            var addCardResponse = CardsController.Handle(addCardRequest);

            // Assert
            Assert.Equal(201, addCardResponse.StatusCode);
            Assert.Equal("text/plain", addCardResponse.ContentType);
            Assert.Equal("Karte erfolgreich hinzugefügt.", addCardResponse.Body);

            // Optional: Überprüfen, ob die Karte tatsächlich in der Datenbank existiert
            var userCards = CardRepository.GetUserCards(_testUsername);
            var addedCard = userCards.Find(c => c.Name == "Test Spell" && c.Type == "spell" && c.Damage == 40 && c.Element == "fire");
            Assert.NotNull(addedCard);
            _createdCardIds.Add(addedCard.Id);
        }

        /// <summary>
        /// Testet das Hinzufügen einer Karte mit ungültigen Daten (fehlender Name).
        /// </summary>
        [Fact]
        public void AddCard_InvalidData_Returns400()
        {
            // Arrange: Fehlender Name
            var addCardDto = new CardsController.AddCardDto
            {
                Name = "",
                Type = "spell",
                Damage = 40,
                Element = "fire"
            };
            string addCardBody = JsonSerializer.Serialize(addCardDto);
            var addCardRequest = new HttpRequest
            {
                Method = "POST",
                Path = "/cards",
                Body = addCardBody,
                Authorization = $"Bearer {_token}"
            };

            // Act
            var addCardResponse = CardsController.Handle(addCardRequest);

            // Assert
            Assert.Equal(400, addCardResponse.StatusCode);
            Assert.Equal("text/plain", addCardResponse.ContentType);
            Assert.Equal("Ungültige Kartendaten.", addCardResponse.Body);
        }

        /// <summary>
        /// Testet das Löschen einer bestehenden Karte.
        /// </summary>
        [Fact]
        public void DeleteCard_ExistingCard_Returns200()
        {
            // Arrange: Zuerst eine Karte hinzufügen
            var addCardDto = new CardsController.AddCardDto
            {
                Name = "Test Monster",
                Type = "monster",
                Damage = 50,
                Element = "water"
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

            // Überprüfen, ob die Karte existiert
            var userCards = CardRepository.GetUserCards(_testUsername);
            var addedCard = userCards.Find(c => c.Name == "Test Monster" && c.Type == "monster" && c.Damage == 50 && c.Element == "water");
            Assert.NotNull(addedCard);
            _createdCardIds.Add(addedCard.Id);

            // Erstellen der Löschanfrage
            var deleteCardRequest = new HttpRequest
            {
                Method = "DELETE",
                Path = $"/cards/{addedCard.Id}",
                Authorization = $"Bearer {_token}"
            };

            // Act: Senden der Löschanfrage
            var deleteCardResponse = CardsController.Handle(deleteCardRequest);

            // Assert
            Assert.Equal(200, deleteCardResponse.StatusCode);
            Assert.Equal("text/plain", deleteCardResponse.ContentType);
            Assert.Equal("Karte erfolgreich entfernt.", deleteCardResponse.Body);

            // Überprüfen, ob die Karte tatsächlich gelöscht wurde
            var updatedUserCards = CardRepository.GetUserCards(_testUsername);
            var deletedCard = updatedUserCards.Find(c => c.Id == addedCard.Id);
            Assert.Null(deletedCard);
        }

        /// <summary>
        /// Testet das Löschen einer nicht existierenden oder fremden Karte.
        /// </summary>
        [Fact]
        public void DeleteCard_NonExistingOrForeignCard_Returns404()
        {
            // Arrange: Verwendung einer nicht existierenden Karten-ID
            int nonExistingCardId = 999999;
            var deleteCardRequest = new HttpRequest
            {
                Method = "DELETE",
                Path = $"/cards/{nonExistingCardId}",
                Authorization = $"Bearer {_token}"
            };

            // Act
            var deleteCardResponse = CardsController.Handle(deleteCardRequest);

            // Assert
            Assert.Equal(404, deleteCardResponse.StatusCode);
            Assert.Equal("text/plain", deleteCardResponse.ContentType);
            Assert.Equal("Karte nicht gefunden oder gehört nicht zum Benutzer.", deleteCardResponse.Body);
        }

        /// <summary>
        /// Testet das Abrufen der Benutzerkarten.
        /// </summary>
        [Fact]
        public void Dispose()
        {
            // Bereinigen: Entfernen der erstellten Karten und des Testbenutzers
            foreach (var cardId in _createdCardIds)
            {
                const string deleteCardSql = @"DELETE FROM cards WHERE id = @id";
                DatabaseManager.ExecuteNonQuery(deleteCardSql, ("id", cardId));
            }

            const string deleteUserSql = @"DELETE FROM users WHERE username = @u";
            DatabaseManager.ExecuteNonQuery(deleteUserSql, ("u", _testUsername));
        }
    }
}
