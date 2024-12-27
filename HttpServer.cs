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
            // Wir wandeln das object in einen TcpClient um
            using var client = (TcpClient)clientObj;
            using var stream = client.GetStream();

            try
            {
                // 1) Versuchen, einen HttpRequest zu parsen
                var request = HttpRequestParser.ParseFromStream(stream);
                Console.WriteLine($"[HttpServer] Request => Method: {request.Method}, Path: {request.Path}");

                // 2) Dummy-Antwort erstellen
                using var writer = new StreamWriter(stream);
                writer.WriteLine("HTTP/1.1 200 OK");
                writer.WriteLine("Content-Type: text/plain");
                writer.WriteLine();
                writer.WriteLine($"Parsed request with Method={request.Method}, Path={request.Path}, " +
                                 $"BodyLength={request.Body.Length}, Auth={request.Authorization}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HttpServer] Error parsing HTTP: {ex.Message}");

                // 3) Fehler-Antwort zurücksenden
                using var writer = new StreamWriter(stream);
                writer.WriteLine("HTTP/1.1 400 Bad Request");
                writer.WriteLine("Content-Type: text/plain");
                writer.WriteLine();
                writer.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
