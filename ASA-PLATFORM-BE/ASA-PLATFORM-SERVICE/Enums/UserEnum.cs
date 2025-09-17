using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.Enums
{
    public enum UserStatus
    {
        [Description("InActive")]
        Inactive = 0,

        [Description("Active")]
        Active = 1
    }

    public enum UserRole
    {
        [Description("Admin")]
        Admin = 1,

        [Description("Staff")]
        Staff = 2
    }
}
