// HttpResponse.cs
using System;
using System.Text;

namespace MonsterCardTradingGame
{
    public class HttpResponse
    {
        public int StatusCode { get; set; }
        public string ContentType { get; set; } = "text/plain";
        public string Body { get; set; } = "";

        public byte[] GetBytes()
        {
            string statusText = GetStatusText(StatusCode);
            string response = $"HTTP/1.1 {StatusCode} {statusText}\r\n" +
                              $"Content-Type: {ContentType}\r\n" +
                              $"Content-Length: {Encoding.UTF8.GetByteCount(Body)}\r\n" +
                              $"Connection: close\r\n" +
                              $"\r\n" +
                              $"{Body}";
            return Encoding.UTF8.GetBytes(response);
        }

        private string GetStatusText(int statusCode)
        {
            return statusCode switch
            {
                200 => "OK",
                201 => "Created",
                400 => "Bad Request",
                404 => "Not Found",
                409 => "Conflict",
                500 => "Internal Server Error",
                _ => "Unknown"
            };
        }
    }
}