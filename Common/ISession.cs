using Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [ServiceContract]
    public interface ISession
    {
        [OperationContract]
        ServerMessage StartSession(Meta meta);

        [OperationContract]
        ServerMessage PushSample(MotorSample sample);

        [OperationContract]
        ServerMessage EndSession();
    }
}
