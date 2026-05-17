using System;
using System.ServiceModel;

namespace Service
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Kreiramo objekat ServiceHost i prosleđujemo mu tip naše klase servisa
            using (ServiceHost host = new ServiceHost(typeof(SessionService)))
            {
                // Otvaramo servis (WCF u ovom momentu čita adrese i bindinge iz App.config-a)
                host.Open();

                Console.WriteLine("[SERVER] WCF Servis je uspešno pokrenut i spreman za rad.");

                // Ova linija drži konzolu otvorenom. Dokle god je konzola otvorena, servis radi.
                Console.ReadLine();
            }
        }
    }
}