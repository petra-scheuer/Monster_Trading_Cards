using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MonsterCardTradingGame
{
    public class HttpServer
    {
        private readonly int _port;
        private TcpListener _listener;
        private bool _isRunning;

        public HttpServer(int port)
        {
            _port = port;
        }

        public void Start()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _isRunning = true;

            Console.WriteLine($"[HttpServer] Server running on port {_port} ...");

            // Wir starten einen Task, damit die Accept-Schleife nicht den Main-Thread blockiert.
            Task.Run(() =>
            {
                while (_isRunning)
                {
                    try
                    {
                        // Blockiert, bis ein neuer Client sich verbindet
                        var client = _listener.AcceptTcpClient();
                        Console.WriteLine("[HttpServer] Client connected!");

                        // Wir geben die Client-Verarbeitung an einen ThreadPool-Worker
                        ThreadPool.QueueUserWorkItem(HandleClient, client);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[HttpServer] Error accepting client: {ex.Message}");
                    }
                }
            });
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
        }

        private void HandleClient(object clientObj)
        {
            using var client = (TcpClient)clientObj;
            using var stream = client.GetStream();

            try
            {
                // Request parsen
                var request = HttpRequestParser.ParseFromStream(stream);
                Console.WriteLine($"[HttpServer] Request => Method: {request.Method}, Path: {request.Path}");

                // Statt Dummy-Antwort: Router aufrufen
                var response = Router.Route(request);

                // Antwort in den Stream schreiben
                response.WriteToStream(stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HttpServer] Error parsing HTTP: {ex.Message}");

                // Fehler-Antwort: Hier sehr minimal gehalten
                // Achte darauf, CRLF statt nur LF zu verwenden:
                using var writer = new StreamWriter(stream)
                {
                    NewLine = "\r\n"  // erzwingt CRLF bei WriteLine
                };

                // Statuszeile
                writer.WriteLine("HTTP/1.1 400 Bad Request");
                // Header
                writer.WriteLine("Content-Type: text/plain");
                // Ende der Header
                writer.WriteLine();

                // Body
                writer.WriteLine($"Error: {ex.Message}");

                writer.Flush();
            }
        }
    }
}
