FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["eSKHub.API/eSKHub.API.csproj", "eSKHub.API/"]
RUN dotnet restore "eSKHub.API/eSKHub.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/eSKHub.API"
RUN dotnet publish "eSKHub.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render environment settings: disable inotify file watcher to fix Linux container limits
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

ENTRYPOINT ["dotnet", "eSKHub.API.dll"]
