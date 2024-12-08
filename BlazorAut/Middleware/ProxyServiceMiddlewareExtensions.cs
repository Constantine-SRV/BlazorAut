using Microsoft.AspNetCore.Builder;

namespace BlazorAut.Middleware
{
    public static class ProxyServiceMiddlewareExtensions
    {
        public static IApplicationBuilder UseProxyService(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ProxyServiceMiddleware>();
        }
    }
}
