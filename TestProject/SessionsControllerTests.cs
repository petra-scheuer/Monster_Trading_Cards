using System;
using System.Text.Json;
using Xunit;
using MonsterCardTradingGame.Controllers;
using MonsterCardTradingGame.Repositories;
using MonsterCardTradingGame.Models;

namespace MonsterCardTradingGame.Tests
{
    public class SessionsControllerTests : IDisposable
    {
        private readonly string _testUsername = "testuser_sessions_" + Guid.NewGuid().ToString("N");
        private readonly string _testPassword = "TestPassword123";

        public SessionsControllerTests()
        {
            // Optional: Initialisieren Sie eine separate Test-Datenbank oder verwenden Sie Transaktionen
        }

        /// <summary>
        /// Testet die erfolgreiche Anmeldung mit gültigen Anmeldeinformationen.
        /// </summary>
        [Fact]
        public void LoginUser_ValidCredentials_Returns200AndToken()
        {
            // Arrange: Registrieren eines Testbenutzers
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

            // Erstellen des Login-Datenobjekts
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

            // Act: Senden der Anmeldeanfrage
            var loginResponse = SessionsController.Handle(loginRequest);

            // Assert: Überprüfen der Antwort
            Assert.Equal(200, loginResponse.StatusCode);
            Assert.Equal("application/json", loginResponse.ContentType);

            var loginResponseData = JsonSerializer.Deserialize<LoginResponseDto>(loginResponse.Body);
            Assert.NotNull(loginResponseData);
            Assert.False(string.IsNullOrWhiteSpace(loginResponseData.Token));
        }

        /// <summary>
        /// Testet die Anmeldung mit ungültigen Anmeldeinformationen (falsches Passwort).
        /// </summary>
        [Fact]
        public void LoginUser_InvalidCredentials_Returns401()
        {
            // Arrange: Registrieren eines Testbenutzers
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

            // Erstellen des Login-Datenobjekts mit falschem Passwort
            var loginDto = new LoginUserDto
            {
                Username = _testUsername,
                Password = "WrongPassword"
            };
            string loginBody = JsonSerializer.Serialize(loginDto);
            var loginRequest = new HttpRequest
            {
                Method = "POST",
                Path = "/sessions",
                Body = loginBody
            };

            // Act: Senden der Anmeldeanfrage
            var loginResponse = SessionsController.Handle(loginRequest);

            // Assert: Überprüfen der Antwort
            Assert.Equal(401, loginResponse.StatusCode);
            Assert.Equal("text/plain", loginResponse.ContentType);
            Assert.Equal("Ungültiger Benutzername oder Passwort", loginResponse.Body);
        }

        /// <summary>
        /// Testet die Anmeldung mit fehlenden Feldern (leeres Passwort).
        /// </summary>
        [Fact]
        public void LoginUser_MissingFields_Returns400()
        {
            // Arrange: Registrieren eines Testbenutzers
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

            // Erstellen des Login-Datenobjekts mit fehlendem Passwort
            var loginDto = new LoginUserDto
            {
                Username = _testUsername,
                Password = "" // Leeres Passwort
            };
            string loginBody = JsonSerializer.Serialize(loginDto);
            var loginRequest = new HttpRequest
            {
                Method = "POST",
                Path = "/sessions",
                Body = loginBody
            };

            // Act: Senden der Anmeldeanfrage
            var loginResponse = SessionsController.Handle(loginRequest);

            // Assert: Überprüfen der Antwort
            Assert.Equal(400, loginResponse.StatusCode);
            Assert.Equal("text/plain", loginResponse.ContentType);
            Assert.Equal("Ungültige Anmeldedaten", loginResponse.Body);
        }

        public void Dispose()
        {
            // Bereinigen: Entfernen des Testbenutzers aus der Datenbank
            const string deleteSql = @"DELETE FROM users WHERE username = @u";
            DatabaseManager.ExecuteNonQuery(deleteSql, ("u", _testUsername));
        }
    }
}
