using System;
using System.Text.Json;
using System.Collections.Generic;
using MonsterCardTradingGame.Models;
using MonsterCardTradingGame.Repositories;

namespace MonsterCardTradingGame.Controllers
{
    public static class DecksController
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

            if (request.Method == "POST" && request.Path == "/decks")
            {
                return CreateOrUpdateDeck(username!, request);
            }
            else if (request.Method == "GET" && request.Path == "/decks")
            {
                return GetDeck(username!);
            }

            return new HttpResponse
            {
                StatusCode = 400,
                ContentType = "text/plain",
                Body = "Bad Request in DecksController"
            };
        }

        private static HttpResponse CreateOrUpdateDeck(string username, HttpRequest request)
        {
            try
            {
                var deckDto = JsonSerializer.Deserialize<CreateDeckDto>(request.Body);
                if (deckDto == null || deckDto.CardIds == null || deckDto.CardIds.Count != 4)
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Ein Deck muss genau 4 Karten enthalten."
                    };
                }

                // Aktualisieren oder Erstellen des Decks
                DeckRepository.CreateOrUpdateDeck(username, deckDto.CardIds);

                return new HttpResponse
                {
                    StatusCode = 200,
                    ContentType = "text/plain",
                    Body = "Deck erfolgreich aktualisiert."
                };
            }
            catch (Exception ex)
            {
                return new HttpResponse
                {
                    StatusCode = 400,
                    ContentType = "text/plain",
                    Body = $"Fehler beim Erstellen des Decks: {ex.Message}"
                };
            }
        }

        private static HttpResponse GetDeck(string username)
        {
            try
            {
                var deck = DeckRepository.GetDeck(username);
                if (deck == null)
                {
                    return new HttpResponse
                    {
                        StatusCode = 404,
                        ContentType = "text/plain",
                        Body = "Deck nicht gefunden."
                    };
                }

                var responseBody = JsonSerializer.Serialize(deck);

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
                    Body = $"Fehler beim Abrufen des Decks: {ex.Message}"
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

        // DTO für das Erstellen eines Decks
        public class CreateDeckDto
        {
            public List<int> CardIds { get; set; } = new List<int>();
        }
    }
}
