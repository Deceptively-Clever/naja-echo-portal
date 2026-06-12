FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY NajaEcho.slnx ./
COPY src/NajaEcho.Domain/NajaEcho.Domain.csproj         src/NajaEcho.Domain/
COPY src/NajaEcho.Application/NajaEcho.Application.csproj src/NajaEcho.Application/
COPY src/NajaEcho.Infrastructure/NajaEcho.Infrastructure.csproj src/NajaEcho.Infrastructure/
COPY src/NajaEcho.Api/NajaEcho.Api.csproj               src/NajaEcho.Api/
RUN dotnet restore src/NajaEcho.Api/NajaEcho.Api.csproj

COPY src/ ./src/
RUN dotnet publish src/NajaEcho.Api/NajaEcho.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
 && apt-get install -y --no-install-recommends wget \
 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:5180 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 5180
ENTRYPOINT ["dotnet", "NajaEcho.Api.dll"]
