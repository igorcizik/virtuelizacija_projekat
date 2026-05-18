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
        
        private static SessionStatus? currentStatus = null;
        private double statorWThreshold;

        public SessionService()
        {
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

            if (sample.PM <= 0 || sample.Stator_Winding <= 0 || sample.Stator_Tooth <= 0 || sample.Stator_Yoke <= 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Vrednosti senzora moraju biti veće od 0."));
            }

      
            if (sample.Stator_Winding > statorWThreshold)
            {
                Console.WriteLine($"[SERVER] Stator Winding prešao prag: {sample.Stator_Winding} > {statorWThreshold}");
            }
            else
            {
                Console.WriteLine($"[SERVER] Uzorak uspešan");
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