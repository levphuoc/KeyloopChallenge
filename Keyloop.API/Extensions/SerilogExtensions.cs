using Serilog;

namespace Keyloop.API.Extensions
{
    public static class SerilogExtensions 
    {
        public static IHostBuilder AddSerilogLogging(this IHostBuilder host)
        {
            host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));
            return host;
        }
    }
}
