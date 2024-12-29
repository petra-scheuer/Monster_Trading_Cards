using System;
using System.Text.Json;
using MonsterCardTradingGame.Repositories;


namespace MonsterCardTradingGame.Controllers
{
    public static class PackagesController
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

            if (request.Method == "POST" && request.Path == "/packages")
            {
                return BuyPackage(username!);
            }

            return new HttpResponse
            {
                StatusCode = 400,
                ContentType = "text/plain",
                Body = "Bad Request in PackagesController"
            };
        }

        private static bool DeductCoins(string username, int amount)
        {
            var user = UserRepository.GetUser(username);
            if (user == null || user.Coins < amount)
            {
                return false;
            }

            const string sql = @"UPDATE users SET coins = coins - @amount WHERE username = @u";
            DatabaseManager.ExecuteNonQuery(sql, ("amount", amount), ("u", username));
            return true;
        }

        private static HttpResponse BuyPackage(string username)
        {
            const int packageCost = 5;
            try
            {
                // Prüfen, ob der Benutzer genügend Münzen hat
                if (!DeductCoins(username, packageCost))
                {
                    return new HttpResponse
                    {
                        StatusCode = 400,
                        ContentType = "text/plain",
                        Body = "Nicht genügend Münzen zum Kaufen eines Pakets."
                    };
                }

                // Paket erstellen und Karten hinzufügen
                PackageRepository.CreatePackage(username);

                return new HttpResponse
                {
                    StatusCode = 200,
                    ContentType = "text/plain",
                    Body = "Paket erfolgreich gekauft und Karten hinzugefügt."
                };
            }
            catch (Exception ex)
            {
                return new HttpResponse
                {
                    StatusCode = 500,
                    ContentType = "text/plain",
                    Body = $"Fehler beim Kaufen des Pakets: {ex.Message}"
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
    }
}
