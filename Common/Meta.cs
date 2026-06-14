using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class Meta
    {
        [DataMember] public bool Stator_Winding { get; set; }
        [DataMember] public bool Stator_Tooth { get; set; }
        [DataMember] public bool Stator_Yoke { get; set; }
        [DataMember] public bool PM { get; set; }
        [DataMember] public bool Profile_ID { get; set; }
        [DataMember] public bool Ambient { get; set; }
        [DataMember] public bool Torque { get; set; }

        public Meta() { }

        public Meta(bool stator_Winding, bool stator_Tooth, bool stator_Yoke, bool pM, bool profile_ID, bool ambient, bool torque)
        {
            Stator_Winding = stator_Winding;
            Stator_Tooth = stator_Tooth;
            Stator_Yoke = stator_Yoke;
            PM = pM;
            Profile_ID = profile_ID;
            Ambient = ambient;
            Torque = torque;
        }
    }
}