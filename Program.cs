using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using BitbucketApi.Services.Implementations;
using BitbucketApi.Services.Interfaces;
using BitbucketApi.Configurations;
using BitbucketApi.Middleware;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IItemService, ItemService>();

// Bitbucket Services
builder.Services.AddHttpClient<IBitbucketService, BitbucketService>();
builder.Services.AddScoped<IMetricsService, MetricsService>();

// CORS Configuration
var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
    ?? (builder.Environment.IsDevelopment() ? new[] { "*" } : Array.Empty<string>());

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        if (builder.Environment.IsDevelopment() && allowedOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// AutoMapper Configuration
builder.Services.AddMappers();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer("Auth0App1", options =>
{
    options.Audience = configuration["Auth0App1:Audience"] ?? Environment.GetEnvironmentVariable("Auth0App1.Audience");
    options.Authority = configuration["Auth0App1:Issuer"] ?? Environment.GetEnvironmentVariable("Auth0App1.Issuer");
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = configuration["Auth0App1:Issuer"] ?? Environment.GetEnvironmentVariable("Auth0App1.Issuer")
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var exception = context.Exception;
            if (exception is SecurityTokenExpiredException)
            {
                throw new SecurityTokenExpiredException("Token has expired", exception);
            }
            else if (exception is SecurityTokenInvalidSignatureException)
            {
                throw new SecurityTokenInvalidSignatureException("Invalid token signature", exception);
            }
            else if (exception is SecurityTokenValidationException)
            {
                throw new SecurityTokenValidationException("Token validation failed", exception);
            }
            else
            {
                throw new UnauthorizedAccessException("Authentication failed", exception);
            }
        },
        OnChallenge = context =>
        {
            if (string.IsNullOrEmpty(context.Request.Headers.Authorization))
            {
                throw new UnauthorizedAccessException("Authorization header is missing");
            }
            context.HandleResponse();
            throw new UnauthorizedAccessException("Authentication challenge failed");
        }
    };
})
.AddJwtBearer("Auth0App2", options =>
{
    options.Audience = configuration["Auth0App2:Audience"] ?? Environment.GetEnvironmentVariable("Auth0App2.Audience");
    options.Authority = configuration["Auth0App2:Issuer"] ?? Environment.GetEnvironmentVariable("Auth0App2.Issuer");
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = configuration["Auth0App2:Issuer"] ?? Environment.GetEnvironmentVariable("Auth0App2.Issuer")
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var exception = context.Exception;
            if (exception is SecurityTokenExpiredException)
            {
                throw new SecurityTokenExpiredException("Token has expired", exception);
            }
            else if (exception is SecurityTokenInvalidSignatureException)
            {
                throw new SecurityTokenInvalidSignatureException("Invalid token signature", exception);
            }
            else if (exception is SecurityTokenValidationException)
            {
                throw new SecurityTokenValidationException("Token validation failed", exception);
            }
            else
            {
                throw new UnauthorizedAccessException("Authentication failed", exception);
            }
        },
        OnChallenge = context =>
        {
            if (string.IsNullOrEmpty(context.Request.Headers.Authorization))
            {
                throw new UnauthorizedAccessException("Authorization header is missing");
            }
            context.HandleResponse();
            throw new UnauthorizedAccessException("Authentication challenge failed");
        }
    };
});


builder.Services.AddControllers();

// Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Global Exception Handler - must be first in pipeline
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseHttpsRedirection();
app.UseRouting();

app.UseCors("AllowSpecificOrigins");

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// Health Check endpoint
app.MapHealthChecks("/health");

app.Run();