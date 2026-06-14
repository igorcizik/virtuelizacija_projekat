using Common;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Service.Storage
{
    public class SessionFileStorage : IDisposable
    {
        private const string Header = "Stator_Winding;Stator_Tooth;Stator_Yoke;PM;Profile_ID;Ambient;Torque;Status;Reason";

        private FileStream measurementsStream;
        private FileStream rejectsStream;
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

            measurementsWriter = CreateWriter(MeasurementsFilePath, out measurementsStream);
            rejectsWriter = CreateWriter(RejectsFilePath, out rejectsStream);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            CloseWriter(ref measurementsWriter, ref measurementsStream, "measurements_session.csv writer je zatvoren.");
            CloseWriter(ref rejectsWriter, ref rejectsStream, "rejects.csv writer je zatvoren.");
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

        private static StreamWriter CreateWriter(string path, out FileStream stream)
        {
            stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.WriteLine(Header);
            writer.Flush();
            return writer;
        }

        private static void CloseWriter(ref StreamWriter writer, ref FileStream stream, string message)
        {
            if (writer == null)
            {
                return;
            }

            writer.Flush();
            writer.Dispose();
            writer = null;
            stream = null;
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
