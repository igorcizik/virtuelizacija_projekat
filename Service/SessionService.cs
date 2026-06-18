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
    public class MessageEventArgs : EventArgs
    {
        public string EventMessage { get; }

        public MessageEventArgs(string eventMessage)
        {
            EventMessage = eventMessage;
        }
    }

    public class SampleReceivedEventArgs : EventArgs
    {
        public MotorSample Sample { get; }
        public int SampleNumber { get; }

        public SampleReceivedEventArgs(MotorSample sample, int sampleNumber)
        {
            Sample = sample;
            SampleNumber = sampleNumber;
        }
    }

    public class SessionService : ISession
    {
        public delegate void MessageEventHandler(object sender, MessageEventArgs e);
        public delegate void SampleReceivedEventHandler(object sender, SampleReceivedEventArgs e);

        public event MessageEventHandler OnTransferStarted;
        public event SampleReceivedEventHandler OnSampleReceived;
        public event MessageEventHandler OnTransferCompleted;
        public event MessageEventHandler OnWarningRaised;
        public event MessageEventHandler PMSpike;
        public event MessageEventHandler StatorSpikeW;
        public event MessageEventHandler StatorSpikeT;
        public event MessageEventHandler OutOfBandWarning;

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
            OnTransferStarted += HandleTransferStarted;
            OnSampleReceived += HandleSampleReceived;
            OnTransferCompleted += HandleTransferCompleted;
            OnWarningRaised += HandleWarningRaised;
            PMSpike += HandlePMSpike;
            StatorSpikeW += HandleStatorSpikeW;
            StatorSpikeT += HandleStatorSpikeT;
            OutOfBandWarning += HandleOutOfBandWarning;
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

            RaiseTransferStarted("Transfer started. Status: IN_PROGRESS");
            WriteConsoleMessage(string.Empty, "Kreirani: measurements_session.csv i rejects.csv", ConsoleColor.DarkCyan);
            WriteConsoleMessage("[ OK  ] ", "Sesija je uspešno inicijalizovana.", ConsoleColor.Green);

            return Response(ServerMessage.ACK, SessionStatus.IN_PROGRESS, "Session initialized successfully.");
        }

        public SessionResponse PushSample(MotorSample sample)
        {
            EnsureSessionInProgress();
            RaiseSampleReceived(sample, ++receivedSamples);

            string rejectReason;
            if (!TryValidateSample(sample, out rejectReason))
            {
                sessionFileStorage.WriteRejectedSample(sample, rejectReason);
                WriteConsoleMessage("[NACK ] ", "Uzorak je odbijen: " + rejectReason, ConsoleColor.Red);
                return Response(ServerMessage.NACK, SessionStatus.IN_PROGRESS, rejectReason);
            }

            CheckSuddenChanges(sample);
            CheckRunningPmMean(sample);
            sessionFileStorage.WriteAcceptedSample(sample);
            acceptedSamples.Add(sample);
            previousAcceptedSample = sample;

            WriteConsoleMessage("[ ACK ] ", "Uzorak je prihvaćen i sačuvan.", ConsoleColor.Green);
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
                RaiseTransferCompleted("Transfer completed.");
                WriteConsoleMessage("[STATUS] ", "COMPLETED", ConsoleColor.Green);

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

            if (!TryValidateRange("PM", sample.PM, 0.0, 29.0, out reason) ||
                !TryValidateRange("Stator_Winding", sample.Stator_Winding, 0.0, 30.0, out reason) ||
                !TryValidateRange("Stator_Tooth", sample.Stator_Tooth, 0.0, 25.0, out reason) ||
                !TryValidateRange("Stator_Yoke", sample.Stator_Yoke, 0.0, 140.0, out reason) ||
                !TryValidateRange("Ambient", sample.Ambient, -40.0, 60.0, out reason))
            {
                return false;
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

        private static bool TryValidateRange(string name, double value, double min, double max, out string reason)
        {
            if (value <= min || value > max)
            {
                reason = $"{name} value {value.ToString(CultureInfo.InvariantCulture)} is out of allowed range ({min.ToString(CultureInfo.InvariantCulture)}, {max.ToString(CultureInfo.InvariantCulture)}].";
                return false;
            }

            reason = string.Empty;
            return true;
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
                RaisePMSpike($"delta PM={deltaPm.ToString(CultureInfo.InvariantCulture)}, threshold={pmThreshold.ToString(CultureInfo.InvariantCulture)}, smer: {direction}");
            }

            if (Math.Abs(deltaStatorWinding) > statorWThreshold)
            {
                string direction = deltaStatorWinding > 0 ? "iznad očekivanog" : "ispod očekivanog";
                RaiseStatorSpikeW($"delta Stator_Winding={deltaStatorWinding.ToString(CultureInfo.InvariantCulture)}, threshold={statorWThreshold.ToString(CultureInfo.InvariantCulture)}, smer: {direction}");
            }

            if (Math.Abs(deltaStatorTooth) > statorTThreshold)
            {
                string direction = deltaStatorTooth > 0 ? "iznad očekivanog" : "ispod očekivanog";
                RaiseStatorSpikeT($"delta Stator_Tooth={deltaStatorTooth.ToString(CultureInfo.InvariantCulture)}, threshold={statorTThreshold.ToString(CultureInfo.InvariantCulture)}, smer: {direction}");
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
                RaiseOutOfBandWarning($"PM is below expected value: PM={sample.PM.ToString(CultureInfo.InvariantCulture)}, T_mean={pmMean.ToString(CultureInfo.InvariantCulture)}");
            }
            else if (sample.PM > upperBound)
            {
                RaiseOutOfBandWarning($"PM is above expected value: PM={sample.PM.ToString(CultureInfo.InvariantCulture)}, T_mean={pmMean.ToString(CultureInfo.InvariantCulture)}");
            }
        }

        public void RaiseTransferStarted(string message)
        {
            if (OnTransferStarted != null)
            {
                OnTransferStarted(this, new MessageEventArgs(message));
            }
        }

        public void RaiseSampleReceived(MotorSample sample, int sampleNumber)
        {
            if (OnSampleReceived != null)
            {
                OnSampleReceived(this, new SampleReceivedEventArgs(sample, sampleNumber));
            }
        }

        public void RaiseTransferCompleted(string message)
        {
            if (OnTransferCompleted != null)
            {
                OnTransferCompleted(this, new MessageEventArgs(message));
            }
        }

        public void RaiseWarning(string message)
        {
            if (OnWarningRaised != null)
            {
                OnWarningRaised(this, new MessageEventArgs(message));
            }
        }

        public void RaisePMSpike(string message)
        {
            if (PMSpike != null)
            {
                PMSpike(this, new MessageEventArgs(message));
            }
        }

        public void RaiseStatorSpikeW(string message)
        {
            if (StatorSpikeW != null)
            {
                StatorSpikeW(this, new MessageEventArgs(message));
            }
        }

        public void RaiseStatorSpikeT(string message)
        {
            if (StatorSpikeT != null)
            {
                StatorSpikeT(this, new MessageEventArgs(message));
            }
        }

        public void RaiseOutOfBandWarning(string message)
        {
            if (OutOfBandWarning != null)
            {
                OutOfBandWarning(this, new MessageEventArgs(message));
            }
        }

        private void HandleTransferStarted(object sender, MessageEventArgs e)
        {
            WriteConsoleMessage(string.Empty, e.EventMessage, ConsoleColor.Cyan);
        }

        private void HandleSampleReceived(object sender, SampleReceivedEventArgs e)
        {
            WriteConsoleMessage(
                $"[{e.SampleNumber,3}] ",
                $"Primljen uzorak | PM: {e.Sample.PM,8:F3} | Winding: {e.Sample.Stator_Winding,8:F3} | Tooth: {e.Sample.Stator_Tooth,8:F3}",
                ConsoleColor.Gray);
        }

        private void HandleTransferCompleted(object sender, MessageEventArgs e)
        {
            WriteConsoleMessage("[KRAJ ] ", e.EventMessage, ConsoleColor.Green);
        }

        private void HandleWarningRaised(object sender, MessageEventArgs e)
        {
            WriteConsoleMessage("[WARN ] ", e.EventMessage, ConsoleColor.Yellow);
        }

        private void HandlePMSpike(object sender, MessageEventArgs e)
        {
            RaiseWarning("PMSpike: " + e.EventMessage);
        }

        private void HandleStatorSpikeW(object sender, MessageEventArgs e)
        {
            RaiseWarning("StatorSpikeW: " + e.EventMessage);
        }

        private void HandleStatorSpikeT(object sender, MessageEventArgs e)
        {
            RaiseWarning("StatorSpikeT: " + e.EventMessage);
        }

        private void HandleOutOfBandWarning(object sender, MessageEventArgs e)
        {
            RaiseWarning("OutOfBandWarning: " + e.EventMessage);
        }

        private static void ThrowDataFormat(string message)
        {
            throw new FaultException<DataFormatFault>(new DataFormatFault(message));
        }

        private static void ThrowSessionState(string message)
        {
            throw new FaultException<SessionStateFault>(new SessionStateFault(message));
        }
        private static void WriteConsoleMessage(string prefix, string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(prefix);
            Console.ResetColor();
            Console.WriteLine(message);
        }
    }
}
