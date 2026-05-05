using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    [ServiceContract]
    public interface ISession
    {
        [OperationContract]
        public bool StartSession(Meta meta);

        [OperationContract]
        public bool PushSample(MotorSample sample);

        [OperationContract]
        public bool EndSession();
    }
}
