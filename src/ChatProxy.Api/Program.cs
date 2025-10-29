using ChatProxy.Domain.Auth;
using ChatProxy.Domain.Chat;
using ChatProxy.Domain.Filter;
using ChatProxy.Infrastructure.Auth;
using ChatProxy.Infrastructure.Chat;
using ChatProxy.Infrastructure.Filter;
using ChatProxy.Infrastructure.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; 
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<BlacklistOptions>(builder.Configuration.GetSection("Blacklist"));
builder.Services.Configure<OpenAiSettings>(builder.Configuration.GetSection("OpenAi"));

var signingKey =
    Environment.GetEnvironmentVariable("JWT_SIGNING_KEY") 
    ?? builder.Configuration["Jwt:SigningKey"] 
    ?? throw new InvalidOperationException("JWT signing key not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new()
        {
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
        };
    });

builder.Services.AddAuthorization();

var rl = builder.Configuration.GetSection("RateLimiting");
builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = rl.GetValue<int>("PermitLimit", 30);
        options.Window = TimeSpan.FromSeconds(rl.GetValue<int>("WindowSeconds", 60));
        options.QueueLimit = rl.GetValue<int>("QueueLimit", 0);
    }));

builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<IWordFilter, WordFilter>(); 
builder.Services.AddHttpClient<IChatProvider, OpenAiChatProvider>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer. Ex.: **Bearer {seu_token}**",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<BlacklistMiddleware>();

app.MapPost("/auth/login", (JwtTokenService jwt, ChatProxy.Domain.Auth.LoginRequest req) =>
{
    if (req.Username == "admin" && req.Password == "P@ssw0rd!")
    {
        var token = jwt.Create(req.Username);
        return Results.Ok(new { access_token = token, token_type = "Bearer" });
    }
    return Results.Unauthorized();
})
.WithName("Login")
.Produces(200)
.Produces(401);

app.MapPost("/chat/ask", async (HttpContext ctx, IChatProvider provider, ChatProxy.Domain.Chat.ChatRequest req, CancellationToken ct) =>
{
    // exige auth
    if (!ctx.User.Identity?.IsAuthenticated ?? true) return Results.Unauthorized();

    var resp = await provider.CompleteAsync(req, ct);
    return Results.Ok(resp);
})
.RequireAuthorization()
.RequireRateLimiting("fixed")
.WithName("ChatAsk")
.Produces<ChatProxy.Domain.Chat.ChatResponse>(200)
.Produces(400)
.Produces(401);

app.Run();
