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

            Task.Run(() =>
            {
                while (_isRunning)
                {
                    try
                    {
                        var client = _listener.AcceptTcpClient();
                        Console.WriteLine("[HttpServer] Client connected!");

                        // NEU: Wir rufen jetzt HandleClient auf
                        ThreadPool.QueueUserWorkItem(HandleClient, client);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[HttpServer] Error accepting client: {ex.Message}");
                    }
                }
            });
        }

        private void HandleClient(object clientObj)
        {
            using var client = (TcpClient)clientObj;
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream);

            // Einfaches Lesen aller Zeilen, bis der Client abbricht:
            try
            {
                string line;
                while ((line = reader.ReadLine()) != null && line.Length > 0)
                {
                    Console.WriteLine("[Client says]: " + line);
                }

                // Ggf. eine primitive Antwort:
                using var writer = new StreamWriter(stream);
                writer.WriteLine("HTTP/1.1 200 OK");
                writer.WriteLine("Content-Type: text/plain");
                writer.WriteLine();
                writer.WriteLine("Hello from our bare-bones server!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HttpServer] Error handling client: {ex.Message}");
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
        }
    }
}
