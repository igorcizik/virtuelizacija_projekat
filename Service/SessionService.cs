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
        private static SessionStatus? currentStatus;
        private static SessionFileStorage sessionFileStorage;
        private static readonly List<MotorSample> acceptedSamples = new List<MotorSample>();

        private readonly double statorWThreshold = ReadThreshold("Stator_w_threshold");
        private readonly double statorTThreshold = ReadThreshold("Stator_t_threshold");
        private readonly double pmThreshold = ReadThreshold("PM_threshold");

        public SessionResponse StartSession(Meta meta)
        {
            if (!IsMetaValid(meta))
            {
                ThrowDataFormat("Meta-zaglavlje nije validno. Sva polja su obavezna.");
            }

            string storagePath = ConfigurationManager.AppSettings["Session_storage_path"];
            if (string.IsNullOrWhiteSpace(storagePath))
            {
                ThrowDataFormat("Putanja za čuvanje fajlova sesije nije definisana u konfiguraciji.");
            }

            acceptedSamples.Clear();
            currentStatus = SessionStatus.IN_PROGRESS;
            sessionFileStorage?.Dispose();
            sessionFileStorage = new SessionFileStorage(storagePath);

            Console.WriteLine("\nStatus: IN_PROGRESS");
            Console.WriteLine("Kreirani fajlovi: measurements_session.csv i rejects.csv");
            Console.WriteLine("Sesija uspešno inicijalizovana.");

            return Response(ServerMessage.ACK, SessionStatus.IN_PROGRESS, "Sesija uspesno inicijalizovana.");
        }

        public SessionResponse PushSample(MotorSample sample)
        {
            EnsureSessionInProgress();

            string rejectReason;
            if (!TryValidateSample(sample, out rejectReason))
            {
                sessionFileStorage.WriteRejectedSample(sample, rejectReason);
                Console.WriteLine("Uzorak je odbačen: " + rejectReason);
                return Response(ServerMessage.NACK, SessionStatus.IN_PROGRESS, rejectReason);
            }

            CheckThresholds(sample);
            CheckAverageDeviation(sample);
            sessionFileStorage.WriteAcceptedSample(sample);
            acceptedSamples.Add(sample);

            Console.WriteLine("Uzorak uspešno prihvaćen i upisan u measurements_session.csv.");
            return Response(ServerMessage.ACK, SessionStatus.IN_PROGRESS, "Uzorak uspešno prihvaćen.");
        }

        public SessionResponse EndSession()
        {
            try
            {
                if (currentStatus != SessionStatus.IN_PROGRESS)
                {
                    ThrowSessionState("Nije moguće završiti sesiju jer aktivna sesija ne postoji.");
                }

                currentStatus = SessionStatus.COMPLETED;
                Console.WriteLine("Status promenjen u: COMPLETED");
                Console.WriteLine("Sesija uspešno zatvorena.\n");

                return Response(ServerMessage.ACK, SessionStatus.COMPLETED, "Sesija uspesno zatvorena");
            }
            finally
            {
                sessionFileStorage?.Dispose();
                sessionFileStorage = null;
            }
        }

        private static double ReadThreshold(string key)
        {
            double value;
            if (double.TryParse(ConfigurationManager.AppSettings[key], NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }

            ThrowDataFormat(key + " nije validno podešen u konfiguraciji.");
            return 0;
        }

        private static SessionResponse Response(ServerMessage message, SessionStatus status, string details)
        {
            return new SessionResponse(message, status, details);
        }

        private static bool IsMetaValid(Meta meta)
        {
            return meta != null && meta.Stator_Winding && meta.Stator_Tooth && meta.Stator_Yoke &&
                   meta.PM && meta.Profile_ID && meta.Ambient && meta.Torque;
        }

        private static void EnsureSessionInProgress()
        {
            if (currentStatus != SessionStatus.IN_PROGRESS)
            {
                ThrowSessionState("Nije moguće poslati uzorak jer sesija nije u IN_PROGRESS stanju.");
            }

            if (sessionFileStorage == null)
            {
                ThrowSessionState("Skladište fajlova nije inicijalizovano. Prvo pokrenite sesiju.");
            }
        }

        private static bool TryValidateSample(MotorSample sample, out string reason)
        {
            if (sample == null)
            {
                reason = "Uzorak je null.";
                return false;
            }

            foreach (var value in new[]
            {
                Tuple.Create("PM", sample.PM),
                Tuple.Create("Stator_Winding", sample.Stator_Winding),
                Tuple.Create("Stator_Tooth", sample.Stator_Tooth),
                Tuple.Create("Stator_Yoke", sample.Stator_Yoke),
                Tuple.Create("Ambient", sample.Ambient),
                Tuple.Create("Profile_ID", (double)sample.Profile_ID)
            })
            {
                if (value.Item2 <= 0)
                {
                    reason = value.Item1 + " mora biti veći od 0.";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        private void CheckThresholds(MotorSample sample)
        {
            foreach (var value in new[]
            {
                Tuple.Create("Stator_Winding", sample.Stator_Winding, statorWThreshold),
                Tuple.Create("Stator_Tooth", sample.Stator_Tooth, statorTThreshold),
                Tuple.Create("PM", sample.PM, pmThreshold)
            }.Where(x => x.Item2 > x.Item3))
            {
                Console.WriteLine($"{value.Item1} prešao prag: {value.Item2} > {value.Item3}");
            }
        }

        private static void CheckAverageDeviation(MotorSample sample)
        {
            if (acceptedSamples.Count == 0)
            {
                return;
            }

            CheckDeviation("Stator_Winding", sample.Stator_Winding, acceptedSamples.Average(x => x.Stator_Winding));
            CheckDeviation("Stator_Tooth", sample.Stator_Tooth, acceptedSamples.Average(x => x.Stator_Tooth));
            CheckDeviation("PM", sample.PM, acceptedSamples.Average(x => x.PM));
        }

        private static void CheckDeviation(string name, double value, double average)
        {
            if (average != 0 && (value < average * 0.75 || value > average * 1.25))
            {
                Console.WriteLine($"{name} odstupa više od ±25% od proseka. Vrednost: {value}, prosek: {average}");
            }
        }

        private static void ThrowDataFormat(string message)
        {
            throw new FaultException<DataFormatFault>(new DataFormatFault(message));
        }

        private static void ThrowSessionState(string message)
        {
            throw new FaultException<SessionStateFault>(new SessionStateFault(message));
        }
    }
}