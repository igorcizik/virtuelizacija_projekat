using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class ValidationFault
    {
        [DataMember] public string Message { get; set; }

        public ValidationFault() { }
        public ValidationFault(string message) { Message = message; }
    }
}