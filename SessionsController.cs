namespace MonsterCardTradingGame
{
    public static class SessionsController
    {
        public static HttpResponse Handle(HttpRequest request)
        {
            return new HttpResponse
            {
                StatusCode = 200,
                ContentType = "text/plain",
                Body = $"[SessionsController] Du hast {request.Method} auf {request.Path} aufgerufen!"
            };
        }
    }
}