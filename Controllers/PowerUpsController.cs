using System;
using System.Text.Json;
using MonsterCardTradingGame.Repositories;
using MonsterCardTradingGame.Models;

namespace MonsterCardTradingGame.Controllers
{
    public static class PowerUpsController
    {
        public static HttpResponse Handle(HttpRequest request)
        {
            if (!IsAuthenticated(request, out string? username))
            {
                return new HttpResponse
                {
                    StatusCode = 401,
                    ContentType = "text/plain",
                    Body = "Unauthorized: Invalid or missing token."
                };
            }

            if (request.Method == "POST" && request.Path == "/powerups/claim")
            {
                return ClaimPowerUp(username!);
            }
            else if (request.Method == "GET" && request.Path == "/powerups")
            {
                return ListPowerUps(username!);
            }

            return new HttpResponse
            {
                StatusCode = 400,
                ContentType = "text/plain",
                Body = "Bad Request in PowerUpsController"
            };
        }

        private static HttpResponse ClaimPowerUp(string username)
        {
            // Prüfen, ob der User bereits einen ungenutzten PowerUp hat
            var unused = PowerUpRepository.GetPowerUps(username, onlyUnused: true);
            if (unused.Count > 0)
            {
                return new HttpResponse
                {
                    StatusCode = 400,
                    ContentType = "text/plain",
                    Body = "Du hast bereits einen ungenutzten PowerUp."
                };
            }

            // Erzeuge einen neuen PowerUp in der DB
            PowerUpRepository.CreatePowerUp(username, "double_damage");

            return new HttpResponse
            {
                StatusCode = 201,
                ContentType = "text/plain",
                Body = "PowerUp erfolgreich erstellt (double_damage)."
            };
        }

        private static HttpResponse ListPowerUps(string username)
        {
            var all = PowerUpRepository.GetPowerUps(username);
            var json = JsonSerializer.Serialize(all);
            return new HttpResponse
            {
                StatusCode = 200,
                ContentType = "application/json",
                Body = json
            };
        }

        private static bool IsAuthenticated(HttpRequest request, out string? username)
        {
            username = null;
            var authHeader = request.Authorization;
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return false;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            username = UserRepository.GetUsernameByToken(token);
            return username != null;
        }
    }
}
