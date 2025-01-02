// Controllers/ScoreboardController.cs
using System;
using System.Text.Json;
using System.Collections.Generic;
using MonsterCardTradingGame.Repositories;
using MonsterCardTradingGame.Models;

namespace MonsterCardTradingGame.Controllers
{
    public static class ScoreboardController
    {
        public static HttpResponse Handle(HttpRequest request)
        {
            if (request.Method == "GET" && request.Path == "/scoreboard")
            {
                return GetScoreboard();
            }

            return new HttpResponse
            {
                StatusCode = 400,
                ContentType = "text/plain",
                Body = "Bad Request in ScoreboardController"
            };
        }

        private static HttpResponse GetScoreboard()
        {
            try
            {
                var users = UserRepository.GetAllUsersOrderedByELO();
                var responseBody = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });

                return new HttpResponse
                {
                    StatusCode = 200,
                    ContentType = "application/json",
                    Body = responseBody
                };
            }
            catch (Exception ex)
            {
                return new HttpResponse
                {
                    StatusCode = 500,
                    ContentType = "text/plain",
                    Body = $"Error fetching scoreboard: {ex.Message}"
                };
            }
        }
    }
}