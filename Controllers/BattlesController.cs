// Controllers/BattlesController.cs
using System;
using System.Text.Json;
using MonsterCardTradingGame.Repositories;
using MonsterCardTradingGame.Models;
using MonsterCardTradingGame.Logic;

namespace MonsterCardTradingGame.Controllers
{
    public static class BattlesController
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

            if (request.Method == "POST" && request.Path == "/battles")
            {
                return InitiateBattle(username!, request);
            }

            return new HttpResponse
            {
                StatusCode = 400,
                ContentType = "text/plain",
                Body = "Bad Request in BattlesController"
            };
        }

        private static HttpResponse InitiateBattle(string username, HttpRequest request)
        {
            try
            {
                var battleDto = JsonSerializer.Deserialize<BattleRequestDto>(request.Body);
                if (battleDto == null || string.IsNullOrWhiteSpace(battleDto.OpponentUsername))
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Invalid battle request data."
                    };
                }

                // Überprüfen, ob der Gegner existiert
                var opponent = UserRepository.GetUser(battleDto.OpponentUsername);
                if (opponent == null)
                {
                    return new HttpResponse
                    {
                        StatusCode = 404,
                        ContentType = "text/plain",
                        Body = "Opponent user not found."
                    };
                }

                // Holen der Decks beider Spieler
                var playerDeck = DeckRepository.GetDeck(username);
                var opponentDeck = DeckRepository.GetDeck(battleDto.OpponentUsername);

                if (playerDeck == null || opponentDeck == null)
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Both players must have a defined deck to initiate a battle."
                    };
                }

                // Holen der Karten
                var playerCards = CardRepository.GetCardsByIds(playerDeck.CardIds);
                var opponentCards = CardRepository.GetCardsByIds(opponentDeck.CardIds);

                // Durchführen des Kampfes
                var battleResult = BattleLogic.PerformBattle(playerCards, opponentCards);

                // Erstellen des Kampfprotokolls
                string battleLog = battleResult.Log;

                // Aktualisieren der ELO-Werte
                if (battleResult.Winner != null)
                {
                    UserRepository.UpdateELO(battleResult.Winner, +3);
                    string loser = battleResult.Winner == username ? battleDto.OpponentUsername : username;
                    UserRepository.UpdateELO(loser, -5);
                }

                // Speichern des Kampfes
                var battle = new Battle
                {
                    Player1Username = username,
                    Player2Username = battleDto.OpponentUsername,
                    BattleLog = battleLog,
                    WinnerUsername = battleResult.Winner,
                    CreatedAt = DateTime.UtcNow
                };

                BattleRepository.AddBattle(battle);

                // Antwort mit Kampfprotokoll und Ergebnis
                var responseBody = JsonSerializer.Serialize(new
                {
                    BattleId = battle.Id,
                    Winner = battleResult.Winner,
                    BattleLog = battleLog
                });

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
                    Body = $"Error initiating battle: {ex.Message}"
                };
            }
        }

        // Helper method to check authentication
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

        // DTO für den Kampf-Request
        public class BattleRequestDto
        {
            public string OpponentUsername { get; set; } = string.Empty;
        }
    }
}
