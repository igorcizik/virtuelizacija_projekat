using Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class ServiceResponse
    {
        [DataMember]
        public SessionStatus status {  get; set; }
        [DataMember]
        public ServerMessage ack {  get; set; }

        [DataMember]
        public string message { get; set; }

    }
}
