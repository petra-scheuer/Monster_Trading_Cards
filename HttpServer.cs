using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MonsterCardTradingGame
{
    public class HttpServer
    {
        private readonly int _port;
        private TcpListener _listener = null!;
        private bool _isRunning;

        public HttpServer(int port)
        {
            _port = port;
        }

        public async void Start()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _isRunning = true;

            Console.WriteLine($"[HttpServer] Server running on port {_port} ...");

            // Asynchrone Akzeptierung von Clients
            while (_isRunning)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine("[HttpServer] Client connected!");

                    // Asynchrone Verarbeitung des Clients
                    _ = HandleClientAsync(client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HttpServer] Error accepting client: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();
            Console.WriteLine("[HttpServer] Server stopped.");
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                try
                {
                    // Request parsen
                    var request = await HttpRequestParser.ParseFromStreamAsync(stream);
                    Console.WriteLine($"[HttpServer] Request => Method: {request.Method}, Path: {request.Path}");

                    // Router aufrufen
                    var response = Router.Route(request);

                    // Antwort in den Stream schreiben
                    var responseBytes = response.GetBytes();
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    Console.WriteLine($"[HttpServer] Response sent with StatusCode: {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HttpServer] Error parsing HTTP: {ex.Message}");

                    // Fehler-Antwort: Hier sehr minimal gehalten
                    using var writer = new StreamWriter(stream)
                    {
                        NewLine = "\r\n"  // erzwingt CRLF bei WriteLine
                    };

                    // Statuszeile
                    await writer.WriteLineAsync("HTTP/1.1 400 Bad Request");
                    // Header
                    await writer.WriteLineAsync("Content-Type: text/plain");
                    // Ende der Header
                    await writer.WriteLineAsync();

                    // Body
                    await writer.WriteLineAsync($"Error: {ex.Message}");

                    await writer.FlushAsync();
                }
            }
        }
    }
}
