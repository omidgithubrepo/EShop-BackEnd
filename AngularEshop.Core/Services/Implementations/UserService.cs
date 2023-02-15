using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngularEshop.Core.DTOs.Account;
using AngularEshop.Core.Security;
using AngularEshop.Core.Services.Interfaces;
using AngularEshop.Core.Utilities.Convertors;
using AngularEshop.DataLayer.Entities.Access;
using AngularEshop.DataLayer.Entities.Account;
using AngularEshop.DataLayer.Repository;
using Microsoft.EntityFrameworkCore;

namespace AngularEshop.Core.Services.Implementations
{
    public class UserService : IUserService
    {
        #region constructor

        private IGenericRepository<User> userRepository;
        private IPasswordHelper passwordHelper;
        private IMailSender mailSender;
        private IViewRenderService renderView;
        private IGenericRepository<UserRole> userRoleRepository;

        public UserService(IGenericRepository<User> userRepository, IPasswordHelper passwordHelper, IMailSender mailSender, IViewRenderService renderView, IGenericRepository<UserRole> userRoleRepository)
        {
            this.userRepository = userRepository;
            this.passwordHelper = passwordHelper;
            this.mailSender = mailSender;
            this.renderView = renderView;
            this.userRoleRepository = userRoleRepository;
        }

        #endregion

        #region User Section

        public async Task<List<User>> GetAllUsers()
        {
            return await userRepository.GetEntitiesQuery().ToListAsync();
        }

        public async Task<RegisterUserResult> RegisterUser(RegisterUserDTO register)
        {
            if (IsUserExistsByEmail(register.Email))
                return RegisterUserResult.EmailExists;

            var user = new User
            {
                Email = register.Email.SanitizeText(),
                Address = register.Address.SanitizeText(),
                FirstName = register.FirstName.SanitizeText(),
                LastName = register.LastName.SanitizeText(),
                EmailActiveCode = Guid.NewGuid().ToString(),
                Password = passwordHelper.EncodePasswordMd5(register.Password)
            };

            await userRepository.AddEntity(user);

            await userRepository.SaveChanges();

            var body = await renderView.RenderToStringAsync("Email/ActivateAccount", user);

            mailSender.Send("mohammad1375ordo@gmail.com", "test", body);

            return RegisterUserResult.Success;
        }

        public bool IsUserExistsByEmail(string email)
        {
            return userRepository.GetEntitiesQuery().Any(s => s.Email == email.ToLower().Trim());
        }

        public async Task<LoginUserResult> LoginUser(LoginUserDTO login, bool checkAdminRole = false)
        {
            var password = passwordHelper.EncodePasswordMd5(login.Password);

            var user = await userRepository.GetEntitiesQuery()
                .SingleOrDefaultAsync(s => s.Email == login.Email.ToLower().Trim() && s.Password == password);

            if (user == null) return LoginUserResult.IncorrectData;

            if (!user.IsActivated) return LoginUserResult.NotActivated;

            if (checkAdminRole)
            {
                if (!await IsUserAdmin(user.Id))
                {
                    return LoginUserResult.NotAdmin;
                }
            }

            return LoginUserResult.Success;
        }

        public async Task<User> GetUserByEmail(string email)
        {
            return await userRepository.GetEntitiesQuery().SingleOrDefaultAsync(s => s.Email == email.ToLower().Trim());
        }

        public async Task<User> GetUserByUserId(long userId)
        {
            return await userRepository.GetEntityById(userId);
        }

        public void ActivateUser(User user)
        {
            user.IsActivated = true;
            user.EmailActiveCode = Guid.NewGuid().ToString();
            userRepository.UpdateEntity(user);
            userRepository.SaveChanges();
        }

        public Task<User> GetUserByEmailActiveCode(string emailActiveCode)
        {
            return userRepository.GetEntitiesQuery().SingleOrDefaultAsync(s => s.EmailActiveCode == emailActiveCode);
        }

        public async Task EditUserInfo(EditUserDTO user, long userId)
        {
            var mainUser = await userRepository.GetEntityById(userId);
            if (mainUser != null)
            {
                mainUser.FirstName = user.FirstName;
                mainUser.LastName = user.LastName;
                mainUser.Address = user.Address;
                userRepository.UpdateEntity(mainUser);
                await userRepository.SaveChanges();
            }
        }

        public async Task<bool> IsUserAdmin(long userId)
        {
            return await userRoleRepository.GetEntitiesQuery()
                .Include(s => s.Role)
                .AsQueryable().AnyAsync(s => s.UserId == userId && s.Role.Name == "Admin");
        }

        #endregion

        #region dispose

        public void Dispose()
        {
            userRepository?.Dispose();
        }

        #endregion
    }
}