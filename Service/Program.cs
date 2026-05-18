using System;
using System.ServiceModel;

namespace Service
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (ServiceHost host = new ServiceHost(typeof(SessionService)))
            {
            
                host.Open();
                Console.WriteLine("[SERVER] WCF Servis je uspešno pokrenut i spreman za rad.");
                Console.ReadLine();
            }
        }
    }
}