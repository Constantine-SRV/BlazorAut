using BlazorAut.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorAut.Services
{
    public class GetUserRolesService
    {
        private readonly ApplicationDbContext _context;

        public GetUserRolesService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<string>> GetUserRolesByEmail(string email)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .SingleOrDefaultAsync(u => u.Email == email);

            if (user == null || user.UserRoles == null || user.UserRoles.Count == 0)
            {
                return new List<string> { "Guest" };
            }

            return user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
        }
    }
}
