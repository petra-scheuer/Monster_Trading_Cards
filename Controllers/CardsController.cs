using System;
using System.Text.Json;
using System.Collections.Generic;
using MonsterCardTradingGame.Repositories;

namespace MonsterCardTradingGame.Controllers
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
                    Body = "Unauthorized: Ungültiges oder fehlendes Token."
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
            // Weitere Endpunkte (z.B. Karte aktualisieren) können hier hinzugefügt werden

            return new HttpResponse
            {
                StatusCode = 400,
                ContentType = "text/plain",
                Body = "Ungültige Anfrage im CardsController."
            };
        }

        // Hilfsmethode zur Überprüfung der Authentifizierung
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

        // Behandelt GET /cards
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

        // Behandelt POST /cards
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
                        Body = "Ungültige Kartendaten."
                    };
                }

                // Kartentyp validieren
                var cardType = cardDto.Type.ToLower();
                if (cardType != "spell" && cardType != "monster")
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Ungültiger Kartentyp. Muss 'spell' oder 'monster' sein."
                    };
                }

                // Elementtyp validieren
                var elementType = cardDto.Element.ToLower();
                if (elementType != "fire" && elementType != "water" && elementType != "normal")
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Ungültiger Elementtyp. Muss 'fire', 'water' oder 'normal' sein."
                    };
                }

                // Erstelle den entsprechenden Kartentyp
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
                    _ => throw new Exception("Ungültiger Kartentyp.")
                };

                // Füge die Karte dem Repository hinzu
                CardRepository.AddCard(username, newCard);

                return new HttpResponse
                {
                    StatusCode = 201,
                    ContentType = "text/plain",
                    Body = "Karte erfolgreich hinzugefügt."
                };
            }
            catch (Exception ex)
            {
                return new HttpResponse
                {
                    StatusCode = 400,
                    ContentType = "text/plain",
                    Body = $"Fehler beim Hinzufügen der Karte: {ex.Message}"
                };
            }
        }

        // Behandelt DELETE /cards/{id}
        private static HttpResponse DeleteCard(string username, HttpRequest request)
        {
            // Pfadformat: /cards/{id}
            var segments = request.Path.Split('/');
            if (segments.Length != 3 || !int.TryParse(segments[2], out int cardId))
            {
                return new HttpResponse
                {
                    StatusCode = 400,
                    ContentType = "text/plain",
                    Body = "Ungültige Karten-ID im Pfad."
                };
            }

            bool removed = CardRepository.RemoveCard(username, cardId);
            if (removed)
            {
                return new HttpResponse
                {
                    StatusCode = 200,
                    ContentType = "text/plain",
                    Body = "Karte erfolgreich entfernt."
                };
            }
            else
            {
                return new HttpResponse
                {
                    StatusCode = 404,
                    ContentType = "text/plain",
                    Body = "Karte nicht gefunden oder gehört nicht zum Benutzer."
                };
            }
        }

        // DTO zum Hinzufügen einer Karte
        public class AddCardDto
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty; // 'spell' oder 'monster'
            public int Damage { get; set; }
            public string Element { get; set; } = string.Empty; // 'fire', 'water', 'normal'
        }
    }
}
