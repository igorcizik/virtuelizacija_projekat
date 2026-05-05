using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
        public class Meta
        {
            public double Stator_Winding { get; set; }
            public double Stator_Tooth { get; set; }
            public double Stator_Yoke { get; set; }
            public double PM { get; set; }
            public double Profile_ID { get; set; }
            public double Ambient { get; set; }
            public double Torque { get; set; }

            public Meta(double stator_Winding, double stator_Tooth, double stator_Yoke, double pM, double profile_ID, double ambient, double torque)
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

