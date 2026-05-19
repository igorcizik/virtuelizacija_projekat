using Common;
using Common.Enums;
using Service.Storage;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.ServiceModel;

namespace Service
{
    public class SessionService : ISession
    {
        
        private static SessionStatus? currentStatus = null;
        private double statorWThreshold;
        private double statorTThreshold;
        private double pmThreshold;
        private static SessionFileStorage sessionFileStorage;
        private static readonly List<MotorSample> acceptedSamples = new List<MotorSample>();

        public SessionService()
        {
            if (!double.TryParse(ConfigurationManager.AppSettings["Stator_w_threshold"], NumberStyles.Float, CultureInfo.InvariantCulture, out statorWThreshold))
            {
                throw new FaultException<DataFormatFault>(new DataFormatFault("Stator_w_threshold nije validno podešen u konfiguraciji."));
            }

            if (!double.TryParse(ConfigurationManager.AppSettings["Stator_t_threshold"], NumberStyles.Float, CultureInfo.InvariantCulture, out statorTThreshold))
            {
                throw new FaultException<DataFormatFault>(new DataFormatFault("Stator_t_threshold nije validno podešen u konfiguraciji."));
            }

            if (!double.TryParse(ConfigurationManager.AppSettings["PM_threshold"], NumberStyles.Float, CultureInfo.InvariantCulture, out pmThreshold))
            {
                throw new FaultException<DataFormatFault>(new DataFormatFault("PM_threshold nije validno podešen u konfiguraciji."));
            }
        }

        public SessionResponse StartSession(Meta meta)
        {
            if (!IsMetaValid(meta))
            {
                throw new FaultException<DataFormatFault>(new DataFormatFault("Meta-zaglavlje nije validno. Sva polja su obavezna."));
            }

            acceptedSamples.Clear();
            currentStatus = SessionStatus.IN_PROGRESS;

            string storagePath = ConfigurationManager.AppSettings["Session_storage_path"];

            if (string.IsNullOrWhiteSpace(storagePath))
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Putanja za čuvanje fajlova sesije nije definisana u konfiguraciji."));
            }

            if (sessionFileStorage != null)
            {
                sessionFileStorage.Dispose();
            }

            sessionFileStorage = new SessionFileStorage(storagePath);

            Console.WriteLine("\nStatus: IN_PROGRESS");
            Console.WriteLine("Kreirani fajlovi: measurements_session.csv i rejects.csv");
            Console.WriteLine("Sesija uspešno inicijalizovana.");

            return new SessionResponse(ServerMessage.ACK,SessionStatus.IN_PROGRESS,"Sesija uspesno inicijalizovana.");
        }

        public SessionResponse PushSample(MotorSample sample)
        {
            if (currentStatus != SessionStatus.IN_PROGRESS)
            {
                throw new FaultException<SessionStateFault>(
                    new SessionStateFault("Nije moguće poslati uzorak jer sesija nije u IN_PROGRESS stanju."));
            }

            if (sessionFileStorage == null)
            {
                throw new FaultException<SessionStateFault>(new SessionStateFault("Skladište fajlova nije inicijalizovano. Prvo pokrenite sesiju."));
            }

            string rejectReason;

            if (!TryValidateSample(sample, out rejectReason))
            {
                sessionFileStorage.WriteRejectedSample(sample, rejectReason);
                Console.WriteLine("Uzorak je odbačen: " + rejectReason);
                return new SessionResponse(ServerMessage.NACK,SessionStatus.IN_PROGRESS,rejectReason);
            }

            CheckThresholds(sample);
            CheckAverageDeviation(sample);

            sessionFileStorage.WriteAcceptedSample(sample);
            acceptedSamples.Add(sample);

            Console.WriteLine("Uzorak uspešno prihvaćen i upisan u measurements_session.csv.");
            return new SessionResponse(ServerMessage.ACK,SessionStatus.IN_PROGRESS,"Uzorak uspešno prihvaćen.");
        }

        public SessionResponse EndSession()
        {
            try
            {
                if (currentStatus != SessionStatus.IN_PROGRESS)
                {
                    throw new FaultException<SessionStateFault>(new SessionStateFault("Nije moguće završiti sesiju jer aktivna sesija ne postoji."));
                }

                currentStatus = SessionStatus.COMPLETED;

                Console.WriteLine("Status promenjen u: COMPLETED");
                Console.WriteLine("Sesija uspešno zatvorena.\n");

                return new SessionResponse(ServerMessage.ACK,SessionStatus.COMPLETED, "Sesija uspesno zatvorena");
            }
            finally
            {
                if (sessionFileStorage != null)
                {
                    sessionFileStorage.Dispose();
                    sessionFileStorage = null;
                }
            }
        }

        private bool IsMetaValid(Meta meta)
        {
            return meta != null &&
                   meta.Stator_Winding &&
                   meta.Stator_Tooth &&
                   meta.Stator_Yoke &&
                   meta.PM &&
                   meta.Profile_ID &&
                   meta.Ambient &&
                   meta.Torque;
        }

        private bool TryValidateSample(MotorSample sample, out string reason)
        {
            if (sample == null)
            {
                reason = "Uzorak je null.";
                return false;
            }

            if (sample.PM <= 0)
            {
                reason = "PM mora biti veći od 0.";
                return false;
            }

            if (sample.Stator_Winding <= 0)
            {
                reason = "Stator_Winding mora biti veći od 0.";
                return false;
            }

            if (sample.Stator_Tooth <= 0)
            {
                reason = "Stator_Tooth mora biti veći od 0.";
                return false;
            }

            if (sample.Stator_Yoke <= 0)
            {
                reason = "Stator_Yoke mora biti veći od 0.";
                return false;
            }

            if (sample.Ambient <= 0)
            {
                reason = "Ambient mora biti veći od 0.";
                return false;
            }

            if (sample.Profile_ID <= 0)
            {
                reason = "Profile_ID mora biti veći od 0.";
                return false;
            }

            reason = "";
            return true;
        }

        private void CheckThresholds(MotorSample sample)
        {
            if (sample.Stator_Winding > statorWThreshold)
            {
                Console.WriteLine($"Stator_Winding prešao prag: {sample.Stator_Winding} > {statorWThreshold}");
            }

            if (sample.Stator_Tooth > statorTThreshold)
            {
                Console.WriteLine($"Stator_Tooth prešao prag: {sample.Stator_Tooth} > {statorTThreshold}");
            }

            if (sample.PM > pmThreshold)
            {
                Console.WriteLine($"PM prešao prag: {sample.PM} > {pmThreshold}");
            }
        }



        private void CheckAverageDeviation(MotorSample sample)
        {
            if (acceptedSamples.Count == 0)
            {
                return;
            }

            double avgStatorWinding = acceptedSamples.Average(x => x.Stator_Winding);
            double avgStatorTooth = acceptedSamples.Average(x => x.Stator_Tooth);
            double avgPM = acceptedSamples.Average(x => x.PM);

            if (!IsWithin25Percent(sample.Stator_Winding, avgStatorWinding))
            {
                Console.WriteLine($"Stator_Winding odstupa više od ±25% od proseka. Vrednost: {sample.Stator_Winding}, prosek: {avgStatorWinding}");
            }

            if (!IsWithin25Percent(sample.Stator_Tooth, avgStatorTooth))
            {
                Console.WriteLine($"Stator_Tooth odstupa više od ±25% od proseka. Vrednost: {sample.Stator_Tooth}, prosek: {avgStatorTooth}");
            }

            if (!IsWithin25Percent(sample.PM, avgPM))
            {
                Console.WriteLine($"PM odstupa više od ±25% od proseka. Vrednost: {sample.PM}, prosek: {avgPM}");
            }
        }

        private bool IsWithin25Percent(double value, double average)
        {
            if (average == 0)
            {
                return true;
            }

            double lowerBound = average * 0.75;
            double upperBound = average * 1.25;

            return value >= lowerBound && value <= upperBound;
        }
    }
}