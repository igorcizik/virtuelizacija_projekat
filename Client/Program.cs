using Client.Readers;
using Common;
using Common.Enums;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel;

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

                using (MotorCsvReader reader = new MotorCsvReader(csvPath, csvLogPath, maxRows))
                {
                    samples = reader.ReadFirstValidSamples();
                }

                factory = new ChannelFactory<ISession>("SessionService");
                proxy = factory.CreateChannel();

                Meta meta = new Meta(true, true, true, true, true, true, true);

                ServerMessage startMessage = proxy.StartSession(meta);
                Console.WriteLine("StartSession: " + startMessage);

                foreach (MotorSample sample in samples)
                {
                    ServerMessage response = proxy.PushSample(sample);
                    Console.WriteLine("PushSample: " + response);
                }

                ServerMessage endMessage = proxy.EndSession();
                Console.WriteLine("EndSession: " + endMessage);

                ((IClientChannel)proxy).Close();
                factory.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: " + e.Message);

                if (proxy != null)
                {
                    ((IClientChannel)proxy).Abort();
                    Console.WriteLine("[DISPOSE] WCF kanal je zatvoren preko Abort.");
                }

                if (factory != null)
                {
                    factory.Abort();
                    Console.WriteLine("[DISPOSE] ChannelFactory je zatvoren preko Abort.");
                }
            }

            Console.ReadLine();
        }
    }
}