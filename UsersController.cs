// UsersController.cs
using System;
using System.Text.Json;
using System.Collections.Generic;

namespace MonsterCardTradingGame
{
    public static class UsersController
    {
        public static HttpResponse Handle(HttpRequest request)
        {
            if (request.Method == "POST" && request.Path == "/users")
            {
                return RegisterUser(request);
            }
            else if (request.Method == "GET" && request.Path == "/users")
            {
                // Überprüfen der Authentifizierung
                var authHeader = request.Authorization;
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return new HttpResponse
                    {
                        StatusCode = 401,
                        ContentType = "text/plain",
                        Body = "Unauthorized: Kein gültiges Token"
                    };
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                var username = GetUsernameByToken(token);
                if (username == null)
                {
                    return new HttpResponse
                    {
                        StatusCode = 401,
                        ContentType = "text/plain",
                        Body = "Unauthorized: Ungültiges Token"
                    };
                }

                // Token ist gültig, Benutzer ist authentifiziert
                return ListUsers();
            }

            // Falls nichts passt, 400 Bad Request
            return new HttpResponse
            {
                StatusCode = 400,
                ContentType = "text/plain",
                Body = "Bad Request in UsersController"
            };
        }

        private static HttpResponse RegisterUser(HttpRequest request)
        {
            try
            {
                var userDto = JsonSerializer.Deserialize<RegisterUserDto>(request.Body);
                if (userDto == null || string.IsNullOrWhiteSpace(userDto.Username) || string.IsNullOrWhiteSpace(userDto.Password))
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Ungültige Benutzerdaten"
                    };
                }

                if (UserRepository.GetUser(userDto.Username) != null)
                {
                    return new HttpResponse
                    {
                        StatusCode = 409,
                        ContentType = "text/plain",
                        Body = "Benutzer existiert bereits"
                    };
                }

                bool created = UserRepository.CreateUser(userDto.Username, userDto.Password);

                if (created)
                {
                    return new HttpResponse
                    {
                        StatusCode = 201,
                        ContentType = "text/plain",
                        Body = $"Benutzer {userDto.Username} erstellt."
                    };
                }
                else
                {
                    return new HttpResponse
                    {
                        StatusCode = 500,
                        ContentType = "text/plain",
                        Body = "Interner Serverfehler beim Erstellen des Benutzers."
                    };
                }
            }
            catch (Exception ex)
            {
                return new HttpResponse
                {
                    StatusCode = 400,
                    ContentType = "text/plain",
                    Body = $"Fehler beim Verarbeiten der Benutzerdaten: {ex.Message}"
                };
            }
        }

        private static HttpResponse ListUsers()
        {
            var usernames = UserRepository.GetAllUsernames();
            var allUsers = string.Join(", ", usernames);

            return new HttpResponse
            {
                StatusCode = 200,
                ContentType = "text/plain",
                Body = $"Registrierte Benutzer: {allUsers}"
            };
        }

        private static string? GetUsernameByToken(string token)
        {
            // Optimierte Methode: Direkte Datenbankabfrage nach Token
            return UserRepository.GetUsernameByToken(token);
        }
    }

    // DTO für die Registrierung
    public class RegisterUserDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
