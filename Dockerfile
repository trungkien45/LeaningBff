FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Install Node.js 24.x and ABP CLI to run abp install-libs
RUN apt-get update && apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_24.x | bash - && \
    apt-get install -y nodejs && \
    dotnet tool install -g Volo.Abp.Cli && \
    rm -rf /var/lib/apt/lists/*

ENV PATH="${PATH}:/root/.dotnet/tools"

COPY common.props NuGet.Config ./
COPY LearningBff/LearningBff.csproj LearningBff/

RUN dotnet restore LearningBff/LearningBff.csproj

COPY . .
WORKDIR /src/LearningBff

RUN abp install-libs

RUN dotnet publish LearningBff.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

RUN apt-get update && \
    apt-get install -y --no-install-recommends libgssapi-krb5-2 && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# PostgreSQL
ENV ConnectionStrings__Default="Host=postgres;Port=5432;Database=LearningBFF;Username=postgres;Password=postgres"

ENV App__HealthCheckUrl="http://127.0.0.1:8080/health-status"
ENV App__SelfUrl="http://localhost:8080"

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "LearningBff.dll"]