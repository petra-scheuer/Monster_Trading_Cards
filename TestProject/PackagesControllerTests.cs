using System;
using System.Text.Json;
using Xunit;
using MonsterCardTradingGame.Controllers;
using MonsterCardTradingGame.Repositories;
using MonsterCardTradingGame.Models;
using System.Collections.Generic;

namespace MonsterCardTradingGame.Tests
{
    public class PackagesControllerTests : IDisposable
    {
        private readonly string _testUsername = "testuser_packages_" + Guid.NewGuid().ToString("N");
        private readonly string _testPassword = "TestPassword123";
        private string? _token;
        private List<int> _createdCardIds = new List<int>();

        public PackagesControllerTests()
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
        /// Testet den erfolgreichen Kauf eines Pakets mit ausreichenden Münzen.
        /// </summary>
        [Fact]
        public void BuyPackage_WithSufficientCoins_Returns200()
        {
            // Arrange: Sicherstellen, dass der Benutzer genügend Münzen hat (Standard: 20)
            var user = UserRepository.GetUser(_testUsername);
            Assert.NotNull(user);
            Assert.True(user.Coins >= 5, "Der Benutzer sollte mindestens 5 Münzen haben.");

            // Erstellen der BuyPackage-Anfrage
            var buyPackageRequest = new HttpRequest
            {
                Method = "POST",
                Path = "/packages",
                Authorization = $"Bearer {_token}"
            };

            // Act
            var buyPackageResponse = PackagesController.Handle(buyPackageRequest);

            // Assert
            Assert.Equal(200, buyPackageResponse.StatusCode);
            Assert.Equal("text/plain", buyPackageResponse.ContentType);
            Assert.Equal("Paket erfolgreich gekauft und Karten hinzugefügt.", buyPackageResponse.Body);

            // Überprüfen, ob die Münzen abgezogen wurden
            var updatedUser = UserRepository.GetUser(_testUsername);
            Assert.NotNull(updatedUser);
            Assert.Equal(user.Coins - 5, updatedUser.Coins);

            // Überprüfen, ob neue Karten hinzugefügt wurden
            var userCards = CardRepository.GetUserCards(_testUsername);
            // Annahme: System-Karten sind nicht im Count enthalten oder initiale Karten sind minimal
            Assert.True(userCards.Count >= 1, "Es sollten mindestens 1 neue Karten hinzugefügt worden sein.");

            // Optional: Speichern der neuen Karten-IDs für die Bereinigung
            foreach (var card in userCards)
            {
                if (!_createdCardIds.Contains(card.Id) && card.Name == _testUsername)
                {
                    _createdCardIds.Add(card.Id);
                }
            }
        }

        /// <summary>
        /// Testet den Versuch, ein Paket mit unzureichenden Münzen zu kaufen.
        /// </summary>
        [Fact]
        public void BuyPackage_WithInsufficientCoins_Returns400()
        {
            // Arrange: Reduzieren der Münzen des Benutzers auf weniger als 5
            UserRepository.UpdateCoins(_testUsername, -16); // Standard: 20 - 16 = 4 Münzen

            var user = UserRepository.GetUser(_testUsername);
            Assert.NotNull(user);
            Assert.True(user.Coins < 5, "Der Benutzer sollte weniger als 5 Münzen haben.");

            // Erstellen der BuyPackage-Anfrage
            var buyPackageRequest = new HttpRequest
            {
                Method = "POST",
                Path = "/packages",
                Authorization = $"Bearer {_token}"
            };

            // Act
            var buyPackageResponse = PackagesController.Handle(buyPackageRequest);

            // Assert
            Assert.Equal(400, buyPackageResponse.StatusCode);
            Assert.Equal("text/plain", buyPackageResponse.ContentType);
            Assert.Equal("Nicht genügend Münzen zum Kaufen eines Pakets.", buyPackageResponse.Body);
        }

        public void Dispose()
        {
            // Bereinigen: Entfernen der erstellten Karten und des Testbenutzers
            foreach (var cardId in _createdCardIds)
            {
                const string deleteCardSql = @"DELETE FROM cards WHERE id = @id";
                DatabaseManager.ExecuteNonQuery(deleteCardSql, ("id", cardId));
            }

            // Löschen des Testbenutzers
            const string deleteUserSql = @"DELETE FROM users WHERE username = @u";
            DatabaseManager.ExecuteNonQuery(deleteUserSql, ("u", _testUsername));
        }
    }
}
