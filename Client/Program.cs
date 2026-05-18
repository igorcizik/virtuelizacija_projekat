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

            try
            {
                string csvPath = ConfigurationManager.AppSettings["Csv_path"];
                string csvLogPath = ConfigurationManager.AppSettings["Csv_log_path"];
                int maxRows = int.Parse(ConfigurationManager.AppSettings["Max_rows"]);

                List<MotorSample> samples;
                Console.WriteLine("Client spreman za slanje podataka");
                using (MotorCsvReader reader = new MotorCsvReader(csvPath, csvLogPath, maxRows))
                {
                    Console.WriteLine("Ucitavanje podataka...");
                    samples = reader.ReadFirstValidSamples();

                    factory = new ChannelFactory<ISession>("SessionService");
                    proxy = factory.CreateChannel();

                    Meta meta = new Meta(true, true, true, true, true, true, true);

                    ServerMessage startMessage = proxy.StartSession(meta);
                    Console.WriteLine("StartSession: " + startMessage);

                    int counter = 0;
                    Console.WriteLine("Slanje podataka...");
                    Thread.Sleep(500);
                    foreach (MotorSample sample in samples)
                    {
                        counter++;

                        Console.WriteLine();
                        Console.WriteLine($"Šaljem sample #{counter}...");

                        ServerMessage response = proxy.PushSample(sample);

                        Console.WriteLine($"Odgovor za sample #{counter}: " + response);

                        Thread.Sleep(100);
                    }

                    ServerMessage endMessage = proxy.EndSession();
                    Console.WriteLine("EndSession: " + endMessage);

                    ((IClientChannel)proxy).Close();
                    factory.Close();
                }

                
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: " + e.Message);

                if (proxy != null)
                {
                    ((IClientChannel)proxy).Abort();
                    Console.WriteLine("WCF kanal je zatvoren preko Abort.");
                }

                if (factory != null)
                {
                    factory.Abort();
                    Console.WriteLine("ChannelFactory je zatvoren preko Abort.");
                }
            }

            Console.ReadLine();
        }
    }
}