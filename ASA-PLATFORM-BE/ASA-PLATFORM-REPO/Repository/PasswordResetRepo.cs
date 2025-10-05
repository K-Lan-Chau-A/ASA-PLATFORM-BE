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
    public class PasswordResetRepo : GenericRepository<PasswordResetOtp>
    {
        public PasswordResetRepo(ASAPLATFORMDBContext context) : base(context)
        {
        }
        private static DateTime GetVietnamNow()
        {
            var utcNow = DateTime.UtcNow;
            var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            return DateTime.SpecifyKind(vietnamTime, DateTimeKind.Unspecified);
        }
        public async Task<PasswordResetOtp> GetValidOtpAsync(string otp)
        {
            var now = GetVietnamNow();

            return await _context.PasswordResetOtps.FirstOrDefaultAsync(o => o.Otp == otp && !o.IsUsed && o.ExpiredAt > now);
        }
        public async Task MarkOtpAsUsedAsync(PasswordResetOtp otp)
        {
            otp.IsUsed = true;
            _context.PasswordResetOtps.Update(otp);
            await _context.SaveChangesAsync();
        }
    }
}
