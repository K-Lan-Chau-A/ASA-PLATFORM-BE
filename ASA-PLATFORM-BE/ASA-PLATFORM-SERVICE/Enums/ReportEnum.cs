using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.Enums
{
    public enum ReportType
    {
        [Description("Tuần")]
        Weekly = 1,

        [Description("Tháng")]
        Monthly = 2
    }
}
