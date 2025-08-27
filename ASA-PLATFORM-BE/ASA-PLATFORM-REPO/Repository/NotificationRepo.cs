using ASA_PLATFORM_REPO.DBContext;
using ASA_PLATFORM_REPO.Models;
using EDUConnect_Repositories.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_REPO.Repository
{
    public class NotificationRepo : GenericRepository<Notification>
    {
        public NotificationRepo(ASAPLATFORMDBContext context) : base(context)
        {
        }
    }
}
