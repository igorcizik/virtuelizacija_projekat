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
        [FaultContract(typeof(SessionStateFault))]
        SessionResponse StartSession(Meta meta);

        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(SessionStateFault))]
        SessionResponse PushSample(MotorSample sample);

        [OperationContract]
        [FaultContract(typeof(SessionStateFault))]
        SessionResponse EndSession();
    }
}