using System;
using System.Collections.Generic;
using System.Text;

namespace MigrateIoTDeviceTwinToADX.Models
{
    public class DeviceGroup
    {
        public string Id { get; set; }

        public string DisplayName { get; set; }

        public IEnumerable<DeviceGroupCondition> Conditions { get; set; }
    }
}