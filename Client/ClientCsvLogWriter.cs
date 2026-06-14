using System;
using System.IO;

namespace Client.Logs
{
    public class ClientCsvLogWriter : IDisposable
    {
        private StreamWriter streamWriter;
        private bool disposed;

        public string Path { get; }

        public ClientCsvLogWriter(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Putanja log fajla nije validna.");
            }

            Path = path;
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            streamWriter = new StreamWriter(path, false);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            streamWriter?.Flush();
            streamWriter?.Dispose();
            streamWriter = null;
            disposed = true;
            Console.WriteLine("CSV log writer je zatvoren.");
        }

        public void WriteInvalidRow(int rowNumber, string line, string reason)
        {
            Write($"INVALID ROW | Row: {rowNumber} | Reason: {reason} | Data: {line}");
        }

        public void WriteExcessRow(int rowNumber, string line)
        {
            Write($"EXCESS ROW | Row: {rowNumber} | Data: {line}");
        }

        public void WriteInfo(string message)
        {
            Write($"INFO | {DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}");
        }

        private void Write(string line)
        {
            ThrowIfDisposed();
            streamWriter.WriteLine(line);
            streamWriter.Flush();
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ClientCsvLogWriter));
            }
        }
    }
}