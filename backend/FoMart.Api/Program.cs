using System.Globalization;
using FoMart.Api.Middleware;
using FoMart.Application.DTO;
using FoMart.Application.Interfaces.UnitOfWork;
using FoMart.Application.Services.Store;
using FoMart.Infrastructure;
using FoMart.Infrastructure.UnitOfWork;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    // ---------- Services (mỗi chủ đề một dòng) ----------
    builder.Services.AddScoped<StoreService>();

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
    builder.Services.AddOpenApi();
    builder.Services.AddExceptionHandler<ApiExceptionHandler>();
    builder.Services.AddProblemDetails(); // bắt buộc để UseExceptionHandler() gọi ApiExceptionHandler; phản hồi vẫn là HTTPResponseData
    builder.Services.AddHealthChecks().AddDbContextCheck<FoMartDbContext>("database");

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();                                                              // /openapi/v1.json
        app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "FoMart API v1")); // /swagger
    }

    app.UseCors("Frontend");
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

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
