using CarStock.Api.Data;
using CarStock.Api.Auth;
using CarStock.Api.Cars;
using CarStock.Api.Errors;
using System.Text.Json;
using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var signingKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey) && builder.Environment.IsDevelopment())
    signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
    throw new InvalidOperationException("Set Jwt__SigningKey to a random secret of at least 32 bytes.");

var databasePath = builder.Configuration["Database:Path"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "Data", "carstock.db");
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(databasePath))!);
var database = new Database(databasePath);
builder.Services.AddSingleton(database);
builder.Services.AddScoped<CarRepository>();
builder.Services.AddSingleton(new TokenSettings(signingKey));
builder.Services.AddSingleton<IPasswordHasher<Dealer>, PasswordHasher<Dealer>>();
builder.Services.AddAuthenticationJwtBearer(options => options.SigningKey = signingKey, options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters.ValidateIssuer = true;
    options.TokenValidationParameters.ValidIssuer = TokenSettings.Issuer;
    options.TokenValidationParameters.ValidateAudience = true;
    options.TokenValidationParameters.ValidAudience = TokenSettings.Audience;
    options.TokenValidationParameters.ValidateLifetime = true;
    options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
});
builder.Services.AddAuthorization(options => options.AddPolicy("Dealer", policy =>
    policy.RequireAuthenticatedUser().RequireAssertion(context =>
        long.TryParse(context.User.FindFirst(TokenSettings.DealerIdClaim)?.Value, out var id) && id > 0)));
builder.Services.AddFastEndpoints();
await DatabaseInitializer.InitializeAsync(database);
var app = builder.Build();
if (app.Environment.IsDevelopment())
    await DevelopmentDealers.SeedAsync(database, app.Services.GetRequiredService<IPasswordHasher<Dealer>>());
app.UseMiddleware<ApiErrorMiddleware>();
app.UseStatusCodePages(context => ApiError.Result(context.HttpContext,
    context.HttpContext.Response.StatusCode).ExecuteAsync(context.HttpContext));
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints(options =>
{
    options.Errors.ContentType = "application/json";
    options.Errors.ResponseBuilder = (failures, context, status) => ApiError.Validation(context, status, failures);
});
app.Run();


