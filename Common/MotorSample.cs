using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class MotorSample
    {
        [DataMember] public double Stator_Winding { get; set; }
        [DataMember] public double Stator_Tooth { get; set; }
        [DataMember] public double Stator_Yoke { get; set; }
        [DataMember] public double PM { get; set; }
        [DataMember] public int Profile_ID { get; set; }
        [DataMember] public double Ambient { get; set; }
        [DataMember] public double Torque { get; set; }

        public MotorSample() { }

        public MotorSample(double stator_Winding, double stator_Tooth, double stator_Yoke, double pM, int profile_ID, double ambient, double torque)
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