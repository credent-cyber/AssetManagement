using AssetManagement.Dto.Auth;
using AssetManagement.Roles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AssetManagement.Client.Services.Contracts
{
    public interface IAuthorizeApi
    {
        Task Login(LoginParameters loginParameters);
        Task Register(RegisterParameters registerParameters);
        Task Logout();
        Task<UserInfo> GetUserInfo();
        Task<List<UserViewModel>> GetUsers();
        Task UpdateUserRole(UserViewModel userViewModel);
        Task<int> GetUserCount();
        Task<List<string>> GetRoles();
        Task ChangePassword(ResetPassword resetPassword);
        Task UpdateUserDetails(UserDetailsUpdateParameters updateParameters);
        Task RequestPasswordResetByEmail(ResetPasswordByAdmin Parameters);
    }
}
