# 2026-09-16 新增（見 Infra-Progress.md §32、Hybrid-Cloud.md §8）：GitHub Actions 用這個 Dockerfile
# build 出 backend-api 的容器映像，推到 Azure Container Registry (myerpacr)，供 K8s 叢集 pull。
#
# 這個 build context 必須是 repo 根目錄（不是 src/MyErp.Api 資料夾本身），因為 MyErp.Api.csproj
# 用相對路徑參考了同一個 solution 底下的 MyErp.Domain/MyErp.Application/MyErp.Infra 三個專案。
# GitHub Actions 裡對應的指令是：
#   docker build -f src/MyErp.Api/Dockerfile -t <image> .

# ---- Build stage：用完整的 SDK 映像還原套件、編譯、發布 ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 先只複製各專案的 .csproj，讓 `dotnet restore` 這一層可以被 Docker layer cache 重複利用——
# 只要 .csproj 內容沒變（沒有新增/更新套件），之後改動 .cs 原始碼重新 build 時就不用重跑 restore。
COPY ["src/MyErp.Domain/MyErp.Domain.csproj", "src/MyErp.Domain/"]
COPY ["src/MyErp.Application/MyErp.Application.csproj", "src/MyErp.Application/"]
COPY ["src/MyErp.Infra/MyErp.Infra.csproj", "src/MyErp.Infra/"]
COPY ["src/MyErp.Api/MyErp.Api.csproj", "src/MyErp.Api/"]
RUN dotnet restore "src/MyErp.Api/MyErp.Api.csproj"

# 再複製其餘原始碼並發布（Release 設定，不含 symbols，減少映像大小）。
COPY src/MyErp.Domain/ src/MyErp.Domain/
COPY src/MyErp.Application/ src/MyErp.Application/
COPY src/MyErp.Infra/ src/MyErp.Infra/
COPY src/MyErp.Api/ src/MyErp.Api/
RUN dotnet publish "src/MyErp.Api/MyErp.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---- Runtime stage：用只含 ASP.NET Core 執行環境的精簡映像，不含 SDK/編譯工具 ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# .NET 8 開始，官方 aspnet 映像預設已經用非 root 的 "app" 使用者執行、
# 且 ASPNETCORE_HTTP_PORTS 預設是 8080（不是舊版的 80），這裡明確寫出來避免日後映像版本改變預設值。
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MyErp.Api.dll"]
