using System;
using System.Collections.Generic;
using System.Text;

namespace EC2ScheduleAgent
{
    public class ControlRequest
    {
        public enum EnumAction
        {
            OFF,
            ON
        }
        public string InstanceID { get; set; }
        public EnumAction Action { get; set; }
    }
}
