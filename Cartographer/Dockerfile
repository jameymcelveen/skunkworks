# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG PROJECT=Cartographer.Api
WORKDIR /src
COPY Directory.Build.props Cartographer.sln ./
COPY src/Cartographer.Core/ src/Cartographer.Core/
COPY src/Cartographer.Api/ src/Cartographer.Api/
COPY src/Cartographer.Worker/ src/Cartographer.Worker/
RUN dotnet restore "src/${PROJECT}/${PROJECT}.csproj"
RUN dotnet publish "src/${PROJECT}/${PROJECT}.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS api
WORKDIR /app
COPY --from=build /app/publish .
COPY styles/ /app/styles/
COPY render/ /app/render/
COPY db/ /app/db/
COPY debug/ /app/debug/
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Cartographer.Api.dll"]

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS worker-playwright
WORKDIR /pw
COPY --from=build /app/publish .
RUN apt-get update && apt-get install -y --no-install-recommends \
    libglib2.0-0 libnss3 libnspr4 libatk1.0-0 libatk-bridge2.0-0 \
    libcups2 libdrm2 libdbus-1-3 libxcb1 libxkbcommon0 libx11-6 \
    libxcomposite1 libxdamage1 libxext6 libxfixes3 libxrandr2 \
    libgbm1 libpango-1.0-0 libcairo2 libasound2 \
    && rm -rf /var/lib/apt/lists/* \
    && pwsh bin/Debug/net9.0/playwright.ps1 install chromium 2>/dev/null \
    || dotnet exec ./Microsoft.Playwright.dll install chromium 2>/dev/null \
    || (dotnet tool install --global Microsoft.Playwright.CLI \
        && export PATH="$PATH:/root/.dotnet/tools" \
        && playwright install chromium)

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS worker
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends \
    libglib2.0-0 libnss3 libnspr4 libatk1.0-0 libatk-bridge2.0-0 \
    libcups2 libdrm2 libdbus-1-3 libxcb1 libxkbcommon0 libx11-6 \
    libxcomposite1 libxdamage1 libxext6 libxfixes3 libxrandr2 \
    libgbm1 libpango-1.0-0 libcairo2 libasound2 fonts-liberation \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
COPY --from=worker-playwright /root/.cache/ms-playwright /ms-playwright
COPY styles/ /app/styles/
COPY render/ /app/render/
COPY db/ /app/db/
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
ENTRYPOINT ["dotnet", "Cartographer.Worker.dll"]
