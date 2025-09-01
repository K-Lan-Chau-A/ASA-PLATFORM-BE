using ASA_PLATFORM_REPO.DBContext;
using ASA_PLATFORM_REPO.Models;
using EDUConnect_Repositories.Basic;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_REPO.Repository
{
    public class ShopRepo : GenericRepository<Shop>
    {
        public ShopRepo(ASAPLATFORMDBContext context) : base(context)
        {
        }
        public IQueryable<Shop> GetFiltered(Shop filter)
        {
            var query = _context.Shops.AsQueryable();

            if (filter.ShopId > 0)
                query = query.Where(c => c.ShopId == filter.ShopId);

            if (!string.IsNullOrEmpty(filter.ShopName))
                query = query.Where(c => c.ShopName.Contains(filter.ShopName));

            if (!string.IsNullOrEmpty(filter.Address))
                query = query.Where(c => c.Address.Contains(filter.Address));

            if (filter.CurrentRequest.HasValue)
                query = query.Where(p => p.CurrentRequest <= filter.CurrentRequest.Value);

            if (filter.CurrentAccount.HasValue)
                query = query.Where(p => p.CurrentAccount <= filter.CurrentAccount.Value);

            if (filter.CreatedAt.HasValue)
                query = query.Where(p => p.CreatedAt <= filter.CreatedAt.Value);

            if (filter.UpdatedAt.HasValue)
                query = query.Where(p => p.UpdatedAt <= filter.UpdatedAt.Value);

            if (filter.Status.HasValue)
                query = query.Where(p => p.Status == filter.Status.Value);

            return query.OrderBy(c => c.ShopId);
        }
    }
}
