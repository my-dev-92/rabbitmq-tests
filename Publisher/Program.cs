using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Publisher;
using Serilog;

Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

var hostBuilder = Host.CreateApplicationBuilder(args);

hostBuilder.Services.AddLogging(x =>
{
    var logger = new LoggerConfiguration()
        .MinimumLevel.Verbose()
        .Enrich.WithThreadId()
        .WriteTo.File(Path.Combine("..", "..", "..", "Logs", "log.txt"),
            outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} <{ThreadId,4}> [{Level:u3}] {SourceContext}: {Scope}{Message} {Properties}{NewLine}{Exception}",
            retainedFileCountLimit: 14,
            rollingInterval: RollingInterval.Day)
        .CreateLogger();
    x.AddSerilog(logger);
});

hostBuilder.Services.AddScoped<ConnectionFactory>();
hostBuilder.Services.AddSingleton<EventPublisher>();
hostBuilder.Services.AddHostedService<PublishWorker>();

var host = hostBuilder.Build();

Console.WriteLine("Starting");
await host.RunAsync();
Console.WriteLine("Stopped");