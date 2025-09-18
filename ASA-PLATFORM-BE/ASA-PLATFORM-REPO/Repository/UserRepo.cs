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
    public class UserRepo : GenericRepository<User>
    {
        public UserRepo(ASAPLATFORMDBContext context) : base(context)
        {
        }

        public IQueryable<User> GetFiltered (User filter)
        {
            var query = _context.Users.AsQueryable();
            if (filter.UserId > 0)
                query = query.Where(u => u.UserId == filter.UserId);
            if (!string.IsNullOrEmpty(filter.Username))
                query = query.Where(u => u.Username.Contains(filter.Username));
            if (filter.Status > 0)
                query = query.Where(u => u.Status == filter.Status);
            if(!string.IsNullOrEmpty(filter.FullName))
                query = query.Where(u => u.FullName.Contains(filter.FullName));
            if (!string.IsNullOrEmpty(filter.Email))
                query = query.Where(u => u.Email.Contains(filter.Email));
            if (!string.IsNullOrEmpty(filter.PhoneNumber))
                query = query.Where(u => u.PhoneNumber.Contains(filter.PhoneNumber));
            if (filter.Role != null)
                query = query.Where(u => u.Role == filter.Role);
            if (!string.IsNullOrEmpty(filter.Avatar))
                query = query.Where(u => u.Avatar.Contains(filter.Avatar));
            return query.OrderBy(u => u.UserId);
        }

        public async Task<User?> GetUserByUsername(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }
    }
}
