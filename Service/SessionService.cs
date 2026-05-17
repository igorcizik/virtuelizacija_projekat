using Common;
using Common.Enums;
using System;
using System.Configuration;
using System.Globalization;
using System.ServiceModel;

namespace Service
{
    public class SessionService : ISession
    {
        // Koristimo statičku promenljivu jer se u praktikumu objekat servisa instancira PerCall.
        // Static obezbeđuje da svi pozivi (metoda) dele isto stanje sesije.
        private static SessionStatus? currentStatus = null;
        private double statorWThreshold;

        public SessionService()
        {
            // Čitanje iz App.config (poglavlje "Konfiguracija WCF servisa" iz praktikuma)
            statorWThreshold = double.Parse(ConfigurationManager.AppSettings["Stator_w_threshold"], CultureInfo.InvariantCulture);
        }

        public ServerMessage StartSession(Meta meta)
        {
            currentStatus = SessionStatus.IN_PROGRESS;
            Console.WriteLine("\n[SERVER] Status: IN_PROGRESS");
            Console.WriteLine($"[SERVER] Sesija uspešno inicijalizovana za Profile_ID: {meta.Profile_ID}");
            return ServerMessage.ACK;
        }

        public ServerMessage PushSample(MotorSample sample)
        {
            // Provera da li je sesija aktivna
            if (currentStatus != SessionStatus.IN_PROGRESS)
            {
                Console.WriteLine("[SERVER] Greška: Pokušaj slanja uzorka pre pokretanja sesije.");
                return ServerMessage.NACK;
            }

            // Validacija opsega (Zadatak 3) - bacanje FaultException-a iz praktikuma
            if (sample.PM <= 0 || sample.Stator_Winding <= 0 || sample.Stator_Tooth <= 0 || sample.Stator_Yoke <= 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Vrednosti senzora moraju biti veće od 0."));
            }

            // Provera praga iz konfiguracije
            if (sample.Stator_Winding > statorWThreshold)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[SERVER] Stator Winding prešao prag: {sample.Stator_Winding} > {statorWThreshold}");
            }
            else
            {
                Console.WriteLine($"[SERVER] Uzorak uspešan -> PM: {sample.PM}, Stator Winding: {sample.Stator_Winding}");
            }

            return ServerMessage.ACK;
        }

        public ServerMessage EndSession()
        {
            currentStatus = SessionStatus.COMPLETED;
            Console.WriteLine("[SERVER] Status promenjen u: COMPLETED");
            Console.WriteLine("[SERVER] Sesija uspešno zatvorena.\n");
            return ServerMessage.ACK;
        }
    }
}