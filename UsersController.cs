using System;
using System.Text.Json;  // Für JSON-Parsing, falls nötig
using System.Collections.Generic;

namespace MonsterCardTradingGame
{
    public static class UsersController
    {
        // Temporärer In-Memory-Speicher für unsere User (später: DB!)
        private static Dictionary<string, string> _users = new Dictionary<string, string>();

        public static HttpResponse Handle(HttpRequest request)
        {
            // Aufteilen nach (Methode, Pfad usw.).
            // Beispiel: POST /users -> User registrieren.
            if (request.Method == "POST" && request.Path == "/users")
            {
                return RegisterUser(request);
            }
            // GET /users -> Liste aller User ausgeben (nur Demo!)
            else if (request.Method == "GET" && request.Path == "/users")
            {
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
            // 1) Body auslesen und als JSON interpretieren
            // Beispiel erwartet: {"Username":"alice","Password":"secret"}
            try
            {
                var userDto = JsonSerializer.Deserialize<RegisterUserDto>(request.Body);
                if (userDto == null || string.IsNullOrWhiteSpace(userDto.Username))
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Invalid user data"
                    };
                }

                // 2) Prüfen, ob User schon existiert
                if (_users.ContainsKey(userDto.Username))
                {
                    return new HttpResponse
                    {
                        StatusCode = 409,
                        ContentType = "text/plain",
                        Body = "User already exists"
                    };
                }

                // 3) User eintragen (hier nur in unser Dictionary)
                _users[userDto.Username] = userDto.Password;

                // Erfolg
                return new HttpResponse
                {
                    StatusCode = 201,
                    ContentType = "text/plain",
                    Body = $"User {userDto.Username} created."
                };
            }
            catch (Exception ex)
            {
                // Body war evtl. kein gültiges JSON
                return new HttpResponse
                {
                    StatusCode = 400,
                    ContentType = "text/plain",
                    Body = $"Could not parse user data: {ex.Message}"
                };
            }
        }

        private static HttpResponse ListUsers()
        {
            // Einfaches Auflisten aller Nutzernamen
            // Nicht sehr datenschutzfreundlich, aber als Demo OK ;-)
            var allUsers = string.Join(", ", _users.Keys);

            return new HttpResponse
            {
                StatusCode = 200,
                ContentType = "text/plain",
                Body = $"Registered Users: {allUsers}"
            };
        }
    }

    // Damit wir was zum Deserialisieren haben:
    public class RegisterUserDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
