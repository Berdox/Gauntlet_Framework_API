# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["gauntlet_framework_api.csproj", "./"]
RUN dotnet restore "gauntlet_framework_api.csproj"

# Copy remaining source code and publish
COPY . .
RUN dotnet publish "gauntlet_framework_api.csproj" -c Release -o /app/publish /p:UseAppHost=false
RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "gauntlet_framework_api.dll"]