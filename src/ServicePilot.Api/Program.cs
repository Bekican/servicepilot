using System.IdentityModel.Tokens.Jwt;
using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using ServicePilot.Api.Authentication;
using ServicePilot.Application;
using ServicePilot.Application.Abstractions.Tenancy;
using ServicePilot.Application.Authentication;
using ServicePilot.Infrastructure;
using ServicePilot.Infrastructure.Authentication;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

JwtOptions jwtOptions =
    JwtOptions.FromConfiguration(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization();
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType =
                JwtRegisteredClaimNames.Sub,
            RoleClaimType =
                AuthenticationClaimNames.Role
        };
    });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;