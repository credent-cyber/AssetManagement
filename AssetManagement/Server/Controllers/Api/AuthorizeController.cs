
using AssetManagement.Dto.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AssetManagement.DataContext.Models;
using Microsoft.EntityFrameworkCore;
using AssetManagement.Roles;

namespace AssetManagement.Server.Controllers.Api
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthorizeController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public AuthorizeController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginParameters parameters)
        {
            //var user = await _userManager.FindByNameAsync(parameters.UserName);
            var user = await _userManager.FindByEmailAsync(parameters.UserEmail);
            if (user == null) return BadRequest("User does not exist");
            var singInResult = await _signInManager.CheckPasswordSignInAsync(user, parameters.Password, false);
            if (!singInResult.Succeeded) return BadRequest("Invalid password");

            await _signInManager.SignInAsync(user, parameters.RememberMe);

            return Ok();
        }


        [HttpPost]
        public async Task<IActionResult> Register(RegisterParameters parameters)
        {
            var user = new ApplicationUser
            {
                UserName = parameters.UserName,
                Email = parameters.Email
            };

            var userCount = await _userManager.Users.CountAsync();

            if (userCount == 0)
            {
                var adminRole = await _roleManager.FindByNameAsync("Admin");
                if (adminRole == null)
                {
                    adminRole = new IdentityRole<Guid>("Admin");
                    var createAdminRoleResult = await _roleManager.CreateAsync(adminRole);
                    if (!createAdminRoleResult.Succeeded)
                        return BadRequest(createAdminRoleResult.Errors.FirstOrDefault()?.Description);
                }

                var result = await _userManager.CreateAsync(user, parameters.Password);
                if (!result.Succeeded)
                    return BadRequest(result.Errors.FirstOrDefault()?.Description);

                var addAdminRoleResult = await _userManager.AddToRoleAsync(user, "Admin");
                if (!addAdminRoleResult.Succeeded)
                    return BadRequest(addAdminRoleResult.Errors.FirstOrDefault()?.Description);
            }
            else
            {
                var userRole = await _roleManager.FindByNameAsync("User");
                if (userRole == null)
                {
                    userRole = new IdentityRole<Guid>("User");
                    var createUserRoleResult = await _roleManager.CreateAsync(userRole);
                    if (!createUserRoleResult.Succeeded)
                        return BadRequest(createUserRoleResult.Errors.FirstOrDefault()?.Description);
                }

                var result = await _userManager.CreateAsync(user, parameters.Password);
                if (!result.Succeeded)
                    return BadRequest(result.Errors.FirstOrDefault()?.Description);

                var addUserRoleResult = await _userManager.AddToRoleAsync(user, "User");
                if (!addUserRoleResult.Succeeded)
                    return BadRequest(addUserRoleResult.Errors.FirstOrDefault()?.Description);
            }

            return await Login(new LoginParameters
            {
                //UserName = parameters.UserName,
                UserEmail = parameters.Email,
                Password = parameters.Password
            });
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok();
        }

        [HttpGet]
        public UserInfo UserInfo()
        {
            //var user = await _userManager.GetUserAsync(HttpContext.User);
            return BuildUserInfo();
        }


        private UserInfo BuildUserInfo()
        {
            return new UserInfo
            {
                IsAuthenticated = User.Identity.IsAuthenticated,
                UserName = User.Identity.Name,
                ExposedClaims = User.Claims
                    //Optionally: filter the claims you want to expose to the client
                    //.Where(c => c.Type == "test-claim")
                    .ToDictionary(c => c.Type, c => c.Value)
            };
        }

        //[Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userManager.Users.ToListAsync();

            var userViewModels = users.Select(u => new UserViewModel
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                Role = _userManager.GetRolesAsync(u).Result.FirstOrDefault()
            }).ToList();

            return Ok(userViewModels);
        }

        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateUserRole(UserViewModel userViewModel)
        {
            var user = await _userManager.FindByIdAsync(userViewModel.Id.ToString());

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            // Remove all roles from the user
            var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, roles);
            if (!removeRolesResult.Succeeded)
            {
                return BadRequest(removeRolesResult.Errors.FirstOrDefault()?.Description);
            }

            // Add the new role to the user
            var role = await _roleManager.FindByNameAsync(userViewModel.Role);
            if (role == null)
            {
                role = new IdentityRole<Guid>(userViewModel.Role);
                var createRoleResult = await _roleManager.CreateAsync(role);
                if (!createRoleResult.Succeeded)
                {
                    return BadRequest(createRoleResult.Errors.FirstOrDefault()?.Description);
                }
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, userViewModel.Role);
            if (!addRoleResult.Succeeded)
            {
                return BadRequest(addRoleResult.Errors.FirstOrDefault()?.Description);
            }

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> UserCount()
        {
            var userCount = await _userManager.Users.CountAsync();

            return Ok(new { Count = userCount });
        }


        [HttpGet]
        public IActionResult GetRoles()
        {
            var roles = _roleManager.Roles.Select(r => r.Name).ToList();
            return Ok(roles);
        }

    }

}

