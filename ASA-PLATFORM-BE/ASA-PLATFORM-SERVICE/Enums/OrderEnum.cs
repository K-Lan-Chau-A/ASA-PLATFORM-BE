using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.Enums
{
    public enum OrderStatus
    {
        [Description("Thành công")]
        Success = 1,

        [Description("Thất bại")]
        Failed = 2
    }
}
