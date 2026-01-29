using EvMarketplace.Api.Endpoints;
using EvMarketplace.Infrastructure.Data;
using EvMarketplace.Infrastructure.Repositories;
using EvMarketplace.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext - connection string will be injected by Aspire
builder.AddNpgsqlDbContext<EvMarketplaceDbContext>("evmarketplace");

// Add repositories
builder.Services.AddScoped<IEvRepository, EvRepository>();
builder.Services.AddScoped<IListingRepository, ListingRepository>();
builder.Services.AddScoped<ISellerRepository, SellerRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IFeaturedListingRepository, FeaturedListingRepository>();

// Add Stripe service
var stripeApiKey = builder.Configuration["Stripe:ApiKey"] ?? throw new InvalidOperationException("Stripe API key not configured");
builder.Services.AddSingleton<IStripeService>(new StripeService(stripeApiKey));

// Add Auth service
builder.Services.AddScoped<IAuthService, AuthService>();

// Add JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ev-marketplace";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ev-marketplace-api";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Apply migrations and seed data in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<EvMarketplaceDbContext>();

    // Apply pending migrations
    await dbContext.Database.MigrateAsync();

    // Seed data
    await SeedData.InitializeAsync(dbContext);
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapAuthEndpoints();
app.MapEvEndpoints();
app.MapCalculatorEndpoints();
app.MapListingEndpoints();
app.MapSellerEndpoints();
app.MapSubscriptionEndpoints();
app.MapPaymentEndpoints();
app.MapFeaturedListingEndpoints();

app.Run();
