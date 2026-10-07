# Multi-stage build: compile with the full SDK, ship only the runtime + published output.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/ECommerceStore.Core/*.csproj src/ECommerceStore.Core/
COPY src/ECommerceStore.Infrastructure/*.csproj src/ECommerceStore.Infrastructure/
COPY src/ECommerceStore.Web/*.csproj src/ECommerceStore.Web/
RUN dotnet restore src/ECommerceStore.Web/ECommerceStore.Web.csproj

COPY . .
RUN dotnet publish src/ECommerceStore.Web/ECommerceStore.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# .NET 8's shortcut for "listen on this port" — no ASPNETCORE_URLS syntax needed.
# Most free hosts (Render, Fly.io, etc.) expect the container to listen on 8080 by default;
# check your host's docs/dashboard if it wants a different port.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Production mode: friendly error pages, no stack traces, HSTS. Never run a live site in Development mode.
ENV ASPNETCORE_ENVIRONMENT=Production

# Behind a reverse proxy or host load balancer: trust X-Forwarded-For/Proto so the rate limiter sees the
# real visitor address and HTTPS redirection works. Only keep this on when the container is NOT reachable directly.
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Files that must survive a redeploy: mount volumes here (or use your host's persistent disk).
#   /app/wwwroot/uploads   product photos
#   /app/App_Data          private payment receipts + data-protection keys
ENV Storage__PrivatePath=/app/App_Data/private
ENV DataProtection__KeysPath=/app/App_Data/keys
RUN mkdir -p /app/wwwroot/uploads /app/App_Data/private /app/App_Data/keys \
    && chown -R $APP_UID /app/wwwroot/uploads /app/App_Data

# The official runtime image provides a non-root user ($APP_UID); run as it instead of root.
USER $APP_UID

ENTRYPOINT ["dotnet", "ECommerceStore.Web.dll"]
