using ASA_PLATFORM_REPO.DBContext;
using ASA_PLATFORM_REPO.Repository;
using ASA_PLATFORM_SERVICE.Implenment;
using ASA_PLATFORM_SERVICE.Interface;
using ASA_TENANT_SERVICE.Mapping;
using Microsoft.EntityFrameworkCore;

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
                    "https://asa-web-app-tawny.vercel.app",
                    "https://asa-fe-three.vercel.app",
                    "https://asa-admin-mu.vercel.app"
                 )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// ==================== Controllers & Swagger ====================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
