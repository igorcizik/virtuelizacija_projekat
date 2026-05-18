using Client.Logs;
using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Client.Readers
{
    public class MotorCsvReader : IDisposable
    {
        private TextReader textReader;
        private ClientCsvLogWriter logWriter;
        private bool disposed = false;

        private readonly string csvPath;
        private readonly string logPath;
        private readonly int maxValidRows;

        public string CsvPath
        {
            get { return csvPath; }
        }

        public MotorCsvReader(string csvPath, string logPath, int maxValidRows)
        {
            if (string.IsNullOrWhiteSpace(csvPath))
            {
                throw new ArgumentException("Putanja do CSV fajla nije validna.");
            }

            if (!File.Exists(csvPath))
            {
                throw new FileNotFoundException("CSV fajl ne postoji.", csvPath);
            }

            if (maxValidRows <= 0)
            {
                throw new ArgumentException("Broj redova za učitavanje mora biti veći od 0.");
            }

            this.csvPath = csvPath;
            this.logPath = logPath;
            this.maxValidRows = maxValidRows;
        }

        ~MotorCsvReader()
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
                    if (textReader != null)
                    {
                        textReader.Dispose();
                        textReader = null;
                        Console.WriteLine("[DISPOSE] CSV reader je zatvoren.");
                    }

                    if (logWriter != null)
                    {
                        logWriter.Dispose();
                        logWriter = null;
                    }
                }

                disposed = true;
            }
        }

        public List<MotorSample> ReadFirstValidSamples()
        {
            ThrowIfDisposed();

            List<MotorSample> samples = new List<MotorSample>(maxValidRows);

            textReader = File.OpenText(csvPath);
            logWriter = new ClientCsvLogWriter(logPath);

            logWriter.WriteInfo("Početak učitavanja CSV fajla.");

            string headerLine = textReader.ReadLine();

            if (string.IsNullOrWhiteSpace(headerLine))
            {
                throw new InvalidDataException("CSV fajl nema zaglavlje.");
            }

            string[] headers = headerLine.Split(',');

            Dictionary<string, int> indexes = GetRequiredColumnIndexes(headers);

            string line;
            int rowNumber = 1;

            while ((line = textReader.ReadLine()) != null)
            {
                rowNumber++;

                if (samples.Count >= maxValidRows)
                {
                    logWriter.WriteExcessRow(rowNumber, line);
                    continue;
                }

                MotorSample sample;
                string reason;

                if (TryParseSample(line, indexes, out sample, out reason))
                {
                    samples.Add(sample);
                }
                else
                {
                    logWriter.WriteInvalidRow(rowNumber, line, reason);
                }
            }

            logWriter.WriteInfo($"Završeno učitavanje. Učitano validnih redova: {samples.Count}.");

            return samples;
        }

        private Dictionary<string, int> GetRequiredColumnIndexes(string[] headers)
        {
            Dictionary<string, int> indexes = new Dictionary<string, int>();

            AddColumnIndex(headers, indexes, "stator_winding");
            AddColumnIndex(headers, indexes, "stator_tooth");
            AddColumnIndex(headers, indexes, "stator_yoke");
            AddColumnIndex(headers, indexes, "pm");
            AddColumnIndex(headers, indexes, "profile_id");
            AddColumnIndex(headers, indexes, "ambient");
            AddColumnIndex(headers, indexes, "torque");

            return indexes;
        }

        private void AddColumnIndex(string[] headers, Dictionary<string, int> indexes, string columnName)
        {
            int index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));

            if (index == -1)
            {
                throw new InvalidDataException($"CSV fajl ne sadrži obaveznu kolonu: {columnName}");
            }

            indexes[columnName] = index;
        }

        private bool TryParseSample(string line, Dictionary<string, int> indexes, out MotorSample sample, out string reason)
        {
            sample = null;
            reason = "";

            if (string.IsNullOrWhiteSpace(line))
            {
                reason = "Prazan red.";
                return false;
            }

            string[] parts = line.Split(',');

            int maxRequiredIndex = indexes.Values.Max();

            if (parts.Length <= maxRequiredIndex)
            {
                reason = "Red nema dovoljan broj kolona.";
                return false;
            }

            double statorWinding;
            double statorTooth;
            double statorYoke;
            double pm;
            double ambient;
            double torque;
            int profileId;

            if (!TryParseDouble(parts[indexes["stator_winding"]], out statorWinding))
            {
                reason = "Nevalidna vrednost za stator_winding.";
                return false;
            }

            if (!TryParseDouble(parts[indexes["stator_tooth"]], out statorTooth))
            {
                reason = "Nevalidna vrednost za stator_tooth.";
                return false;
            }

            if (!TryParseDouble(parts[indexes["stator_yoke"]], out statorYoke))
            {
                reason = "Nevalidna vrednost za stator_yoke.";
                return false;
            }

            if (!TryParseDouble(parts[indexes["pm"]], out pm))
            {
                reason = "Nevalidna vrednost za pm.";
                return false;
            }

            if (!TryParseInt(parts[indexes["profile_id"]], out profileId))
            {
                reason = "Nevalidna vrednost za profile_id.";
                return false;
            }

            if (!TryParseDouble(parts[indexes["ambient"]], out ambient))
            {
                reason = "Nevalidna vrednost za ambient.";
                return false;
            }

            if (!TryParseDouble(parts[indexes["torque"]], out torque))
            {
                reason = "Nevalidna vrednost za torque.";
                return false;
            }

            sample = new MotorSample(
                statorWinding,
                statorTooth,
                statorYoke,
                pm,
                profileId,
                ambient,
                torque
            );

            return true;
        }

        private bool TryParseDouble(string value, out double result)
        {
            return double.TryParse(
                value.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out result
            );
        }

        private bool TryParseInt(string value, out int result)
        {
            return int.TryParse(
                value.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out result
            );
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(MotorCsvReader));
            }
        }
    }
}