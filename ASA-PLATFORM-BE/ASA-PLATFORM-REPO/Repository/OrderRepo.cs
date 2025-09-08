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
    public class OrderRepo : GenericRepository<Order>
    {
        public OrderRepo(ASAPLATFORMDBContext context) : base(context)
        {
        }
        public IQueryable<Order> GetFiltered(Order filter)
        {
            var query = _context.Orders.AsQueryable();

            if (filter.OrderId > 0)
                query = query.Where(c => c.OrderId == filter.OrderId);
            if (!string.IsNullOrEmpty(filter.Note))
                query = query.Where(c => c.Note.Contains(filter.Note));
            if (filter.TotalPrice.HasValue)
                query = query.Where(p => p.TotalPrice <= filter.TotalPrice);
            if (filter.CreatedAt.HasValue)
                query = query.Where(p => p.CreatedAt <= filter.CreatedAt);
            if (filter.ExpiredAt.HasValue)
                query = query.Where(p => p.ExpiredAt <= filter.ExpiredAt);
            if (filter.Status.HasValue)
                query = query.Where(p => p.Status == filter.Status);
            if(filter.Discount.HasValue)
                query = query.Where(p => p.Discount <= filter.Discount);
            if(filter.PaymentMethod != null)
                query = query.Where(p => p.PaymentMethod == filter.PaymentMethod);
            if (filter.UserId > 0)
                query = query.Where(p => p.UserId == filter.UserId);
            if (filter.ShopId > 0)
                query = query.Where(p => p.ShopId == filter.ShopId);
            if (filter.ProductId > 0)
                query = query.Where(p => p.ProductId == filter.ProductId);


            return query.OrderBy(c => c.OrderId);
        }

        public async Task<Order?> GetCurrentShopProduct(long shopId) 
        {
            // Assumption: Status 2 = order success and not expired (currently are using)
            return await _context.Orders
                .Include(o => o.Product)
                .Where(o => o.ShopId == shopId
                        && o.Status == 2)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
