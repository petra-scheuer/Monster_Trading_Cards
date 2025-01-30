// Router.cs
using System;
using MonsterCardTradingGame.Controllers; // Füge diese Zeile hinzu


namespace MonsterCardTradingGame
{
    // Router.cs
    public static class Router
    {
        /// <summary>
        /// Bestimmt basierend auf  request.Path welcher Controller zuständig ist 
        /// </summary>
        /// <param name="request"></param>
        /// <returns>gibt 404 zurück wenn kein passender pfad gefunden wird</returns>
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
            else if (request.Path.StartsWith("/cards"))
            {
                return CardsController.Handle(request);
            }
            else if (request.Path.StartsWith("/packages"))
            {
                return PackagesController.Handle(request);
            }
            else if (request.Path.StartsWith("/decks"))
            {
                return DecksController.Handle(request);
            }
            else if (request.Path.StartsWith("/battles"))
            {
                return BattlesController.Handle(request);
            }
            else if (request.Path.StartsWith("/scoreboard"))
            {
                return ScoreboardController.Handle(request);
            }
            else if (request.Path.StartsWith("/trades"))
            {
                return TradesController.Handle(request);
            }
            else if (request.Path.StartsWith("/powerups"))
            {
                return PowerUpsController.Handle(request);
            }

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