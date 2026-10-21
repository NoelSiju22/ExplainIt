using ExplainIt.API.Data;
using ExplainIt.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

const string FrontendCorsPolicy = "ExplainItFrontend";

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Enter your Supabase access token."
    });

    options.AddSecurityRequirement(document =>
        new Microsoft.OpenApi.OpenApiSecurityRequirement
        {
            [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

builder.Services.AddDbContext<ExplainItDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("ExplainIt")));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://dpiwrkhmtvezajxiwxqy.supabase.co/auth/v1";
        options.Audience = "authenticated";
        options.RequireHttpsMetadata = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpClient<SupabaseAuthService>();
builder.Services.AddScoped<ExplainItAiService>();
builder.Services.AddHttpClient<OfflineNlpService>((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["OfflineNlp:BaseUrl"] ?? "http://127.0.0.1:8000/";
    client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
    var timeoutSeconds = configuration.GetValue<int?>("OfflineNlp:TimeoutSeconds") ?? 90;
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 10, 300));
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
