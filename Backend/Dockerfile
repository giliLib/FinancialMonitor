
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["FinancialMonitor.Api/FinancialMonitor.Api.csproj", "FinancialMonitor.Api/"]

RUN dotnet restore "FinancialMonitor.Api/FinancialMonitor.Api.csproj"

COPY . .

WORKDIR /src/FinancialMonitor.Api

RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

# Built-in non-root user in .NET 8 images
USER $APP_UID

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "FinancialMonitor.Api.dll"]
