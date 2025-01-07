using System;
using System.Text.Json;
using MonsterCardTradingGame.Models;
using MonsterCardTradingGame.Repositories;
using MonsterCardTradingGame.Logic;

namespace MonsterCardTradingGame.Controllers
{
    public static class BattlesController
    {
        // Instanz unserer Battle-Logik
        private static readonly BattleLogic battleLogic = new BattleLogic();

        public static HttpResponse Handle(HttpRequest request)
        {
            if (request.Method == "POST" && request.Path == "/battles")
            {
                return StartBattle(request);
            }
            else if (request.Method == "POST" && request.Path.StartsWith("/battles/") && request.Path.EndsWith("/turn"))
            {
                return PerformBattleTurn(request);
            }
            else if (request.Method == "POST" && request.Path.StartsWith("/battles/") && request.Path.EndsWith("/finish"))
            {
                // NEU: Route für das komplette Durchspielen
                return PerformBattleToTheEnd(request);
            }
            else if (request.Method == "GET" && request.Path.StartsWith("/battles/"))
            {
                return GetBattleStatus(request);
            }

            // Falls keine der obigen Bedingungen greift -> Bad Request
            return new HttpResponse
            {
                StatusCode = 400,
                ContentType = "text/plain",
                Body = "Bad Request in BattlesController"
            };
        }

        private static HttpResponse StartBattle(HttpRequest request)
        {
            try
            {
                var battleDto = JsonSerializer.Deserialize<StartBattleDto>(request.Body);
                if (battleDto == null ||
                    string.IsNullOrWhiteSpace(battleDto.Username) ||
                    battleDto.CardIds == null ||
                    battleDto.CardIds.Count != 4)
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Ungültige Battle-Daten. Bitte stelle sicher, dass Username und genau 4 CardIds angegeben sind."
                    };
                }

                // Authentifizierung prüfen
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
                if (username == null || username != battleDto.Username)
                {
                    return new HttpResponse
                    {
                        StatusCode = 401,
                        ContentType = "text/plain",
                        Body = "Unauthorized: Ungültiges Token oder Username stimmt nicht überein."
                    };
                }

                // Battle starten
                var battle = battleLogic.StartBattle(battleDto.Username, battleDto.CardIds);

                return new HttpResponse
                {
                    StatusCode = 201,
                    ContentType = "application/json",
                    Body = JsonSerializer.Serialize(new
                    {
                        battle.Id,
                        battle.Status,
                        battle.PlayerHealth,
                        battle.OpponentHealth
                    })
                };
            }
            catch (Exception ex)
            {
                return new HttpResponse
                {
                    StatusCode = 500,
                    ContentType = "text/plain",
                    Body = $"Interner Serverfehler: {ex.Message}"
                };
            }
        }

        private static HttpResponse PerformBattleTurn(HttpRequest request)
        {
            try
            {
                var pathParts = request.Path.Split('/');
                if (pathParts.Length < 3 || !Guid.TryParse(pathParts[2], out Guid battleId))
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Ungültige Battle-ID."
                    };
                }

                var battle = battleLogic.PerformBattleTurn(battleId);

                return new HttpResponse
                {
                    StatusCode = 200,
                    ContentType = "application/json",
                    Body = JsonSerializer.Serialize(new
                    {
                        battle.Id,
                        battle.Status,
                        battle.PlayerHealth,
                        battle.OpponentHealth,
                        battle.Logs
                    })
                };
            }
            catch (Exception ex)
            {
                return new HttpResponse
                {
                    StatusCode = 500,
                    ContentType = "text/plain",
                    Body = $"Interner Serverfehler: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// NEU: Komplettes Ausspielen des Battles bis max. 100 Runden
        /// </summary>
        private static HttpResponse PerformBattleToTheEnd(HttpRequest request)
        {
            try
            {
                var pathParts = request.Path.Split('/');
                // Pfad erwartet: /battles/{id}/finish
                if (pathParts.Length < 3 || !Guid.TryParse(pathParts[2], out Guid battleId))
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Ungültige Battle-ID."
                    };
                }

                // Ganze Schlacht durchspielen
                var battle = battleLogic.PerformFullBattle(battleId);

                // Ergebnis als JSON zurückgeben
                return new HttpResponse
                {
                    StatusCode = 200,
                    ContentType = "application/json",
                    Body = JsonSerializer.Serialize(new
                    {
                        battle.Id,
                        battle.Status,
                        PlayerCards = battle.PlayerCardIds,
                        OpponentCards = battle.OpponentCardIds,
                        Logs = battle.Logs
                    })
                };
            }
            catch (Exception ex)
            {
                return new HttpResponse
                {
                    StatusCode = 500,
                    ContentType = "text/plain",
                    Body = $"Interner Serverfehler: {ex.Message}"
                };
            }
        }

        private static HttpResponse GetBattleStatus(HttpRequest request)
        {
            try
            {
                var pathParts = request.Path.Split('/');
                if (pathParts.Length < 3 || !Guid.TryParse(pathParts[2], out Guid battleId))
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Ungültige Battle-ID."
                    };
                }

                var battle = BattleRepository.GetBattle(battleId);
                if (battle == null)
                {
                    return new HttpResponse
                    {
                        StatusCode = 404,
                        ContentType = "text/plain",
                        Body = "Battle nicht gefunden."
                    };
                }

                return new HttpResponse
                {
                    StatusCode = 200,
                    ContentType = "application/json",
                    Body = JsonSerializer.Serialize(new
                    {
                        battle.Id,
                        battle.Status,
                        battle.PlayerHealth,
                        battle.OpponentHealth,
                        battle.Logs
                    })
                };
            }
            catch (Exception ex)
            {
                return new HttpResponse
                {
                    StatusCode = 500,
                    ContentType = "text/plain",
                    Body = $"Interner Serverfehler: {ex.Message}"
                };
            }
        }

        private static string? GetUsernameByToken(string token)
        {
            return UserRepository.GetUsernameByToken(token);
        }
    }

    // DTO für den Start einer Battle
    public class StartBattleDto
    {
        public string Username { get; set; } = string.Empty;
        public List<int> CardIds { get; set; } = new List<int>();
    }
}
