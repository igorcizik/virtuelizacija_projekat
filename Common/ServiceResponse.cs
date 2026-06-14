using Common.Enums;
using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class ServiceResponse
    {
        [DataMember] public SessionStatus status { get; set; }
        [DataMember] public ServerMessage ack { get; set; }
        [DataMember] public string message { get; set; }
    }
}