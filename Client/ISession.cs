using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Common;
namespace Client
{
    [ServiceContract]
    public interface ISession
    {
        [OperationContract]
        bool StartSession(Meta meta);

        [OperationContract]
        bool PushSample(MotorSample sample);

        [OperationContract]
        bool EndSession();
    }
}
