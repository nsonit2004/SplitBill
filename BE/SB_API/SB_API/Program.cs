using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SB_BusinessObjects;
using SB_Repositories.Implementations;
using SB_Repositories.Interfaces;
using SB_Services.Implementations;
using SB_Services.Interfaces;
using SB_Services.Strategies;
using SB_Services.Strategies.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 1. Đăng ký Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<ISettleTransactionRepository, SettleTransactionRepository>();
builder.Services.AddScoped<IGroupInviteRepository, GroupInviteRepository>();

// 2. Đăng ký Strategies cho Split Bill
builder.Services.AddScoped<ISplitStrategy, EquallySplitStrategy>();
builder.Services.AddScoped<ISplitStrategy, ExactAmountSplitStrategy>();
builder.Services.AddScoped<ISplitStrategy, BySharesSplitStrategy>();
builder.Services.AddScoped<ISplitStrategy, ExcludeSplitStrategy>();
builder.Services.AddScoped<SplitStrategyFactory>();

// 3. Đăng ký Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<ISettlementService, SettlementService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHttpClient<IBankAccountVerificationService, BankAccountVerificationService>();
builder.Services.AddHttpClient<IOcrService, GeminiOcrService>();

// 4. Cấu hình JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "VietQRSplitBillProSuperSecuritySecretKey2026";
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "VietQRSplitBillPro",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "VietQRSplitBillProUsers",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// 5. Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Sử dụng CORS và Authentication
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
