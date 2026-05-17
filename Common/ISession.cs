using System.ServiceModel;
using Common.Enums;

namespace Common
{
    [ServiceContract]
    public interface ISession
    {
        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        ServerMessage StartSession(Meta meta);

        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        ServerMessage PushSample(MotorSample sample);

        [OperationContract]
        ServerMessage EndSession();
    }
}