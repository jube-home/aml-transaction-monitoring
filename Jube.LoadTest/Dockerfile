FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base

USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       docker.io \
       ca-certificates \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Jube.LoadTest/Jube.LoadTest.csproj", "Jube.LoadTest/"]
RUN dotnet restore "Jube.LoadTest/Jube.LoadTest.csproj"
COPY ["Jube.LoadTest/", "Jube.LoadTest/"]
WORKDIR "/src/Jube.LoadTest"
RUN dotnet build "Jube.LoadTest.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Jube.LoadTest.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Container CPU/RAM sampling shells out to `docker`, which needs the host's docker.sock
# bind-mounted in at runtime, e.g. -v /var/run/docker.sock:/var/run/docker.sock
ENTRYPOINT ["dotnet", "Jube.LoadTest.dll"]
