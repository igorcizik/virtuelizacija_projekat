using Common;
using System;
using System.Globalization;
using System.IO;

namespace Service.Storage
{
    public class SessionFileStorage : IDisposable
    {
        private StreamWriter measurementsWriter;
        private StreamWriter rejectsWriter;
        private bool disposed = false;

        private readonly string sessionDirectoryPath;
        private readonly string measurementsFilePath;
        private readonly string rejectsFilePath;

        public string SessionDirectoryPath
        {
            get { return sessionDirectoryPath; }
        }

        public string MeasurementsFilePath
        {
            get { return measurementsFilePath; }
        }

        public string RejectsFilePath
        {
            get { return rejectsFilePath; }
        }

        public SessionFileStorage(string baseDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(baseDirectoryPath))
            {
                throw new ArgumentException("Putanja za čuvanje sesije nije validna.");
            }

            sessionDirectoryPath = baseDirectoryPath;

            if (!Directory.Exists(sessionDirectoryPath))
            {
                Directory.CreateDirectory(sessionDirectoryPath);
            }

            measurementsFilePath = Path.Combine(sessionDirectoryPath, "measurements_session.csv");
            rejectsFilePath = Path.Combine(sessionDirectoryPath, "rejects.csv");

            measurementsWriter = new StreamWriter(measurementsFilePath, false);
            rejectsWriter = new StreamWriter(rejectsFilePath, false);

            WriteHeader(measurementsWriter);
            WriteHeader(rejectsWriter);

            measurementsWriter.Flush();
            rejectsWriter.Flush();
        }

        ~SessionFileStorage()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (measurementsWriter != null)
                    {
                        measurementsWriter.Dispose();
                        measurementsWriter = null;
                        Console.WriteLine("[DISPOSE] measurements_session.csv writer je zatvoren.");
                    }

                    if (rejectsWriter != null)
                    {
                        rejectsWriter.Dispose();
                        rejectsWriter = null;
                        Console.WriteLine("[DISPOSE] rejects.csv writer je zatvoren.");
                    }
                }

                disposed = true;
            }
        }

        public void WriteAcceptedSample(MotorSample sample)
        {
            ThrowIfDisposed();
            WriteSample(measurementsWriter, sample, "ACCEPTED", "");
            measurementsWriter.Flush();
        }

        public void WriteRejectedSample(MotorSample sample, string reason)
        {
            ThrowIfDisposed();
            WriteSample(rejectsWriter, sample, "REJECTED", reason);
            rejectsWriter.Flush();
        }

        private void WriteHeader(StreamWriter writer)
        {
            writer.WriteLine("Stator_Winding;Stator_Tooth;Stator_Yoke;PM;Profile_ID;Ambient;Torque;Status;Reason");
        }

        private void WriteSample(StreamWriter writer, MotorSample sample, string status, string reason)
        {
            if (sample == null)
            {
                writer.WriteLine($";;;;;;;{status};{reason}");
                return;
            }

            string line = string.Join(";",
                sample.Stator_Winding.ToString(CultureInfo.InvariantCulture),
                sample.Stator_Tooth.ToString(CultureInfo.InvariantCulture),
                sample.Stator_Yoke.ToString(CultureInfo.InvariantCulture),
                sample.PM.ToString(CultureInfo.InvariantCulture),
                sample.Profile_ID.ToString(CultureInfo.InvariantCulture),
                sample.Ambient.ToString(CultureInfo.InvariantCulture),
                sample.Torque.ToString(CultureInfo.InvariantCulture),
                status,
                reason
            );

            writer.WriteLine(line);
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