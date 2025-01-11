using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MonsterCardTradingGame
{
    public static class HttpRequestParser
    {
        public static async Task<HttpRequest> ParseFromStreamAsync(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, false, 8192, leaveOpen: true);

            // 1) Erste Zeile => "GET /users HTTP/1.1"
            var requestLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(requestLine))
            {
                throw new Exception("Leere oder ungültige Request-Line.");
            }

            var tokens = requestLine.Split(' ');
            if (tokens.Length < 3)
            {
                throw new Exception("Zu wenige Token in der Request-Line.");
            }

            var method = tokens[0];          // "GET" / "POST" / ...
            var path = tokens[1];            // "/users" (oder "/something")
            var httpVersion = tokens[2];     // "HTTP/1.1"

            // 2) Header lesen, bis Leerzeile
            string? line;
            string? authorization = null;
            int contentLength = 0;
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
            {
                if (line.StartsWith("Authorization:", StringComparison.OrdinalIgnoreCase))
                {
                    authorization = line.Substring("Authorization:".Length).Trim();
                }
                else if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                {
                    var value = line.Substring("Content-Length:".Length).Trim();
                    int.TryParse(value, out contentLength);
                }
            }

            // 3) Body lesen (falls Content-Length > 0)
            string body = "";
            if (contentLength > 0)
            {
                var buffer = new char[contentLength];
                int read = await reader.ReadBlockAsync(buffer, 0, contentLength);
                body = new string(buffer, 0, read);
            }

            return new HttpRequest
            {
                Method = method,
                Path = path,
                Body = body,
                Authorization = authorization ?? string.Empty
            };
        }
    }
}
