using ASA_PLATFORM_REPO.DBContext;
using ASA_PLATFORM_REPO.Repository;
using ASA_PLATFORM_SERVICE.CronJobs;
using ASA_PLATFORM_SERVICE.Implenment;
using ASA_PLATFORM_SERVICE.Interface;
using ASA_TENANT_SERVICE.Mapping;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
// Cấu hình để chạy trên Docker/Render
var port = Environment.GetEnvironmentVariable("PORT");

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    if (string.IsNullOrEmpty(port))
    {
        // Local
        serverOptions.ListenAnyIP(8081);
    }
    else
    {
        // Render (PORT luôn = 8080 do Render set)
        serverOptions.ListenAnyIP(int.Parse(port));
    }
});


Console.WriteLine("🌍 ENVIRONMENT = " + builder.Environment.EnvironmentName);
Console.WriteLine("🔌 CONNECTION = " + builder.Configuration.GetConnectionString("DefaultConnection"));
// Add services to the container.
builder.Services.AddScoped<ILogActivityService, LogActivityService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IPromotionProductService, PromotionProductService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IReportDetailService, ReportDetailService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IShopService, ShopService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IEmailService, EmailService>();


// Register repositories
builder.Services.AddScoped<LogActivityRepo>();
builder.Services.AddScoped<NotificationRepo>();
builder.Services.AddScoped<OrderRepo>();
builder.Services.AddScoped<ProductRepo>();
builder.Services.AddScoped<PromotionProductRepo>();
builder.Services.AddScoped<PromotionRepo>();
builder.Services.AddScoped<ReportDetailRepo>();
builder.Services.AddScoped<ReportRepo>();
builder.Services.AddScoped<ShopRepo>();
builder.Services.AddScoped<UserRepo>();
builder.Services.AddScoped<PasswordResetRepo>();

// Add HttpClient for BeTenant
builder.Services.AddHttpClient("BETenantUrl", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BETenantUrl:Url"]);
    client.Timeout = TimeSpan.FromSeconds(60);
});

// Đăng ký AutoMapper
builder.Services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        builder =>
        {
            builder.WithOrigins(
                    "http://localhost:5173",
                    "http://localhost:3000",
                    "https://asa-web-app-tawny.vercel.app",
                    "https://asa-fe-three.vercel.app",
                    "https://asa-admin-mu.vercel.app",
                    "http://localhost:8081",
                    "https://localhost:8081",
                    "https://localhost:8080",
                    "https://asa-tenant-be.onrender.com"
                 )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

//Đăng ký Quartz
builder.Services.AddQuartz(q =>
{
    // Weekly job: chạy mỗi ngày lúc 00:05 T2
    var dailyJobKey = new JobKey("WeeklyReportJob");
    q.AddJob<WeeklyReportJob>(opts => opts.WithIdentity(dailyJobKey));
    q.AddTrigger(opts => opts
        .ForJob(dailyJobKey)
        .WithIdentity("WeeklyReportTrigger")
        .WithCronSchedule("0 5 0 ? * MON", x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")))); // 00:05 mỗi thứ Hai (UTC)

    // Monthly job: chạy ngày 1 hàng tháng lúc 00:30 
    var monthlyJobKey = new JobKey("MonthlyReportJob");
    q.AddJob<MonthlyReportJob>(opts => opts.WithIdentity(monthlyJobKey));
    q.AddTrigger(opts => opts
        .ForJob(monthlyJobKey)
        .WithIdentity("MonthlyReportTrigger")
        .WithCronSchedule("0 30 0 1 * ?", x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")))); // 00:30 UTC ngày 1 hàng tháng

    // Check Trial job: chạy mỗi ngày lúc 01:00
    var checkTrialJobKey = new JobKey("CheckTrialJob");
    q.AddJob<CheckTrialJob>(opts => opts.WithIdentity(checkTrialJobKey));
    q.AddTrigger(opts => opts
        .ForJob(checkTrialJobKey)
        .WithIdentity("CheckTrialTrigger")
        .WithCronSchedule("0 0 1 * * ?", x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")))); // 01:00 UTC mỗi ngày
});

//// Quartz test 5p và 10p
//builder.Services.AddQuartz(q =>
//{
    //// Daily job: chạy mỗi 5 phút
    //var dailyJobKey = new JobKey("DailyReportJob");
    //q.AddJob<WeeklyReportJob>(opts => opts.WithIdentity(dailyJobKey));
    //q.AddTrigger(opts => opts
    //    .ForJob(dailyJobKey)
    //    .WithIdentity("DailyReportTrigger")
    //    .WithSimpleSchedule(x => x.WithIntervalInMinutes(1).RepeatForever()));

    ////Monthly job: chạy mỗi 10 phút(chỉ để test)
    //var monthlyJobKey = new JobKey("MonthlyReportJob");
    //q.AddJob<MonthlyReportJob>(opts => opts.WithIdentity(monthlyJobKey));
    //q.AddTrigger(opts => opts
    //    .ForJob(monthlyJobKey)
    //    .WithIdentity("MonthlyReportTrigger")
    //    .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever()));

    //Check Trial job: chạy mỗi 5 phút
//   var checkTrialJobKey = new JobKey("CheckTrialJob");
//q.AddJob<CheckTrialJob>(opts => opts.WithIdentity(checkTrialJobKey));
//q.AddTrigger(opts => opts
//    .ForJob(checkTrialJobKey)
//    .WithIdentity("CheckTrialTrigger")
//    .WithSimpleSchedule(x => x.WithIntervalInMinutes(1).RepeatForever()));
//});


// Dùng Quartz background service
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// Add Memory Cache
builder.Services.AddMemoryCache();

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// ==================== Controllers & Swagger ====================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        Description = "JWT Authorization header using the access token",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };
    options.SwaggerDoc("v1", new() { Title = "ASA-PLATFORM-BE API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        jwtSecurityScheme, Array.Empty<string>()
                    }
                });
});

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtConfig");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };

    // Custom response when the token is invalid or missing
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            // Skip default behavior
            context.HandleResponse();

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
            {
                Success = false,
                Status = 401,
                Message = "Unauthorized: Token is missing or invalid"
            }));
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
            {
                Success = false,
                Status = 403,
                Message = "Forbidden: You do not have permission to access this resource"
            }));
        }
    };
});


// Add DbContext
builder.Services.AddDbContext<ASAPLATFORMDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// ==================== Middleware Pipeline ====================
// Luôn bật swagger (kể cả Production như EDUConnect)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ASA PLATFORM API v1");
});

// CORS phải đặt trước Authorization
app.UseCors("AllowFrontend");

//app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
