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
            var query = _context.Shops.Include(s => s.Orders).AsQueryable();

            if (filter.ShopId > 0)
                query = query.Where(c => c.ShopId == filter.ShopId);

            if (!string.IsNullOrEmpty(filter.ShopName))
                query = query.Where(c => c.ShopName.ToLower().Contains(filter.ShopName.ToLower()));
            if (!string.IsNullOrEmpty(filter.Fullname))
                query = query.Where(c => c.Fullname.ToLower().Contains(filter.Fullname.ToLower()));
            if (!string.IsNullOrEmpty(filter.Phonenumber))
                query = query.Where(c => c.Phonenumber.Contains(filter.Phonenumber));
            if (!string.IsNullOrEmpty(filter.Address))
                query = query.Where(c => c.Address.Contains(filter.Address));
            if (filter.CreatedAt.HasValue)
                query = query.Where(p => p.CreatedAt <= filter.CreatedAt.Value);

            if (filter.UpdatedAt.HasValue)
                query = query.Where(p => p.UpdatedAt <= filter.UpdatedAt.Value);

            if (filter.Status.HasValue)
                query = query.Where(p => p.Status == filter.Status.Value);

            return query.OrderBy(c => c.ShopId);
        }

        public async Task<Shop?> GetShopById(long id)
        {
            return await _context.Shops.FirstOrDefaultAsync(u => u.ShopId == id);
        }

        public async Task<List<ShopTrialDto>> CheckTrialShops()
        {
            var today = DateTime.UtcNow.Date;

            var shops = await _context.Shops
                .Where(s => s.Status == 2) // trial
                .ToListAsync();

            var needNotify = new List<ShopTrialDto>();

            foreach (var shop in shops)
            {
                if (!shop.CreatedAt.HasValue) continue;

                var daysUsed = (int)(today - shop.CreatedAt.Value.Date).TotalDays;
                var trialDays = 15; // ví dụ mặc định 15 ngày trial
                var daysLeft = trialDays - daysUsed;

                if (daysUsed >= trialDays)
                {
                    shop.Status = 0; // hết hạn -> Inactive
                    shop.UpdatedAt = DateTime.UtcNow;
                }
                else if (daysUsed == 12 || daysUsed == 13 || daysUsed == 14)
                {
                    needNotify.Add(new ShopTrialDto
                    {
                        ShopId = shop.ShopId,
                        ShopName = shop.ShopName,
                        Fullname = shop.Fullname,
                        Email = shop.Email,
                        DaysLeft = daysLeft
                    });
                }
            }

            await _context.SaveChangesAsync();

            return needNotify;
        }


        public class ShopTrialDto
        {
            public long ShopId { get; set; }
            public string ShopName { get; set; }
            public string Fullname { get; set; }
            public string Email { get; set; }
            public int DaysLeft { get; set; }
        }

    }
}
