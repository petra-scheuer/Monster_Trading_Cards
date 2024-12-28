// Router.cs
using System;

namespace MonsterCardTradingGame
{
    public static class Router
    {
        public static HttpResponse Route(HttpRequest request)
        {
            if (request.Path.StartsWith("/users"))
            {
                return UsersController.Handle(request);
            }
            else if (request.Path.StartsWith("/sessions"))
            {
                return SessionsController.Handle(request);
            }
            // Weitere Controller können hier hinzugefügt werden
            else
            {
                return new HttpResponse
                {
                    StatusCode = 404,
                    ContentType = "text/plain",
                    Body = "Nicht gefunden"
                };
            }
        }
    }
}