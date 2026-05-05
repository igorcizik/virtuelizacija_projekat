using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Common.Enums;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Channels;
namespace Service
{
    public class SessionService : Common.ISession
    {
        private static int broj = 0;
        private static double tekucaSumaW = 0;
        private static double tekucaSumaT = 0;
        private static double tekucaSumaPM = 0;
        private static int tekuciBrojSemplova = 0;

        public ServerMessage StartSession(Meta meta)
        {
            throw new NotImplementedException();
        }

        public ServerMessage PushSample(MotorSample sample)
        {
            tekucaSumaW += sample.Stator_Winding;
            tekucaSumaT += sample.Stator_Tooth;
            tekucaSumaPM += sample.PM;
            broj++;
            var w_threshold = ConfigurationManager.AppSettings["Stator_w_threshold"];
            var t_threshold = ConfigurationManager.AppSettings["Stator_t_threshold"];
            var pm_threshold = ConfigurationManager.AppSettings["PM_threshold"];



            if ((sample.Stator_Winding > Double.Parse(w_threshold)) || (sample.Stator_Winding * 0.75 < tekucaSumaW / broj) || (sample.Stator_Winding * 1.25 > tekucaSumaW / broj))
            {
                throw new FaultException<ValidationFault>(new ValidationFault { Message = "Invalid input for stator winding" });

            }
            else if (sample.Stator_Tooth > Double.Parse(t_threshold) || (sample.Stator_Tooth * 0.75 < tekucaSumaT / broj) || (sample.Stator_Tooth * 1.25 > tekucaSumaT / broj))
            {
                throw new FaultException<ValidationFault>(new ValidationFault { Message = "Invalid input for stator tooth" });
            }
            else if(sample.PM > Double.Parse(pm_threshold) || (sample.PM * 0.75 < tekucaSumaPM / broj) || (sample.PM * 1.25 > tekucaSumaPM / broj))
            {
                throw new FaultException<ValidationFault>(new ValidationFault { Message = "Invalid input for PM" });
            }

            return ServerMessage.ACK;

        }

        public ServerMessage EndSession()
        {
            throw new NotImplementedException();
        }
    }

}
