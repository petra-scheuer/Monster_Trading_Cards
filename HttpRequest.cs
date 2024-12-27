namespace MonsterCardTradingGame
{
    public class HttpRequest
    {
        public string Method { get; set; }      // z.B. "GET"
        public string Path { get; set; }        // z.B. "/users"
        public string Body { get; set; }        // Raw Body-Daten
        public string Authorization { get; set; } // z.B. "Basic ...", "Bearer ...", etc
    }
}

