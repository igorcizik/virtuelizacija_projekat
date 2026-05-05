using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;


namespace Common
{
    [DataContract]
    public class MotorSample
    {
        [DataMember]
        public double Stator_Winding { get; set; }
        [DataMember]
        public double Stator_Tooth { get; set; }
        [DataMember]
        public double Stator_Yoke { get; set; }
        [DataMember]
        public double PM {  get; set; }
        [DataMember]
        public double Profile_ID {  get; set; }
        [DataMember]
        public double Ambient {  get; set; }
        [DataMember]
        public double Torque { get; set; }

        public MotorSample(double stator_Winding, double stator_Tooth, double stator_Yoke, double pM, double profile_ID, double ambient, double torque)
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
