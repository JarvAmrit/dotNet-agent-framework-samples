using AzureAIFoundryApi.Configuration;
using AzureAIFoundryApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Bind configuration sections
builder.Services.Configure<AzureFoundrySettings>(
    builder.Configuration.GetSection(AzureFoundrySettings.SectionName));
builder.Services.Configure<ServicePrincipalSettings>(
    builder.Configuration.GetSection(ServicePrincipalSettings.SectionName));

// Register services
builder.Services.AddSingleton<CredentialFactory>();
builder.Services.AddSingleton<AIProjectClientFactory>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
