using Client.Readers;
using Common;
using Common.Enums;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel;
using System.Threading;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ChannelFactory<ISession> factory = null;
            ISession proxy = null;

            Console.Title = "PMSM Motor Monitoring - Klijent";

            string csvPath = ConfigurationManager.AppSettings["Csv_path"];
            string csvLogPath = ConfigurationManager.AppSettings["Csv_log_path"];
            int maxRows = int.Parse(ConfigurationManager.AppSettings["Max_rows"]);


            try
            {
               
                List<MotorSample> samples;
                WriteInfo("Klijent je spreman za slanje podataka.");
                using (MotorCsvReader reader = new MotorCsvReader(csvPath, csvLogPath, maxRows))
                {
                    WriteInfo($"Učitavanje podataka iz: {csvPath}");
                    samples = reader.ReadFirstParsableSamples();
                    WriteSuccess($"Učitano validnih uzoraka: {samples.Count}");

                    factory = new ChannelFactory<ISession>("SessionService");
                    proxy = factory.CreateChannel();

                    Meta meta = new Meta(true, true, true, true, true, true, true);

                    SessionResponse startResponse = proxy.StartSession(meta);
                    WriteResponse("START", startResponse);

                    int counter = 0;
                    Console.WriteLine();
                    WriteSection($"SLANJE UZORAKA ({samples.Count})");
                    Thread.Sleep(500);

                    foreach (MotorSample sample in samples)
                    {
                        counter++;

                        SessionResponse response = proxy.PushSample(sample);
                        WriteSampleResponse(counter, samples.Count, response);

                        Thread.Sleep(100);
                    }

                    SessionResponse endResponse = proxy.EndSession();
                    Console.WriteLine();
                    WriteResponse("KRAJ", endResponse);

                    ((IClientChannel)proxy).Close();
                    factory.Close();
                }

                
            }
            catch (Exception e)
            {
                WriteError(e.Message);

                if (proxy != null)
                {
                    ((IClientChannel)proxy).Abort();
                    WriteWarning("WCF kanal je prinudno zatvoren.");
                }

                if (factory != null)
                {
                    factory.Abort();
                    WriteWarning("ChannelFactory je prinudno zatvoren.");
                }
            }

            Console.WriteLine();
            WriteInfo("Pritisnite ENTER za izlaz.");
            Console.ReadLine();

            /**SIMULACIJA PREKIDA
             * Console.WriteLine("\n--- TEST: Dispose pattern pri prekidu veze ---");
            Console.WriteLine("Pokretanje testa...");

            MotorCsvReader testReader = null;
            ChannelFactory<ISession> testFactory = null;
            ISession testProxy = null;

            try
            {
                testReader = new MotorCsvReader(csvPath, csvLogPath, maxRows);
                var testSamples = testReader.ReadFirstParsableSamples();

                testFactory = new ChannelFactory<ISession>("SessionService");
                testProxy = testFactory.CreateChannel();

                testProxy.StartSession(new Meta(true, true, true, true, true, true, true));

                int i = 0;
                foreach (MotorSample sample in testSamples)
                {
                    i++;
                    testProxy.PushSample(sample);
                    Console.WriteLine($"Test sample #{i} poslan.");

                    if (i == 5)
                    {
                        throw new CommunicationException("Simulirani prekid veze na sample #5.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Uhvaćen izuzetak: {ex.Message}");
                Console.WriteLine("Pozivam Dispose...");

                if (testProxy != null)
                {
                    ((IClientChannel)testProxy).Abort();
                }

                if (testFactory != null)
                {
                    testFactory.Abort();
                }
            }
            finally
            {
                testReader?.Dispose();
                Console.WriteLine("Test završen.");
            }

            Console.ReadLine();**/
        }

        private static void WriteSection(string title)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"--- {title} ---");
            Console.ResetColor();
        }

        private static void WriteInfo(string message)
        {
            WriteColored("[INFO] ", message, ConsoleColor.Gray);
        }

        private static void WriteSuccess(string message)
        {
            WriteColored("[ OK ] ", message, ConsoleColor.Green);
        }

        private static void WriteWarning(string message)
        {
            WriteColored("[WARN] ", message, ConsoleColor.Yellow);
        }

        private static void WriteError(string message)
        {
            WriteColored("[ERR ] ", message, ConsoleColor.Red);
        }

        private static void WriteResponse(string operation, SessionResponse response)
        {
            ConsoleColor color = response.Message == ServerMessage.ACK
                ? ConsoleColor.Green
                : ConsoleColor.Yellow;

            WriteColored(
                $"[{operation,-5}] ",
                $"{response.Message,-4} | Status: {response.Status,-11} | {response.Details}",
                color);
        }

        private static void WriteSampleResponse(int number, int total, SessionResponse response)
        {
            ConsoleColor color = response.Message == ServerMessage.ACK
                ? ConsoleColor.Green
                : ConsoleColor.Yellow;

            WriteColored(
                $"[{number,3}/{total,-3}] ",
                $"{response.Message,-4} | {response.Details}",
                color);
        }

        private static void WriteColored(string prefix, string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(prefix);
            Console.ResetColor();
            Console.WriteLine(message);
        }
    }
}

