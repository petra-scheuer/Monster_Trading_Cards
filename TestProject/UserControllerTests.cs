// MonsterCardTradingGame.Tests/UsersControllerTests.cs
using System;
using System.Text.Json;
using Xunit;
using MonsterCardTradingGame.Controllers;
using MonsterCardTradingGame.Repositories;
using MonsterCardTradingGame.Models;

namespace MonsterCardTradingGame.Tests
{
    public class UsersControllerTests : IDisposable
    {
        private readonly string _testUsername = "testuser_" + Guid.NewGuid().ToString("N");
        private readonly string _testPassword = "TestPassword123";

        public UsersControllerTests()
        {
            // Optional: Initialisieren Sie eine separate Test-Datenbank oder setzen Sie Transaktionen ein
        }

        [Fact]
        public void RegisterUser_ValidData_Returns201()
        {
            // Arrange
            var registerDto = new RegisterUserDto
            {
                Username = _testUsername,
                Password = _testPassword
            };
            string requestBody = JsonSerializer.Serialize(registerDto);
            var request = new HttpRequest
            {
                Method = "POST",
                Path = "/users",
                Body = requestBody
            };

            // Act
            var response = UsersController.Handle(request);

            // Assert
            Assert.Equal(201, response.StatusCode);
            Assert.Equal("text/plain", response.ContentType);
            Assert.Contains($"Benutzer {_testUsername} erstellt.", response.Body);

            // Überprüfen, ob der Benutzer tatsächlich in der Datenbank erstellt wurde
            var user = UserRepository.GetUser(_testUsername);
            Assert.NotNull(user);
            Assert.Equal(_testUsername, user.Username);
            Assert.True(UserRepository.ValidateCredentials(_testUsername, _testPassword));
            Assert.Equal(20, user.Coins);
            Assert.Equal(100, user.ELO);
        }

        [Fact]
        public void RegisterUser_DuplicateUsername_Returns409()
        {
            // Arrange
            var registerDto = new RegisterUserDto
            {
                Username = _testUsername,
                Password = _testPassword
            };
            string requestBody = JsonSerializer.Serialize(registerDto);
            var request = new HttpRequest
            {
                Method = "POST",
                Path = "/users",
                Body = requestBody
            };

            // Erstes Registrieren
            var firstResponse = UsersController.Handle(request);
            Assert.Equal(201, firstResponse.StatusCode);

            // Zweites Registrieren mit demselben Benutzernamen
            var secondResponse = UsersController.Handle(request);

            // Assert
            Assert.Equal(409, secondResponse.StatusCode);
            Assert.Equal("text/plain", secondResponse.ContentType);
            Assert.Equal("Benutzer existiert bereits", secondResponse.Body);
        }

        [Fact]
        public void RegisterUser_InvalidData_Returns400()
        {
            // Arrange: Fehlende Username
            var registerDto = new RegisterUserDto
            {
                Username = "",
                Password = _testPassword
            };
            string requestBody = JsonSerializer.Serialize(registerDto);
            var request = new HttpRequest
            {
                Method = "POST",
                Path = "/users",
                Body = requestBody
            };

            // Act
            var response = UsersController.Handle(request);

            // Assert
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("text/plain", response.ContentType);
            Assert.Equal("Ungültige Benutzerdaten", response.Body);
        }

        [Fact]
        public void RegisterUser_NullDto_Returns400()
        {
            // Arrange: Null Body
            string requestBody = ""; // Leerer Body
            var request = new HttpRequest
            {
                Method = "POST",
                Path = "/users",
                Body = requestBody
            };

            // Act
            var response = UsersController.Handle(request);

            // Assert
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("text/plain", response.ContentType);
        }

        public void Dispose()
        {
            // Bereinigen: Entfernen des Testbenutzers aus der Datenbank
            const string deleteSql = @"DELETE FROM users WHERE username = @u";
            DatabaseManager.ExecuteNonQuery(deleteSql, ("u", _testUsername));
        }
    }
}
