using System.Globalization;
using System.Threading.RateLimiting;
using FoMart.Api.Middleware;
using FoMart.Application.DTO;
using FoMart.Application.DTO.Auth;
using FoMart.Application.Interfaces.Authentication;
using FoMart.Application.Interfaces.UnitOfWork;
using FoMart.Application.Services.Auth;
using FoMart.Application.Services.Catalog;
using FoMart.Application.Services.Inventory;
using FoMart.Application.Services.Purchasing;
using FoMart.Application.Services.Store;
using FoMart.Infrastructure;
using FoMart.Infrastructure.Authentication;
using FoMart.Infrastructure.UnitOfWork;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, logger) => logger
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // ---------- Database ----------
    // Chuỗi kết nối đặt bằng User Secrets (database/README.md), không ghi vào appsettings.json.
    var connectionString = builder.Configuration.GetConnectionString("FoMart");
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("Missing ConnectionStrings:FoMart. Set it with: dotnet user-secrets set \"ConnectionStrings:FoMart\" \"...\" --project FoMart.Api");
    builder.Services.AddDbContext<FoMartDbContext>(options => options.UseSqlServer(connectionString));

    // ---------- Unit of Work (repository do UoW tự tạo, không đăng ký riêng) ----------
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // ---------- Authentication (JWT) ----------
    // Jwt:SigningKey đặt bằng User Secrets / biến môi trường; thiếu hoặc ngắn hơn 32 ký tự thì API không khởi động.
    builder.Services.AddOptions<JwtOptions>()
        .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();
    var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? throw new InvalidOperationException("Missing Jwt configuration section.");
    builder.Services.AddSingleton<ITokenService, JwtTokenService>();
    builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false; // giữ nguyên tên claim: sub, name, role
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = JwtTokenService.CreateSigningKey(jwt.SigningKey),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                NameClaimType = JwtRegisteredClaimNames.Name,
                RoleClaimType = "role",
                ClockSkew = TimeSpan.FromSeconds(30),
            };
            // 401/403 cũng trả HTTPResponseData như mọi API khác.
            options.Events = new JwtBearerEvents
            {
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    return WriteResponseAsync(context.HttpContext, StatusCodes.Status401Unauthorized, AuthResponseMessageDTO.Unauthorized);
                },
                OnForbidden = context =>
                    WriteResponseAsync(context.HttpContext, StatusCodes.Status403Forbidden, AuthResponseMessageDTO.Forbidden),
            };
        });

    // Mặc định MỌI endpoint phải đăng nhập; endpoint công khai đánh dấu [AllowAnonymous] (NFR-09).
    builder.Services.AddAuthorizationBuilder()
        .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

    // Chống dò mật khẩu/PIN: mỗi IP tối đa RateLimiting:AuthPermitLimit lần/phút (mặc định 10)
    // cho các endpoint đăng nhập [EnableRateLimiting("auth")].
    var authPermitLimit = builder.Configuration.GetValue("RateLimiting:AuthPermitLimit", 10);
    builder.Services.AddRateLimiter(options =>
    {
        options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = authPermitLimit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        options.OnRejected = (context, _) =>
            new ValueTask(WriteResponseAsync(context.HttpContext, StatusCodes.Status429TooManyRequests, AuthResponseMessageDTO.TooManyRequests));
    });

    // ---------- Services (mỗi chủ đề một dòng) ----------
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<UserService>();
    builder.Services.AddScoped<StoreService>();
    builder.Services.AddScoped<ProductService>();
    builder.Services.AddScoped<CategoryService>();
    builder.Services.AddScoped<SupplierService>();
    builder.Services.AddScoped<LocationService>();
    builder.Services.AddScoped<GoodsReceiptService>();
    builder.Services.AddScoped<StockService>();

    // ---------- CORS ----------
    // Origin được phép đọc từ Cors:AllowedOrigins. Không AllowAnyOrigin; JWT gửi qua header nên không cần AllowCredentials.
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

    // ---------- API ----------
    builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
        // Lỗi validate request (400) cũng trả HTTPResponseData thay vì ProblemDetails mặc định.
        options.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(new HTTPResponseData<Dictionary<string, string[]>>
        {
            DataResponse = context.ModelState
                .Where(entry => entry.Value is { Errors.Count: > 0 })
                .ToDictionary(entry => entry.Key, entry => entry.Value!.Errors.Select(e => e.ErrorMessage).ToArray()),
            Message = "Dữ liệu gửi lên không hợp lệ.",
            StatusCode = StatusCodes.Status400BadRequest
        }));
    // Swagger có nút Authorize: dán AccessToken lấy từ POST /api/auth/login.
    builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "AccessToken từ POST /api/auth/login (không cần gõ chữ Bearer).",
        };
        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
        return Task.CompletedTask;
    }));
    builder.Services.AddExceptionHandler<ApiExceptionHandler>();
    builder.Services.AddProblemDetails(); // bắt buộc để UseExceptionHandler() gọi ApiExceptionHandler; phản hồi vẫn là HTTPResponseData
    builder.Services.AddHealthChecks().AddDbContextCheck<FoMartDbContext>("database");

    var app = builder.Build();

    // Ghi log request bọc ngoài ExceptionHandler để log đúng mã trả về cuối cùng (409 chứ không phải 500).
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi().AllowAnonymous();                                               // /openapi/v1.json
        app.UseSwaggerUI(o =>                                                             // /swagger
        {
            o.SwaggerEndpoint("/openapi/v1.json", "FoMart API v1");
            o.EnablePersistAuthorization();
        });
    }

    app.UseCors("Frontend");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health").AllowAnonymous();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "FoMart API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Ghi phản hồi lỗi theo khuôn HTTPResponseData (dùng cho 401/403/429 phát sinh ngoài controller).
static Task WriteResponseAsync(HttpContext context, int statusCode, string message)
{
    context.Response.StatusCode = statusCode;
    return context.Response.WriteAsJsonAsync(HTTPResponse.Fail<object>(statusCode, message));
}
