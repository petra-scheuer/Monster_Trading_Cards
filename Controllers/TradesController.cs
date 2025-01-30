using System;
using System.Text.Json;
using MonsterCardTradingGame.Models;
using MonsterCardTradingGame.Repositories;

namespace MonsterCardTradingGame.Controllers
{
    public static class TradesController
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

            if (request.Method == "POST" && request.Path == "/trades")
            {
                return CreateTrade(username!, request);
            }
            else if (request.Method == "POST" && request.Path.StartsWith("/trades/") && request.Path.EndsWith("/accept"))
            {
                return AcceptTrade(username!, request);
            }
            else if (request.Method == "GET" && request.Path == "/trades")
            {
                return GetActiveTrades();
            }

            return new HttpResponse
            {
                StatusCode = 400,
                ContentType = "text/plain",
                Body = "Bad Request in TradesController"
            };
        }

        
        // Überprüft die Authentifizierung und gibt den Benutzernamen zurück.
        
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

        //Erstellt ein neues Handelsangebot.
        private static HttpResponse CreateTrade(string username, HttpRequest request)
        {
            try
            {
                var tradeDto = JsonSerializer.Deserialize<CreateTradeDto>(request.Body); //JSON objekt erstellen
                if (tradeDto == null || tradeDto.OfferedCardId <= 0 ||
                    string.IsNullOrWhiteSpace(tradeDto.RequirementType))
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Invalid trade data. Ensure OfferedCardId and RequirementType are provided."
                    };
                }

                // Validierung der RequirementType
                var reqType = tradeDto.RequirementType.ToLower();
                if (reqType != "spell" && reqType != "monster")
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Invalid RequirementType. Must be 'spell' or 'monster'."
                    };
                }

                // Überprüfen, ob die angebotene Karte dem Benutzer gehört und nicht im Deck ist
                var userCards = CardRepository.GetUserCards(username);
                var offeredCard = userCards.FirstOrDefault(c => c.Id == tradeDto.OfferedCardId);
                if (offeredCard == null)
                {
                    return new HttpResponse
                    {
                        StatusCode = 404,
                        ContentType = "text/plain",
                        Body = "Offered card not found or does not belong to the user."
                    };
                }

                // Überprüfen, ob die Karte im Deck ist
                var deck = DeckRepository.GetDeck(username);
                if (deck != null && deck.CardIds.Contains(offeredCard.Id))
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Cannot trade a card that is currently in the deck."
                    };
                }

                // Überprüfen, ob die Karte bereits in einem aktiven Handelsangebot ist
                var activeTrades = TradeRepository.GetActiveTrades();
                if (activeTrades.Any(t => t.OfferedCardId == offeredCard.Id))
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "The offered card is already in an active trade."
                    };
                }

                // Erstellen des Handelsangebots
                TradeRepository.CreateTrade(username, offeredCard.Id, reqType, tradeDto.RequirementElement, tradeDto.RequirementMinDamage);

                return new HttpResponse
                {
                    StatusCode = 201,
                    ContentType = "text/plain",
                    Body = "Trade offer created successfully."
                };
            }
            catch (Exception ex)
            {
                return new HttpResponse
                {
                    StatusCode = 400,
                    ContentType = "text/plain",
                    Body = $"Error creating trade offer: {ex.Message}"
                };
            }
        }

        // Nimmt ein bestehendes Handelsangebot an.
        private static HttpResponse AcceptTrade(string username, HttpRequest request)
        {
            try
            {
                // Pfad: /trades/{id}/accept
                var pathParts = request.Path.Split('/');
                if (pathParts.Length != 4 || !int.TryParse(pathParts[2], out int tradeId))
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Invalid trade ID in path."
                    };
                }

                var trade = TradeRepository.GetTradeById(tradeId);
                if (trade == null)
                {
                    return new HttpResponse
                    {
                        StatusCode = 404,
                        ContentType = "text/plain",
                        Body = "Trade not found or already completed."
                    };
                }

                // Body sollte die angebotene Karte des annehmenden Benutzers enthalten
                var acceptTradeDto = JsonSerializer.Deserialize<AcceptTradeDto>(request.Body);
                if (acceptTradeDto == null || acceptTradeDto.RequestedCardId <= 0)
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Invalid trade acceptance data. Ensure RequestedCardId is provided."
                    };
                }

                // Überprüfen, ob die angeforderte Karte dem annehmenden Benutzer gehört und nicht im Deck ist
                var userCards = CardRepository.GetUserCards(username);
                var requestedCard = userCards.FirstOrDefault(c => c.Id == acceptTradeDto.RequestedCardId);
                if (requestedCard == null)
                {
                    return new HttpResponse
                    {
                        StatusCode = 404,
                        ContentType = "text/plain",
                        Body = "Requested card not found or does not belong to the user."
                    };
                }

                // Überprüfen, ob die Karte im Deck ist
                var userDeck = DeckRepository.GetDeck(username);
                if (userDeck != null && userDeck.CardIds.Contains(requestedCard.Id))
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Cannot trade a card that is currently in the deck."
                    };
                }

                // Überprüfen, ob die angeforderte Karte die Anforderungen des Handelsangebots erfüllt
                var reqType = trade.RequirementType.ToLower();
                if (reqType != requestedCard.Type.ToLower())
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = $"Requested card type does not match the trade requirement: {trade.RequirementType} required."
                    };
                }

                if (!string.IsNullOrWhiteSpace(trade.RequirementElement))
                {
                    if (trade.RequirementElement.ToLower() != requestedCard.Element.ToLower())
                    {
                        return new HttpResponse
                        {
                            StatusCode = 400,
                            ContentType = "text/plain",
                            Body = $"Requested card element does not match the trade requirement: {trade.RequirementElement} required."
                        };
                    }
                }

                if (trade.RequirementMinDamage.HasValue)
                {
                    if (requestedCard.Damage < trade.RequirementMinDamage.Value)
                    {
                        return new HttpResponse
                        {
                            StatusCode = 400,
                            ContentType = "text/plain",
                            Body = $"Requested card does not meet the minimum damage requirement: {trade.RequirementMinDamage} required."
                        };
                    }
                }

                // Durchführung des Handels
                // 1. Aktualisiere die Eigentümerschaft der Karten
                bool transferOfferedCard = CardRepository.TransferCardOwnership(trade.OfferedCardId, username);
                if (!transferOfferedCard)
                {
                    return new HttpResponse
                    {
                        StatusCode = 500,
                        ContentType = "text/plain",
                        Body = "Failed to transfer the offered card."
                    };
                }

                bool transferRequestedCard = CardRepository.TransferCardOwnership(acceptTradeDto.RequestedCardId, trade.OfferedByUsername);
                if (!transferRequestedCard)
                {
                    // Rollback: Rückgängigmachen der ersten Übertragung
                    CardRepository.TransferCardOwnership(trade.OfferedCardId, trade.OfferedByUsername);
                    return new HttpResponse
                    {
                        StatusCode = 500,
                        ContentType = "text/plain",
                        Body = "Failed to transfer the requested card."
                    };
                }

                // 2. Deaktiviere das Handelsangebot
                TradeRepository.DeactivateTrade(tradeId);

                return new HttpResponse
                {
                    StatusCode = 200,
                    ContentType = "text/plain",
                    Body = "Trade accepted and completed successfully."
                };
            }
            catch (Exception ex)
            {
                return new HttpResponse
                {
                    StatusCode = 400,
                    ContentType = "text/plain",
                    Body = $"Error accepting trade offer: {ex.Message}"
                };
            }
        }

        // Holt alle aktiven Handelsangebote.
        private static HttpResponse GetActiveTrades()
        {
            try
            {
                var activeTrades = TradeRepository.GetActiveTrades();
                var responseBody = JsonSerializer.Serialize(activeTrades);

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
                    Body = $"Error fetching active trades: {ex.Message}"
                };
            }
        }

        // DTOs

        public class CreateTradeDto
        {
            public int OfferedCardId { get; set; }
            public string RequirementType { get; set; } = string.Empty; // 'spell' oder 'monster'
            public string? RequirementElement { get; set; } // Optional: 'fire', 'water', 'normal'
            public int? RequirementMinDamage { get; set; } // Optional: Mindestschaden
        }

        public class AcceptTradeDto
        {
            public int RequestedCardId { get; set; }
        }
    }
}
