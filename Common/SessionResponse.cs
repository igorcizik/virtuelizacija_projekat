using Common.Enums;
using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class SessionResponse
    {
        [DataMember] public ServerMessage Message { get; set; }
        [DataMember] public SessionStatus Status { get; set; }
        [DataMember] public string Details { get; set; }

        public SessionResponse() { }

        public SessionResponse(ServerMessage message, SessionStatus status, string details)
        {
            Message = message;
            Status = status;
            Details = details;
        }
    }
}