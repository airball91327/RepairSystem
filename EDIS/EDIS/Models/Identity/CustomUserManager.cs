using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using EDIS.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;


namespace EDIS.Models.Identity
{
    public class CustomUserManager : UserManager<ApplicationUser>
    {
        public IUserStore<ApplicationUser> _userStore;
        private readonly ApplicationDbContext _context;
        public CustomUserManager(ApplicationDbContext context, IUserStore<ApplicationUser> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<ApplicationUser> passwordHasher, IEnumerable<IUserValidator<ApplicationUser>> userValidators, IEnumerable<IPasswordValidator<ApplicationUser>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<ApplicationUser>> logger) 
            : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
        {
            _context = context;
            _userStore = store;
        }

        public new Task<ApplicationUser> FindByNameAsync(string userName)
        {
            return _userStore.FindByNameAsync(userName, CancellationToken);
        }

        public override string GetUserName(ClaimsPrincipal principal)
        {
            return base.GetUserName(principal);
        }

        public string GetUserFullName(ClaimsPrincipal principal)
        {
            var userName = principal.FindFirstValue(Options.ClaimsIdentity.UserNameClaimType);
            var userId = _userStore.FindByNameAsync(userName, CancellationToken).Result.Id;
            var userFullName = _context.AppUsers.Find(Convert.ToInt32(userId)).FullName;
            return userFullName;
        }

        public int GetCurrentUserId(ClaimsPrincipal principal)
        {
            var userName = principal.FindFirstValue(Options.ClaimsIdentity.UserNameClaimType);
            var userId = _userStore.FindByNameAsync(userName, CancellationToken).Result.Id;
            return Convert.ToInt32(userId);
        }

        public bool IsInRole(ClaimsPrincipal principal, string roleName)
        {
            var userId = Convert.ToInt32(principal.FindFirstValue(Options.ClaimsIdentity.UserIdClaimType));
            var userRoles = _context.UsersInRoles.Include(ur => ur.AppRoles).Include(ur => ur.AppUsers)
                                                 .Where(u => u.UserId == userId).ToList();
            foreach(var item in userRoles)
            {
                if(item.AppRoles.RoleName == roleName)
                {
                    return true;
                }

            }
            return false;
        }

        public string GetCurrentUserDptId(ClaimsPrincipal principal)
        {
            var userName = principal.FindFirstValue(Options.ClaimsIdentity.UserNameClaimType);
            var userId = _userStore.FindByNameAsync(userName, CancellationToken).Result.Id;
            var dptId = _context.AppUsers.Where(u => u.UserName == userName).FirstOrDefault().DptId;
            return dptId;
        }

        public IdentityResult AddToRole(ApplicationUser user, string roleName)
        {
            var uid = Convert.ToInt32(user.Id);
            var role = _context.AppRoles.Where(r => r.RoleName == roleName).FirstOrDefault();
            if (role != null)
            {
                UsersInRolesModel usersInRoles = new UsersInRolesModel();
                usersInRoles.UserId = uid;
                usersInRoles.UserName = user.UserName;
                usersInRoles.RoleId = role.RoleId;
                try
                {
                    _context.UsersInRoles.Add(usersInRoles);
                    _context.SaveChanges();
                    return IdentityResult.Success;
                }
                catch (Exception e)
                {
                    IdentityError[] errors = new IdentityError[1];
                    errors[0].Code = "500";
                    errors[0].Description = e.Message;
                    return IdentityResult.Failed();
                }
            }
            else
            {
                IdentityError[] errors = new IdentityError[1];
                errors[0].Code = "500";
                errors[0].Description = "無此腳色";
                return IdentityResult.Failed();
            }
        }

        public IdentityResult RemoveFromRoles(ApplicationUser user, string[] roleNames)
        {
            var uid = Convert.ToInt32(user.Id);
            foreach(var rName in roleNames)
            {
                var role = _context.AppRoles.Where(r => r.RoleName == rName).FirstOrDefault();
                if (role != null)
                {
                    UsersInRolesModel usersInRoles = _context.UsersInRoles.Find(uid, role.RoleId);
                    _context.UsersInRoles.Remove(usersInRoles);
                }
            }
            try
            {
                _context.SaveChanges();
                return IdentityResult.Success;
            }
            catch (Exception e)
            {
                IdentityError[] errors = new IdentityError[1];
                errors[0].Code = "500";
                errors[0].Description = e.Message;
                return IdentityResult.Failed();
            }

        }

    }
}
