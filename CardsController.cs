// CardsController.cs

using System;
using System.Text.Json;
using System.Collections.Generic;

namespace MonsterCardTradingGame
{
    public static class CardsController
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

            if (request.Method == "GET" && request.Path == "/cards")
            {
                return GetUserCards(username!);
            }
            else if (request.Method == "POST" && request.Path == "/cards")
            {
                return AddCard(username!, request);
            }
            else if (request.Method == "DELETE" && request.Path.StartsWith("/cards/"))
            {
                return DeleteCard(username!, request);
            }
            // Additional endpoints (e.g., update card) can be added here

            return new HttpResponse
            {
                StatusCode = 400,
                ContentType = "text/plain",
                Body = "Bad Request in CardsController"
            };
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

        // Handles GET /cards
        private static HttpResponse GetUserCards(string username)
        {
            var cards = CardRepository.GetUserCards(username);
            var responseBody = JsonSerializer.Serialize(cards);

            return new HttpResponse
            {
                StatusCode = 200,
                ContentType = "application/json",
                Body = responseBody
            };
        }

        // Handles POST /cards
        private static HttpResponse AddCard(string username, HttpRequest request)
        {
            try
            {
                var cardDto = JsonSerializer.Deserialize<AddCardDto>(request.Body);
                if (cardDto == null || string.IsNullOrWhiteSpace(cardDto.Name) ||
                    string.IsNullOrWhiteSpace(cardDto.Type) || string.IsNullOrWhiteSpace(cardDto.Element) ||
                    cardDto.Damage <= 0)
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Invalid card data."
                    };
                }

                // Validate card type
                var cardType = cardDto.Type.ToLower();
                if (cardType != "spell" && cardType != "monster")
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Invalid card type. Must be 'spell' or 'monster'."
                    };
                }

                // Validate element type
                var elementType = cardDto.Element.ToLower();
                if (elementType != "fire" && elementType != "water" && elementType != "normal")
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Invalid element type. Must be 'fire', 'water', or 'normal'."
                    };
                }

                // Create the appropriate card type
                Card newCard = cardType switch
                {
                    "spell" => new SpellCard
                    {
                        Name = cardDto.Name,
                        Type = "spell",
                        Damage = cardDto.Damage,
                        Element = elementType
                    },
                    "monster" => new MonsterCard
                    {
                        Name = cardDto.Name,
                        Type = "monster",
                        Damage = cardDto.Damage,
                        Element = elementType
                    },
                    _ => throw new Exception("Invalid card type.")
                };

                // Add the card to the repository
                CardRepository.AddCard(username, newCard);

                return new HttpResponse
                {
                    StatusCode = 201,
                    ContentType = "text/plain",
                    Body = "Card added successfully."
                };
            }
            catch (Exception ex)
            {
                return new HttpResponse
                {
                    StatusCode = 400,
                    ContentType = "text/plain",
                    Body = $"Error adding card: {ex.Message}"
                };
            }
        }

        // Handles DELETE /cards/{id}
        private static HttpResponse DeleteCard(string username, HttpRequest request)
        {
            // Path format: /cards/{id}
            var segments = request.Path.Split('/');
            if (segments.Length != 3 || !int.TryParse(segments[2], out int cardId))
            {
                return new HttpResponse
                {
                    StatusCode = 400,
                    ContentType = "text/plain",
                    Body = "Invalid card ID in path."
                };
            }

            bool removed = CardRepository.RemoveCard(username, cardId);
            if (removed)
            {
                return new HttpResponse
                {
                    StatusCode = 200,
                    ContentType = "text/plain",
                    Body = "Card removed successfully."
                };
            }
            else
            {
                return new HttpResponse
                {
                    StatusCode = 404,
                    ContentType = "text/plain",
                    Body = "Card not found or does not belong to the user."
                };
            }
        }

        // DTO for adding a card
        public class AddCardDto
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty; // 'spell' or 'monster'
            public int Damage { get; set; }
            public string Element { get; set; } = string.Empty; // 'fire', 'water', 'normal'
        }
    }
}
