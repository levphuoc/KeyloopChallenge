using Keyloop.API.Extensions;
using Keyloop.Application;
using Keyloop.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilogLogging();
builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddAppDbContextFactory(builder.Configuration);
builder.Services.AddPersistenceAuditing();
builder.Services.AddApplicationRequestPipeline();
builder.Services.AddApplicationAuthentication(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

app.UseApiPipeline();
app.Run();

public partial class Program;
