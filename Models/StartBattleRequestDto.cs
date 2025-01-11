// Models/StartBattleRequestDto.cs
namespace MonsterCardTradingGame.Models
{
    public class StartBattleRequestDto
    {
        public string Username { get; set; } = string.Empty;
        // Optional: Falls man gegen jemanden anderen als "AI" kämpfen will,
        // könnte man hier z.B. OpponentUsername ergänzen.
    }
}