// HttpRequest.cs
namespace MonsterCardTradingGame
{
    //Repräsentiert Anfragen, stellt das Gründgerüst für die HTTP Anfrage
    public class HttpRequest
    {
        public string Method { get; set; } = string.Empty;         // z.B. "GET"
        public string Path { get; set; } = string.Empty;           // z.B. "/users"
        public string Body { get; set; } = string.Empty;           // Raw Body-Daten
        public string Authorization { get; set; } = string.Empty;  // z.B. "Basic ...", "Bearer ...", etc.
    }
}