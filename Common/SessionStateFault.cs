using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class SessionStateFault
    {
        [DataMember] public string Message { get; set; }

        public SessionStateFault() { }
        public SessionStateFault(string message) { Message = message; }
    }
}