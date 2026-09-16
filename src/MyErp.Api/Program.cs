using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MyErp.Api.Filters;
using MyErp.Api.Middleware;
using MyErp.Application;
using MyErp.Application.Common;
using MyErp.Infra;
using MyErp.Infra.Seed;

var builder = WebApplication.CreateBuilder(args);

// ---- Application / Infra 層註冊 ----
builder.Services.AddMyErpApplication();
builder.Services.AddMyErpInfra(builder.Configuration);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

// ---- JWT 驗證 ----
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtKey = jwtSection["Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    // 開發階段就直接爆炸，比讓 JWT 用一把空字串簽章、卻在執行期才發現安全性問題好。
    throw new InvalidOperationException(
        "找不到 Jwt:Key。請先執行 `dotnet user-secrets set \"Jwt:Key\" \"...\"`（見交付說明）。");
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization();

// ---- Controllers / Swagger ----
builder.Services.AddScoped<ActivityLogActionFilter>();
builder.Services.AddControllers(options =>
{
    // 全域套用：自動記錄「誰在什麼時候做了什麼」，見 ActivityLogActionFilter 的說明。
    options.Filters.Add<ActivityLogActionFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "MyErp API", Version = "v1" });

    // Swashbuckle.AspNetCore v10 改用 Microsoft.OpenApi v2 的物件模型，
    // AddSecurityRequirement 要用 Func<OpenApiDocument, OpenApiSecurityRequirement>，
    // 並且用 OpenApiSecuritySchemeReference 參照剛剛用 AddSecurityDefinition 定義的 scheme
    // （舊版直接 new OpenApiReference {...} 的寫法在這個版本已經不能用）。
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "貼上登入拿到的 JWT token（不用加 \"Bearer \" 前綴，Swagger UI 會自動加上）。",
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
    });
});

builder.Services.AddCors(options =>
{
    // ERP.md §2：前端是獨立的 Vite + React 專案，開發時跑在不同的 port，需要開 CORS。
    // 正式上線網址還沒決定，先只開放本機開發常見的幾個 port；等前端網址確定後要記得回來改。
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// ---- 啟動時自動套用 migration + seed 預設帳號 ----
// 單機部署（ERP.md §9）情境下，這樣比額外再教一個人手動下 `dotnet ef database update` 簡單。
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MyErpDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.SeedAsync(db);
}

// ---- HTTP pipeline ----
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 放在管線最前面，確保後面任何 middleware／controller 丟出的例外都攔得到。
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2026-09-16 移除 UseHttpsRedirection（見 Infra-Progress.md §17、MyERP-gitops Helm chart）：
// 正式環境的實際路徑是「瀏覽器 → Cloudflare 邊緣（TLS 在這裡終止）→ Cloudflare Tunnel →
// Cilium Gateway API（只監聽 HTTP port 80，叢集內完全沒有 TLS）→ 這個 Pod」，Pod 收到的
// 永遠是 plain HTTP 請求。如果留著 UseHttpsRedirection，這裡會對每個請求回 307 導去
// https://...，瀏覽器照做後在 Cloudflare 邊緣又被終止、又轉成 HTTP 送進來，又被導向
// https——形成無限重導迴圈，正式環境會整個打不通。TLS 這件事完全交給 Cloudflare 邊緣負責，
// 應用程式本身不需要、也不能再做一次。

// K8s liveness/readiness probe 用的健康檢查端點：故意不用 Swagger（Swagger 只有
// Development 環境才會啟用，見上面 IsDevelopment() 判斷，正式環境會 404），也不用經過
// JWT 驗證，單純回 200 讓 kubelet 判斷這個 Pod 是否還活著、可以開始收流量
//（見 MyERP-gitops Helm chart 的 api-deployment.yaml）。
app.MapGet("/healthz", () => Results.Ok());

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
