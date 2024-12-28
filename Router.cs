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
            // ggf. weitere Controller
            else
            {
                return new HttpResponse
                {
                    StatusCode = 404,
                    ContentType = "text/plain",
                    Body = "Not Found"
                };
            }
        }
    }
}
