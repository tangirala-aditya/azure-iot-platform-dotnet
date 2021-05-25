using System;
using System.Collections.Generic;
using System.Text;

namespace ResourceDataCollector.Shared
{
    public class TenantAccessInformation
    {
        public string TenantId { get; set; }
        public DateTime LastAccessDateTime { get; set; }
        public bool IsTelemetryFound { get; set; }
    }
}
