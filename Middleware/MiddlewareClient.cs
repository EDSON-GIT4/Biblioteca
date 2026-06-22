using Microsoft.Extensions.Caching.Memory;
using LibraryAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Middleware
{
    public class ClientRateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;

        public ClientRateLimitMiddleware(RequestDelegate next, IMemoryCache cache)
        {
            _next = next;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context, LibraryDbContext dbContext)
        {
            // Só aplicar para clientes autenticados
            var clientId = context.User?.FindFirst("client_id")?.Value;
            
            if (!string.IsNullOrEmpty(clientId))
            {
                var cliente = await dbContext.Clientes
                    .FirstOrDefaultAsync(c => c.ClientId == clientId);
                
                if (cliente != null)
                {
                    var clientKey = $"rate_limit_{clientId}";
                    
                    // Rate limit por minuto
                    var minuteKey = $"{clientKey}_minute_{DateTime.UtcNow.Minute}";
                    var minuteCount = _cache.GetOrCreate(minuteKey, entry =>
                    {
                        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                        return 0;
                    });
                    
                    if (minuteCount >= cliente.RateLimitPerMinute)
                    {
                        context.Response.StatusCode = 429; // Too Many Requests
                        context.Response.Headers["Retry-After"] = "60";
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = "rate_limit_exceeded",
                            message = $"Limite de {cliente.RateLimitPerMinute} requisições por minuto excedido"
                        });
                        return;
                    }
                    
                    // Rate limit por dia
                    var dayKey = $"{clientKey}_day_{DateTime.UtcNow:yyyyMMdd}";
                    var dayCount = _cache.GetOrCreate(dayKey, entry =>
                    {
                        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
                        return 0;
                    });
                    
                    if (dayCount >= cliente.RateLimitPerDay)
                    {
                        context.Response.StatusCode = 429;
                        context.Response.Headers["Retry-After"] = "86400";
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = "rate_limit_exceeded",
                            message = $"Limite diário de {cliente.RateLimitPerDay} requisições excedido"
                        });
                        return;
                    }
                    
                    // Incrementar contadores
                    _cache.Set(minuteKey, minuteCount + 1, TimeSpan.FromMinutes(1));
                    _cache.Set(dayKey, dayCount + 1, TimeSpan.FromDays(1));
                    
                    // Adicionar headers de rate limit
                    context.Response.Headers["X-RateLimit-Minute-Limit"] = 
                        cliente.RateLimitPerMinute.ToString();
                    context.Response.Headers["X-RateLimit-Minute-Remaining"] = 
                        (cliente.RateLimitPerMinute - minuteCount - 1).ToString();
                    context.Response.Headers["X-RateLimit-Day-Limit"] = 
                        cliente.RateLimitPerDay.ToString();
                    context.Response.Headers["X-RateLimit-Day-Remaining"] = 
                        (cliente.RateLimitPerDay - dayCount - 1).ToString();
                }
            }
            
            await _next(context);
        }
    }

    public static class ClientRateLimitMiddlewareExtensions
    {
        public static IApplicationBuilder UseClientRateLimit(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ClientRateLimitMiddleware>();
        }
    }
}