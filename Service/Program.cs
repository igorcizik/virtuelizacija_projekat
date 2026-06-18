using System;
using System.ServiceModel;

namespace Service
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "PMSM Motor Monitoring - Servis";

            using (ServiceHost host = new ServiceHost(typeof(SessionService)))
            {
            
                host.Open();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[ OK ] ");
                Console.ResetColor();
                Console.WriteLine("Servis je pokrenut na net.tcp://localhost:4000/Service");
                Console.WriteLine();
                Console.WriteLine("Čekanje na klijenta...");
                Console.ReadLine();
            }
        }
    }
}
