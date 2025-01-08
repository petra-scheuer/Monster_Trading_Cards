using System;
using System.Text.Json;
using MonsterCardTradingGame.Models;
using MonsterCardTradingGame.Repositories;
using MonsterCardTradingGame.Logic;

namespace MonsterCardTradingGame.Controllers
{
    public static class BattlesController
    {
        // Instanz der Battle-Logik
        private static readonly BattleLogic battleLogic = new BattleLogic();

        public static HttpResponse Handle(HttpRequest request)
        {
            if (request.Method == "POST" && request.Path == "/battles")
            {
                // Battle starten -> DECK wird verwendet
                return StartBattle(request);
            }
            else if (request.Method == "POST" && request.Path.StartsWith("/battles/") && request.Path.EndsWith("/turn"))
            {
                // Eine einzelne Runde (falls du das Feature noch nutzt)
                return PerformBattleTurn(request);
            }
            else if (request.Method == "POST" && request.Path.StartsWith("/battles/") && request.Path.EndsWith("/finish"))
            {
                // Ganzes Battle in bis zu 100 Runden durchspielen
                return PerformBattleToTheEnd(request);
            }
            else if (request.Method == "POST" && request.Path.StartsWith("/battles/") && request.Path.EndsWith("/usepowerup"))
            {
                // PowerUp in diesem Battle einsetzen
                return UsePowerUpInBattle(request);
            }
            else if (request.Method == "GET" && request.Path.StartsWith("/battles/"))
            {
                // Battle-Status ausgeben
                return GetBattleStatus(request);
            }

            // Falls nichts passt, 400 Bad Request
            return new HttpResponse
            {
                StatusCode = 400,
                ContentType = "text/plain",
                Body = "Bad Request in BattlesController"
            };
        }

        // ================================================================
        // 1) START BATTLE (Deck wird aus DB gelesen)
        // ================================================================
        private static HttpResponse StartBattle(HttpRequest request)
        {
            try
            {
                // Neuer DTO-Name: wir erwarten nur Username
                var battleRequest = JsonSerializer.Deserialize<StartBattleRequestDto>(request.Body);
                if (battleRequest == null || string.IsNullOrWhiteSpace(battleRequest.Username))
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Ungültige Daten. Bitte Username angeben."
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
                        Body = "Unauthorized: Kein gültiges Token."
                    };
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                var usernameFromToken = GetUsernameByToken(token);
                if (usernameFromToken == null || usernameFromToken != battleRequest.Username)
                {
                    return new HttpResponse
                    {
                        StatusCode = 401,
                        ContentType = "text/plain",
                        Body = "Unauthorized: Ungültiges Token oder Username stimmt nicht überein."
                    };
                }

                // Deck aus der DB laden
                var deck = DeckRepository.GetDeck(usernameFromToken);
                if (deck == null || deck.CardIds.Count != 4)
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Kein gültiges Deck vorhanden. Bitte zuerst ein Deck mit 4 Karten definieren."
                    };
                }

                // Battle starten (Karten kommen aus dem Deck)
                var battle = battleLogic.StartBattle(usernameFromToken, deck.CardIds);

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

        // ================================================================
        // 2) PERFORM BATTLE TURN (optional Feature)
        // ================================================================
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

        // ================================================================
        // 3) FINISH BATTLE (gesamtes Battle in einem Rutsch)
        // ================================================================
        private static HttpResponse PerformBattleToTheEnd(HttpRequest request)
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

        // ================================================================
        // 4) USE POWERUP (/battles/{id}/usepowerup)
        // ================================================================
        private static HttpResponse UsePowerUpInBattle(HttpRequest request)
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

                // Auth-Check
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
                        Body = "Unauthorized: Ungültiger Token"
                    };
                }

                // Hat user einen ungenutzten PowerUp?
                var unusedPowerUps = PowerUpRepository.GetPowerUps(username, onlyUnused: true);
                if (unusedPowerUps.Count == 0)
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Keine ungenutzten PowerUps vorhanden."
                    };
                }

                // Nimm den ersten ungenutzten
                var powerUp = unusedPowerUps[0];

                // Markiere ihn als benutzt
                PowerUpRepository.MarkPowerUpAsUsed(powerUp.Id);

                // Hole das Battle
                var battle = BattleRepository.GetBattle(battleId);
                if (battle == null || battle.Status != "In Progress")
                {
                    return new HttpResponse
                    {
                        StatusCode = 404,
                        ContentType = "text/plain",
                        Body = "Battle nicht gefunden oder bereits abgeschlossen."
                    };
                }

                // Setze im Battle ein Flag für "nächste Runde Double-Damage"
                battle.NextRoundPowerUpUser = username;
                BattleRepository.UpdateBattle(battle);

                return new HttpResponse
                {
                    StatusCode = 200,
                    ContentType = "text/plain",
                    Body = $"PowerUp '{powerUp.PowerUpType}' wird in der nächsten Runde genutzt."
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

        // ================================================================
        // 5) GET BATTLE STATUS
        // ================================================================
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
                        battle.Logs,
                        battle.NextRoundPowerUpUser
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

        // ================================================================
        // HILFSFUNKTION: USERNAME VIA TOKEN
        // ================================================================
        private static string? GetUsernameByToken(string token)
        {
            return UserRepository.GetUsernameByToken(token);
        }
    }

    // DTO (neu) für den Start einer Battle
    public class StartBattleRequestDto
    {
        public string Username { get; set; } = string.Empty;
        // Optional: Falls man gegen jemanden anderen als "AI" kämpfen will,
        // könnte man hier z.B. OpponentUsername ergänzen.
    }
}
