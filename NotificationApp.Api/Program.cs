using NotificationApp.Core.Interfaces;
using NotificationApp.Core.Models;
using NotificationApp.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.Configure<NotificationSettings>(
    builder.Configuration.GetSection(NotificationSettings.SectionName));

var discordSettings = builder.Configuration.GetSection(NotificationSettings.SectionName).Get<NotificationSettings>();

var isTestEnvironment = builder.Environment.EnvironmentName == "Test";
if (!isTestEnvironment && discordSettings?.Discord.IsConfigured == true)
{
    builder.Services.AddHttpClient("Discord");
    builder.Services.AddSingleton<IDiscordSender, DiscordSender>();
}
else
{
    builder.Services.AddSingleton<IDiscordSender, MockDiscordSender>();
}

builder.Services.AddSingleton<IRateLimiter, NotificationRateLimiter>();
builder.Services.AddScoped<NotificationProcessor>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Notification API",
        Version = "v1",
        Description = "Receives notifications and send qualifying levels to an external interface."
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = "error",
            message = "An unexpected error occurred. Please try again later."
        });
    });
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
