using System;
using System.Threading;

namespace MonsterCardTradingGame
{
    class Program
    {
        static void Main(string[] args)
        {
            // Datenbank einrichten
            DatabaseManager.SetupTables();

            // Verbindung testen
            bool dbOk = DatabaseManager.TestConnection();
            if (dbOk)
                Console.WriteLine("[DB] Connection is OK!");
            else
                Console.WriteLine("[DB] Could not connect to DB!");
            // Port festlegen
            int port = 10001;

            // Instanz unseres HttpServers erzeugen
            HttpServer server = new HttpServer(port);

            // Server starten
            server.Start();

            
            // Drücke 'q' um das Programm zu beendenq.
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