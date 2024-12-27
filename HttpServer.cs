using System;
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

            // Wir starten einen Task, damit das in einem Hintergrund-Thread läuft
            Task.Run(() =>
            {
                while (_isRunning)
                {
                    try
                    {
                        // Blockiert, bis ein Client sich verbindet
                        var client = _listener.AcceptTcpClient();
                        Console.WriteLine("[HttpServer] Client connected!");

                        // Noch keine richtige Verarbeitung – nur Info
                        // Hier später: ThreadPool.QueueUserWorkItem(HandleClient, client);
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
    }
}