namespace MonsterCardTradingGame
{
    public static class UsersController
    {
        public static HttpResponse Handle(HttpRequest request)
        {
            // Einfaches Beispiel:
            // Unterscheide z.B. GET /users vs. POST /users
            // Hier nur eine Dummy-Antwort:

            return new HttpResponse
            {
                StatusCode = 200,
                ContentType = "text/plain",
                Body = $"[UsersController] Du hast {request.Method} auf {request.Path} aufgerufen!"
            };
        }
    }
}
