using System.Security.Claims;

namespace LibraryAPI.Services
{
    public interface ICurrentUserService
    {
        int? UserId { get; }
        string? Email { get; }
        string? Name { get; }
        bool IsAuthenticated { get; }
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? UserId
        {
            get
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(userIdClaim, out int userId))
                    return userId;

                return null;
            }
        }

        public string? Email
        {
            get
            {
                return _httpContextAccessor.HttpContext?.User?
                    .FindFirst(ClaimTypes.Email)?.Value;
            }
        }

        public string? Name
        {
            get
            {
                return _httpContextAccessor.HttpContext?.User?
                    .FindFirst(ClaimTypes.Name)?.Value;
            }
        }

        public bool IsAuthenticated => UserId.HasValue;
    }
}