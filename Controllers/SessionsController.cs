// SessionsController.cs
using System;
using System.Text.Json;
using MonsterCardTradingGame.Repositories;

namespace MonsterCardTradingGame.Controllers
{
    public static class SessionsController
    {
        public static HttpResponse Handle(HttpRequest request)
        {
            if (request.Method == "POST" && request.Path == "/sessions")
            {
                return LoginUser(request);
            }

            // Falls nichts passt, 400 Bad Request
            return new HttpResponse
            {
                StatusCode = 400,
                ContentType = "text/plain",
                Body = "Bad Request in SessionsController"
            };
        }

        private static HttpResponse LoginUser(HttpRequest request)
        {
            try
            {
                // Body auslesen und als JSON interpretieren
                // Erwartet: {"Username":"alice","Password":"secret"}
                var loginDto = JsonSerializer.Deserialize<LoginUserDto>(request.Body);
                if (loginDto == null || string.IsNullOrWhiteSpace(loginDto.Username) || string.IsNullOrWhiteSpace(loginDto.Password))
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Ungültige Anmeldedaten"
                    };
                }

                // Anmeldeinformationen validieren
                if (!UserRepository.ValidateCredentials(loginDto.Username, loginDto.Password))
                {
                    return new HttpResponse
                    {
                        StatusCode = 401,
                        ContentType = "text/plain",
                        Body = "Ungültiger Benutzername oder Passwort"
                    };
                }

                // Token generieren (z.B. GUID)
                string token = Guid.NewGuid().ToString();

                // Token in der Datenbank aktualisieren
                bool tokenUpdated = UserRepository.UpdateToken(loginDto.Username, token);
                if (!tokenUpdated)
                {
                    return new HttpResponse
                    {
                        StatusCode = 500,
                        ContentType = "text/plain",
                        Body = "Fehler beim Generieren des Tokens"
                    };
                }

                // Erfolg: Token zurückgeben
                var responseBody = JsonSerializer.Serialize(new { Token = token });

                return new HttpResponse
                {
                    StatusCode = 200,
                    ContentType = "application/json",
                    Body = responseBody
                };
            }
            catch (Exception ex)
            {
                // Fehler beim Verarbeiten der Anfrage
                return new HttpResponse
                {
                    StatusCode = 400,
                    ContentType = "text/plain",
                    Body = $"Fehler beim Verarbeiten der Anmeldeanfrage: {ex.Message}"
                };
            }
        }
    }

    // DTO für die Anmeldung
    public class LoginUserDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
