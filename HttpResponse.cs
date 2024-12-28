using System.IO;
using System.Text;

namespace MonsterCardTradingGame
{
    public class HttpResponse
    {
        public int StatusCode { get; set; } = 200;
        public string ContentType { get; set; } = "text/plain";
        public string Body { get; set; } = "";

        public void WriteToStream(Stream stream)
        {
            using var writer = new StreamWriter(stream, Encoding.UTF8, 8192, leaveOpen: true);
            writer.WriteLine($"HTTP/1.1 {StatusCode} {GetStatusMessage(StatusCode)}");
            writer.WriteLine($"Content-Type: {ContentType}");
            writer.WriteLine($"Content-Length: {Encoding.UTF8.GetByteCount(Body)}");
            writer.WriteLine();  // Leerzeile
            writer.Write(Body);
        }

        private string GetStatusMessage(int code)
        {
            return code switch
            {
                200 => "OK",
                201 => "Created",
                400 => "Bad Request",
                401 => "Unauthorized",
                404 => "Not Found",
                409 => "Conflict",
                500 => "Internal Server Error",
                _   => "Unknown"
            };
        }
    }
}
