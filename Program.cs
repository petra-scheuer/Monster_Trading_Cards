using System;
using System.Threading;

namespace MonsterCardTradingGame
{
    class Program
    {
        static void Main(string[] args)
        {
            // Port festlegen
            int port = 10001;

            // Instanz unseres HttpServers erzeugen
            HttpServer server = new HttpServer(port);

            // Server starten
            server.Start();

            // Eine kleine Schleife, damit die Main nicht gleich beendet wird:
            // Drücke 'q' um das Programm zu beenden (Server.Stop() zu rufen).
            Console.WriteLine("Press 'q' to stop the server...");
            while (true)
            {
                if (Console.ReadKey(true).KeyChar == 'q')
                {
                    server.Stop();
                    break;
                }
            }
            Console.WriteLine("Server stopped. Bye!");
        }
    }
}