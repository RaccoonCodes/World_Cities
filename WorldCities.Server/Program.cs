using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using WorldCities.Server.Data;
using WorldCities.Server.Data.Models;
using WorldCities.Server.Data.GraphQL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
        )
);

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
}).AddApiEndpoints().AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        RequireExpirationTime = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecurityKey"]!))
    };
}).AddBearerToken(IdentityConstants.BearerScheme);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.MSSqlServer(connectionString:
                ctx.Configuration.GetConnectionString("DefaultConnection"),
            restrictedToMinimumLevel: LogEventLevel.Information,
            sinkOptions: new MSSqlServerSinkOptions
            {
                TableName = "LogEvents",
                AutoCreateSqlTable = true
            }
            )
    .WriteTo.Console()
);

builder.Services.AddScoped<JwtHandler>();

builder.Services.AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddFiltering()
    .AddSorting();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapIdentityApi<IdentityUser>();
app.MapControllers();
app.MapGraphQL("/api/graphql");

app.MapFallbackToFile("/index.html");

app.Run();

/*Packages 
 * Microsoft.EntityFrameworkCore version 8.0.0
 * Microsoft.EntityFrameworkCore.Tools version 8.0.0
 * Microsoft.EntityFrameworkCore.SqlServer version 8.0.0
 * Microsoft.AspNetCore.Identity.EntityFrameworkCore version 8.0.0
 * Microsoft.AspNetCore.Authentication.JwtBearer version 8.0.0
 * EPPlus version 4.5.3.3 (for practice purposes and license infringement, used older version) 
 * EFCore.BulkExtensions version 8.0.1
 * Serilog.AspNetCore version 9.0.0
 * Serilog.Settings.Configuration version 9.0.0
 * Serilog.Sinks.MSSqlServer version 8.1.0
 * HotChocolate.AspNetCore version 13.7.0
 * HotChocolate.AspNetCore.Authorization version 13.7.0
 * HotChocolate.Data.EntityFramework version 13.7.0
 * 
 * Unit Testing:
 * Moq version 4.20.70
 * Microsoft.EntityFrameworkCore.InMemory version 8.0.11
 * 
 */
