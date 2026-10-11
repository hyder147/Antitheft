using System.Text;
using AntiTheftApp.Api.Data;
using AntiTheftApp.Api.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------- Database ----------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- App services ----------
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IFirebaseService, FirebaseService>();

// ---------- Firebase Admin SDK (singleton, initialized once at startup) ----------
var firebaseCredsPath = builder.Configuration["Firebase:CredentialsPath"];
var firebaseConfigured = false;
if (!string.IsNullOrWhiteSpace(firebaseCredsPath) && File.Exists(firebaseCredsPath))
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseCredsPath)
    });
    firebaseConfigured = true;
}
else
{
    // Don't crash local dev if the service account file isn't set up yet —
    // /api/devices/{id}/commands will just fail until this is fixed, but
    // now with a clear message instead of a raw NullReferenceException.
    Console.WriteLine("WARNING: Firebase credentials not found. Push notifications will not work until Firebase:CredentialsPath is set in appsettings.json (see README).");
}
builder.Services.AddSingleton(new FirebaseConfigStatus(firebaseConfigured));

// ---------- JWT Auth ----------
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key missing in configuration");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep raw "sub" claim name instead of ASP.NET's auto-remapping
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// ---------- Controllers / Swagger ----------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "AntiTheftApp API", Version = "v1" });

    // Lets you paste a JWT into Swagger's "Authorize" button to test protected endpoints.
    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
    options.AddSecurityRequirement(new()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ---------- CORS (so the React portal on a different port/domain can call this API) ----------
builder.Services.AddCors(options =>
{
    options.AddPolicy("PortalPolicy", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["Cors:PortalOrigin"] ?? "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("PortalPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposed so the integration tests can use WebApplicationFactory<Program>.
public partial class Program { }
