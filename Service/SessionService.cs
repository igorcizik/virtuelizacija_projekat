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
        public delegate void TransferStartedHandler(string message);
        public delegate void SampleReceivedHandler(MotorSample sample, int sampleNumber);
        public delegate void TransferCompletedHandler(string message);
        public delegate void WarningRaisedHandler(string message);

        public event TransferStartedHandler OnTransferStarted;
        public event SampleReceivedHandler OnSampleReceived;
        public event TransferCompletedHandler OnTransferCompleted;
        public event WarningRaisedHandler OnWarningRaised;
        public event WarningRaisedHandler PMSpike;
        public event WarningRaisedHandler StatorSpikeW;
        public event WarningRaisedHandler StatorSpikeT;
        public event WarningRaisedHandler OutOfBandWarning;

        private static SessionStatus? currentStatus;
        private static SessionFileStorage sessionFileStorage;
        private static readonly List<MotorSample> acceptedSamples = new List<MotorSample>();
        private static MotorSample previousAcceptedSample;
        private static int receivedSamples;

        private readonly double statorWThreshold = ReadThreshold("Stator_w_threshold");
        private readonly double statorTThreshold = ReadThreshold("Stator_t_threshold");
        private readonly double pmThreshold = ReadThreshold("PM_threshold");
        private readonly double pmDeviationPercent = ReadThreshold("PM_deviation_percent");

        public SessionService()
        {
            OnTransferStarted += Console.WriteLine;
            OnSampleReceived += (sample, sampleNumber) => Console.WriteLine($"Transfer in progress... received sample #{sampleNumber}");
            OnTransferCompleted += Console.WriteLine;
            OnWarningRaised += message => Console.WriteLine("Warning: " + message);
            PMSpike += message => OnWarningRaised?.Invoke("PMSpike: " + message);
            StatorSpikeW += message => OnWarningRaised?.Invoke("StatorSpikeW: " + message);
            StatorSpikeT += message => OnWarningRaised?.Invoke("StatorSpikeT: " + message);
            OutOfBandWarning += message => OnWarningRaised?.Invoke("OutOfBandWarning: " + message);
        }

        public SessionResponse StartSession(Meta meta)
        {
            if (!IsMetaValid(meta))
            {
                ThrowDataFormat("Meta header is not valid. All fields are required.");
            }

            string storagePath = ConfigurationManager.AppSettings["Session_storage_path"];
            if (string.IsNullOrWhiteSpace(storagePath))
            {
                ThrowDataFormat("Session storage path is not defined in configuration.");
            }

            acceptedSamples.Clear();
            previousAcceptedSample = null;
            receivedSamples = 0;
            currentStatus = SessionStatus.IN_PROGRESS;
            sessionFileStorage?.Dispose();
            sessionFileStorage = new SessionFileStorage(storagePath);

            OnTransferStarted?.Invoke("Transfer started. Status: IN_PROGRESS");
            Console.WriteLine("Created files: measurements_session.csv and rejects.csv");
            Console.WriteLine("Session initialized successfully.");

            return Response(ServerMessage.ACK, SessionStatus.IN_PROGRESS, "Session initialized successfully.");
        }

        public SessionResponse PushSample(MotorSample sample)
        {
            EnsureSessionInProgress();
            OnSampleReceived?.Invoke(sample, ++receivedSamples);

            string rejectReason;
            if (!TryValidateSample(sample, out rejectReason))
            {
                sessionFileStorage.WriteRejectedSample(sample, rejectReason);
                Console.WriteLine("Sample rejected: " + rejectReason);
                return Response(ServerMessage.NACK, SessionStatus.IN_PROGRESS, rejectReason);
            }

            CheckThresholds(sample);
            CheckSuddenChanges(sample);
            CheckRunningPmMean(sample);
            sessionFileStorage.WriteAcceptedSample(sample);
            acceptedSamples.Add(sample);
            previousAcceptedSample = sample;

            Console.WriteLine("Sample accepted and written to measurements_session.csv.");
            return Response(ServerMessage.ACK, SessionStatus.IN_PROGRESS, "Sample accepted.");
        }

        public SessionResponse EndSession()
        {
            try
            {
                if (currentStatus != SessionStatus.IN_PROGRESS)
                {
                    ThrowSessionState("Session cannot be completed because there is no active session.");
                }

                currentStatus = SessionStatus.COMPLETED;
                OnTransferCompleted?.Invoke("Transfer completed.");
                Console.WriteLine("Status changed to: COMPLETED");
                Console.WriteLine("Session closed successfully.\n");

                return Response(ServerMessage.ACK, SessionStatus.COMPLETED, "Transfer completed. Session closed successfully.");
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

            ThrowDataFormat(key + " is not valid in configuration.");
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
                ThrowSessionState("Cannot send sample because the session is not IN_PROGRESS.");
            }

            if (sessionFileStorage == null)
            {
                ThrowSessionState("File storage is not initialized. Start session first.");
            }
        }

        private static bool TryValidateSample(MotorSample sample, out string reason)
        {
            if (sample == null)
            {
                reason = "Sample is null.";
                return false;
            }

            var rangeChecks = new[]
            {
                Tuple.Create("PM", sample.PM, 0.0, 29.0),
                Tuple.Create("Stator_Winding", sample.Stator_Winding, 0.0, 30.0),
                Tuple.Create("Stator_Tooth", sample.Stator_Tooth, 0.0, 25.0),
                Tuple.Create("Stator_Yoke", sample.Stator_Yoke, 0.0, 140.0),
                Tuple.Create("Ambient", sample.Ambient, -40.0, 60.0)
            };

            foreach (var check in rangeChecks)
            {
                if (check.Item2 <= check.Item3 || check.Item2 > check.Item4)
                {
                    reason = $"{check.Item1} value {check.Item2.ToString(CultureInfo.InvariantCulture)} is out of allowed range ({check.Item3.ToString(CultureInfo.InvariantCulture)}, {check.Item4.ToString(CultureInfo.InvariantCulture)}].";
                    return false;
                }
            }

            if (sample.Torque > 48.5)
            {
                reason = $"Torque value {sample.Torque.ToString(CultureInfo.InvariantCulture)} exceeds allowed maximum (45).";
                return false;
            }

            if (sample.Profile_ID <= 0)
            {
                reason = "Profile_ID must be greater than 0.";
                return false;
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
                OnWarningRaised?.Invoke($"{value.Item1} exceeded configured threshold: {value.Item2} > {value.Item3}");
            }
        }

        private void CheckSuddenChanges(MotorSample sample)
        {
            if (previousAcceptedSample == null)
            {
                return;
            }

            double deltaPm = sample.PM - previousAcceptedSample.PM;
            double deltaStatorWinding = sample.Stator_Winding - previousAcceptedSample.Stator_Winding;
            double deltaStatorTooth = sample.Stator_Tooth - previousAcceptedSample.Stator_Tooth;

            if (Math.Abs(deltaPm) > pmThreshold)
            {
                string direction = deltaPm > 0 ? "iznad očekivanog" : "ispod očekivanog";
                PMSpike?.Invoke($"delta PM={deltaPm.ToString(CultureInfo.InvariantCulture)}, threshold={pmThreshold.ToString(CultureInfo.InvariantCulture)}, smer: {direction}");
            }

            if (Math.Abs(deltaStatorWinding) > statorWThreshold)
            {
                string direction = deltaStatorWinding > 0 ? "iznad očekivanog" : "ispod očekivanog";
                StatorSpikeW?.Invoke($"delta Stator_Winding={deltaStatorWinding.ToString(CultureInfo.InvariantCulture)}, threshold={statorWThreshold.ToString(CultureInfo.InvariantCulture)}, smer: {direction}");
            }

            if (Math.Abs(deltaStatorTooth) > statorTThreshold)
            {
                string direction = deltaStatorTooth > 0 ? "iznad očekivanog" : "ispod očekivanog";
                StatorSpikeT?.Invoke($"delta Stator_Tooth={deltaStatorTooth.ToString(CultureInfo.InvariantCulture)}, threshold={statorTThreshold.ToString(CultureInfo.InvariantCulture)}, smer: {direction}");
            }
        }

        private void CheckRunningPmMean(MotorSample sample)
        {
            double pmMean = (acceptedSamples.Sum(x => x.PM) + sample.PM) / (acceptedSamples.Count + 1);
            if (pmMean == 0)
            {
                return;
            }

            double lowerBound = pmMean * (1 - pmDeviationPercent);
            double upperBound = pmMean * (1 + pmDeviationPercent);

            if (sample.PM < lowerBound)
            {
                OutOfBandWarning?.Invoke($"PM is below expected value: PM={sample.PM.ToString(CultureInfo.InvariantCulture)}, T_mean={pmMean.ToString(CultureInfo.InvariantCulture)}");
            }
            else if (sample.PM > upperBound)
            {
                OutOfBandWarning?.Invoke($"PM is above expected value: PM={sample.PM.ToString(CultureInfo.InvariantCulture)}, T_mean={pmMean.ToString(CultureInfo.InvariantCulture)}");
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
