# 2026-09-16 新增（見 Infra-Progress.md §32、Hybrid-Cloud.md §8）：GitHub Actions 用這個 Dockerfile
# build 出 worker 的容器映像，推到 Azure Container Registry (myerpacr)，供 K8s 叢集 pull
# （KEDA 依 sales-events 佇列長度把這個 Deployment 從 0 拉起到 N，見 Infra-Progress.md §32）。
#
# 跟 MyErp.Api/Dockerfile 一樣，build context 必須是 repo 根目錄：
#   docker build -f src/MyErp.Worker/Dockerfile -t <image> .

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/MyErp.Domain/MyErp.Domain.csproj", "src/MyErp.Domain/"]
COPY ["src/MyErp.Application/MyErp.Application.csproj", "src/MyErp.Application/"]
COPY ["src/MyErp.Infra/MyErp.Infra.csproj", "src/MyErp.Infra/"]
COPY ["src/MyErp.Worker/MyErp.Worker.csproj", "src/MyErp.Worker/"]
RUN dotnet restore "src/MyErp.Worker/MyErp.Worker.csproj"

COPY src/MyErp.Domain/ src/MyErp.Domain/
COPY src/MyErp.Application/ src/MyErp.Application/
COPY src/MyErp.Infra/ src/MyErp.Infra/
COPY src/MyErp.Worker/ src/MyErp.Worker/
RUN dotnet publish "src/MyErp.Worker/MyErp.Worker.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---- Runtime stage：用純 runtime 映像（不是 aspnet）——worker 是 Generic Host 背景服務，
# 不需要 ASP.NET Core／Kestrel 那層共用框架，鏡像比 MyErp.Api 更精簡。不對外提供任何網路服務，
# 所以不用 EXPOSE 任何 port。 ----
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MyErp.Worker.dll"]
