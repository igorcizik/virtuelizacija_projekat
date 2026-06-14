using Common;
using System;
using System.Globalization;
using System.IO;

namespace Service.Storage
{
    public class SessionFileStorage : IDisposable
    {
        private const string Header = "Stator_Winding;Stator_Tooth;Stator_Yoke;PM;Profile_ID;Ambient;Torque;Status;Reason";

        private StreamWriter measurementsWriter;
        private StreamWriter rejectsWriter;
        private bool disposed;

        public string SessionDirectoryPath { get; }
        public string MeasurementsFilePath { get; }
        public string RejectsFilePath { get; }

        public SessionFileStorage(string baseDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(baseDirectoryPath))
            {
                throw new ArgumentException("Putanja za čuvanje sesije nije validna.");
            }

            SessionDirectoryPath = baseDirectoryPath;
            Directory.CreateDirectory(SessionDirectoryPath);

            MeasurementsFilePath = Path.Combine(SessionDirectoryPath, "measurements_session.csv");
            RejectsFilePath = Path.Combine(SessionDirectoryPath, "rejects.csv");

            measurementsWriter = CreateWriter(MeasurementsFilePath);
            rejectsWriter = CreateWriter(RejectsFilePath);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            CloseWriter(ref measurementsWriter, "measurements_session.csv writer je zatvoren.");
            CloseWriter(ref rejectsWriter, "rejects.csv writer je zatvoren.");
            disposed = true;
            GC.SuppressFinalize(this);
        }

        public void WriteAcceptedSample(MotorSample sample)
        {
            WriteSample(measurementsWriter, sample, "ACCEPTED", string.Empty);
        }

        public void WriteRejectedSample(MotorSample sample, string reason)
        {
            WriteSample(rejectsWriter, sample, "REJECTED", reason);
        }

        private static StreamWriter CreateWriter(string path)
        {
            var writer = new StreamWriter(path, false);
            writer.WriteLine(Header);
            writer.Flush();
            return writer;
        }

        private static void CloseWriter(ref StreamWriter writer, string message)
        {
            if (writer == null)
            {
                return;
            }

            writer.Dispose();
            writer = null;
            Console.WriteLine(message);
        }

        private void WriteSample(StreamWriter writer, MotorSample sample, string status, string reason)
        {
            ThrowIfDisposed();

            writer.WriteLine(sample == null
                ? $";;;;;;;{status};{reason}"
                : string.Join(";",
                    sample.Stator_Winding.ToString(CultureInfo.InvariantCulture),
                    sample.Stator_Tooth.ToString(CultureInfo.InvariantCulture),
                    sample.Stator_Yoke.ToString(CultureInfo.InvariantCulture),
                    sample.PM.ToString(CultureInfo.InvariantCulture),
                    sample.Profile_ID.ToString(CultureInfo.InvariantCulture),
                    sample.Ambient.ToString(CultureInfo.InvariantCulture),
                    sample.Torque.ToString(CultureInfo.InvariantCulture),
                    status,
                    reason));

            writer.Flush();
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(SessionFileStorage));
            }
        }
    }
}