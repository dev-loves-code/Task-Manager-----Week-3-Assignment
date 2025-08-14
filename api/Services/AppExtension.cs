using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Serilog.Formatting.Json;

namespace api.Services
{
    public static class AppExtension
    {
        public static void SerilogConfiguration(this IHostBuilder host)
        {
            host.UseSerilog((context, loggerConfiguration) =>
            {
                loggerConfiguration
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.File(new JsonFormatter(), "logs/applogs-.txt", rollingInterval: RollingInterval.Day);
            });
        }
    }
}