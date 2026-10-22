using System.Security.Claims;

namespace JJDevHub.News.Api.Http
{
    public sealed class CurrentUser(IHttpContextAccessor http)
    {
        public string? IdentityGuid => http.HttpContext?.User.FindFirstValue("sub");
    }
}
