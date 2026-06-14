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
        private static readonly string[] RequiredColumns =
        {
            "stator_winding", "stator_tooth", "stator_yoke", "pm", "profile_id", "ambient", "torque"
        };

        private TextReader textReader;
        private ClientCsvLogWriter logWriter;
        private bool disposed;

        public string CsvPath { get; }
        private string LogPath { get; }
        private int MaxValidRows { get; }

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

            CsvPath = csvPath;
            LogPath = logPath;
            MaxValidRows = maxValidRows;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            textReader?.Dispose();
            logWriter?.Dispose();
            textReader = null;
            logWriter = null;
            disposed = true;
            Console.WriteLine("CSV reader je zatvoren.");
        }

        public List<MotorSample> ReadFirstParsableSamples()
        {
            ThrowIfDisposed();

            var samples = new List<MotorSample>(MaxValidRows);
            textReader = File.OpenText(CsvPath);
            logWriter = new ClientCsvLogWriter(LogPath);
            logWriter.WriteInfo("Početak učitavanja CSV fajla.");

            string headerLine = textReader.ReadLine();
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                throw new InvalidDataException("CSV fajl nema zaglavlje.");
            }

            Dictionary<string, int> indexes = GetRequiredColumnIndexes(headerLine.Split(','));
            string line;
            int rowNumber = 1;
            int readRows = 0;

            while (readRows < MaxValidRows && (line = textReader.ReadLine()) != null)
            {
                rowNumber++;
                readRows++;

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

        private static Dictionary<string, int> GetRequiredColumnIndexes(string[] headers)
        {
            return RequiredColumns.ToDictionary(column => column, column => FindColumn(headers, column));
        }

        private static int FindColumn(string[] headers, string columnName)
        {
            int index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
            if (index == -1)
            {
                throw new InvalidDataException($"CSV fajl ne sadrži obaveznu kolonu: {columnName}");
            }

            return index;
        }

        private static bool TryParseSample(string line, Dictionary<string, int> indexes, out MotorSample sample, out string reason)
        {
            sample = null;
            reason = string.Empty;

            if (string.IsNullOrWhiteSpace(line))
            {
                reason = "Prazan red.";
                return false;
            }

            string[] parts = line.Split(',');
            if (parts.Length <= indexes.Values.Max())
            {
                reason = "Red nema dovoljan broj kolona.";
                return false;
            }

            double statorWinding, statorTooth, statorYoke, pm, ambient, torque;
            int profileId;

            if (!TryParseDouble(parts, indexes, "stator_winding", out statorWinding, out reason) ||
                !TryParseDouble(parts, indexes, "stator_tooth", out statorTooth, out reason) ||
                !TryParseDouble(parts, indexes, "stator_yoke", out statorYoke, out reason) ||
                !TryParseDouble(parts, indexes, "pm", out pm, out reason) ||
                !TryParseInt(parts, indexes, "profile_id", out profileId, out reason) ||
                !TryParseDouble(parts, indexes, "ambient", out ambient, out reason) ||
                !TryParseDouble(parts, indexes, "torque", out torque, out reason))
            {
                return false;
            }

            sample = new MotorSample(statorWinding, statorTooth, statorYoke, pm, profileId, ambient, torque);
            return true;
        }

        private static bool TryParseDouble(string[] parts, Dictionary<string, int> indexes, string column, out double result, out string reason)
        {
            bool parsed = double.TryParse(parts[indexes[column]].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
            reason = parsed ? string.Empty : "Nevalidna vrednost za " + column + ".";
            return parsed;
        }

        private static bool TryParseInt(string[] parts, Dictionary<string, int> indexes, string column, out int result, out string reason)
        {
            bool parsed = int.TryParse(parts[indexes[column]].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
            reason = parsed ? string.Empty : "Nevalidna vrednost za " + column + ".";
            return parsed;
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

