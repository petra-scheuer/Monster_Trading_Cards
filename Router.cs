using System;

namespace MonsterCardTradingGame
{
    public static class Router
    {
        public static HttpResponse Route(HttpRequest request)
        {
            // Hier entscheidest du, welche "Controller" oder welche Methode aufgerufen wird.
            try
            {
                // Beispiel-Routing nach Pfad:
                if (request.Path.StartsWith("/users"))
                {
                    return UsersController.Handle(request);
                }
                else if (request.Path.StartsWith("/sessions"))
                {
                    return SessionsController.Handle(request);
                }
                // usw. für /packages, /battles etc.

                // Standard-Antwort, falls nichts passt
                return new HttpResponse
                {
                    StatusCode = 404,
                    ContentType = "text/plain",
                    Body = "Not Found"
                };
            }
            catch (Exception ex)
            {
                // Hier könntest du z.B. 500 Internal Server Error zurückgeben
                return new HttpResponse
                {
                    StatusCode = 500,
                    ContentType = "text/plain",
                    Body = $"Server error: {ex.Message}"
                };
            }
        }
    }
}
