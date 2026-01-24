using EvMarketplace.Api.Endpoints;
using EvMarketplace.Infrastructure.Data;
using EvMarketplace.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

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

// Map endpoints
app.MapEvEndpoints();
app.MapCalculatorEndpoints();
app.MapListingEndpoints();
app.MapSellerEndpoints();

app.Run();
