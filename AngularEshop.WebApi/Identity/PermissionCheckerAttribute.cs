using AngularEshop.Core.Services.Interfaces;
using AngularEshop.Core.Utilities.Common;
using AngularEshop.Core.Utilities.Extensions.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AngularEshop.WebApi.Identity
{
    public class PermissionCheckerAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private string _role;
        private IAccessService _accessService;

        public PermissionCheckerAttribute(string role)
        {
            _role = role;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            _accessService = (IAccessService)context.HttpContext.RequestServices.GetService(typeof(IAccessService));

            if (context.HttpContext.User.Identity.IsAuthenticated)
            {
                var userId = context.HttpContext.User.GetUserId();

                var result = _accessService.CheckUserRole(userId, _role).Result;

                if (!result)
                {
                    context.Result = JsonResponseStatus.NoAccess();
                }
            }
            else
            {
                context.Result = JsonResponseStatus.NoAccess();
            }
        }
    }
}
