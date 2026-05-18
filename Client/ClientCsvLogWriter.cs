using System;
using System.IO;

namespace Client.Logs
{
    public class ClientCsvLogWriter : IDisposable
    {
        private TextWriter textWriter;
        private bool disposed = false;
        private readonly string path;

        public string Path
        {
            get { return path; }
        }

        public ClientCsvLogWriter(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Putanja log fajla nije validna.");
            }

            this.path = path;

            string directory = System.IO.Path.GetDirectoryName(path);

            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            textWriter = File.AppendText(path);
        }

        ~ClientCsvLogWriter()
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
                    if (textWriter != null)
                    {
                        textWriter.Dispose();
                        textWriter = null;
                        Console.WriteLine("[DISPOSE] CSV log writer je zatvoren.");
                    }
                }

                disposed = true;
            }
        }

        public void WriteInvalidRow(int rowNumber, string line, string reason)
        {
            ThrowIfDisposed();
            textWriter.WriteLine($"INVALID ROW | Row: {rowNumber} | Reason: {reason} | Data: {line}");
            textWriter.Flush();
        }

        public void WriteExcessRow(int rowNumber, string line)
        {
            ThrowIfDisposed();
            textWriter.WriteLine($"EXCESS ROW | Row: {rowNumber} | Data: {line}");
            textWriter.Flush();
        }

        public void WriteInfo(string message)
        {
            ThrowIfDisposed();
            textWriter.WriteLine($"INFO | {DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}");
            textWriter.Flush();
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