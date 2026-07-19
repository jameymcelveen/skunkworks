# One container runs everything: dashboard server + periodic scan sweep.
# Build context is the REPO ROOT (Railway convention when dockerfilePath points
# into a subdir). Local build: docker build -f jobscan/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /build
COPY jobscan/nuget.config jobscan/
COPY jobscan/src/ jobscan/src/
RUN dotnet publish jobscan/src/Jobscan/Jobscan.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
COPY jobscan/profiles/ profiles/
COPY jobscan/web/ web/
ENV JOBSCAN_ROOT=/app
ENV SCAN_INTERVAL_HOURS=6
EXPOSE 8080
ENTRYPOINT ["dotnet", "jobscan.dll", "--daemon"]
