using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MonsterCardTradingGame
{
    
    public static class HttpRequestParser
    {
        /// <summary>
        /// liest die eingehende Anfrage aus dem Stream und erstellt ein HTTP Request Objekt
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>HttpRequest-Objekt</returns>

        public static async Task<HttpRequest> ParseFromStreamAsync(Stream stream)
        {
            //reader wird erzeugt und es wird dafür gesorgt, dass er standartmäßig nahc dem lesen nicht geschlossen wird
            using var reader = new StreamReader(stream, Encoding.UTF8, false, 8192, leaveOpen: true);

            // 1) Danach wird die erste Zeile eingelesen - sie enthält die Methode (z.b GET), den Pfad (z.B: /users), und die HTTP Version (z.b HTTP/1.1)
            var requestLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(requestLine))
            {
                throw new Exception("Leere oder ungültige Request-Line.");
            }
            
            //dann werden die Zeilen in Tokens aufgetelt
            var tokens = requestLine.Split(' ');
            if (tokens.Length < 3)
            {
                throw new Exception("Zu wenige Token in der Request-Line.");
            }

            var method = tokens[0];          // "GET" / "POST" / ...
            var path = tokens[1];            // "/users" (oder "/something")
            var httpVersion = tokens[2];     // "HTTP/1.1"

            // 2) Die Header Zeilen werden nacheinander gelesen bis ""
            string? line;
            string? authorization = null; 
            int contentLength = 0;
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
            {
                if (line.StartsWith("Authorization:", StringComparison.OrdinalIgnoreCase))
                {
                    authorization = line.Substring("Authorization:".Length).Trim(); //Wert des Authorzation Headers wird gespeichert
                }
                else if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                {
                    var value = line.Substring("Content-Length:".Length).Trim(); // Länge des HTTP Body in Zeicen wird gespeichert
                    int.TryParse(value, out contentLength);
                }
            }

            // Wenn Content Length >0 , Body lesen
            string body = "";
            if (contentLength > 0)
                //Body wird in einem Puffer gelesen und in einen String umgewandelt
            {
                var buffer = new char[contentLength];
                int read = await reader.ReadBlockAsync(buffer, 0, contentLength);
                body = new string(buffer, 0, read);
            }
            //Objekt wird mit den Ausgelesenen daten befüllt. 
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
