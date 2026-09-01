FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       libkrb5-3 \
       libgssapi-krb5-2 \
       krb5-user \
       ca-certificates \
       curl \
       dnsutils \
       inetutils-telnet \
       iproute2 \
       iputils-ping \
       lsof \
       mtr-tiny \
       netcat-openbsd \
       openssl \
       procps \
       socat \
       strace \
       tcpdump \
       traceroute \
    && rm -rf /var/lib/apt/lists/*

USER $APP_UID
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Jube.App/Jube.App.csproj", "Jube.App/"]
COPY ["Jube.Data/Jube.Data.csproj", "Jube.Data/"]
COPY ["Accord.Statistics/Accord.Statistics.csproj", "Accord.Statistics/"]
COPY ["Accord.Math/Accord.Math.csproj", "Accord.Math/"]
COPY ["Accord.Math.Core/Accord.Math.Core.csproj", "Accord.Math.Core/"]
COPY ["Accord.Core/Accord.Core.csproj", "Accord.Core/"]
COPY ["Jube.Extensions/Jube.Extensions.csproj", "Jube.Extensions/"]
COPY ["Jube.DynamicEnvironment/Jube.DynamicEnvironment.csproj", "Jube.DynamicEnvironment/"]
COPY ["Jube.Engine/Jube.Engine.csproj", "Jube.Engine/"]
COPY ["Jube.HttpAdaptationProtocol/Jube.HttpAdaptationProtocol.csproj", "Jube.HttpAdaptationProtocol/"]
COPY ["Accord.Genetic/Accord.Genetic.csproj", "Accord.Genetic/"]
COPY ["Accord.MachineLearning/Accord.MachineLearning.csproj", "Accord.MachineLearning/"]
COPY ["Accord.Neuro/Accord.Neuro.csproj", "Accord.Neuro/"]
COPY ["Jube.Parser/Jube.Parser.csproj", "Jube.Parser/"]
COPY ["Jube.Case/Jube.Case.csproj", "Jube.Case/"]
COPY ["Jube.Migrations/Jube.Migrations.csproj", "Jube.Migrations/"]
COPY ["Jube.Service/Jube.Service.csproj", "Jube.Service/"]
COPY ["Jube.Validations/Jube.Validations.csproj", "Jube.Validations/"]
COPY ["Jube.Cache/Jube.Cache.csproj", "Jube.Cache/"]
COPY ["Jube.Dictionary/Jube.Dictionary.csproj", "Jube.Dictionary/"]
COPY ["Jube.HealthCheck/Jube.HealthCheck.csproj", "Jube.HealthCheck/"]
COPY ["Jube.TaskCancellation/Jube.TaskCancellation.csproj", "Jube.TaskCancellation/"]
COPY ["Jube.Tests/Jube.Tests.csproj", "Jube.Tests/"]
COPY ["Jube.ResilientNpgsql/Jube.ResilientNpgsql.csproj", "Jube.ResilientNpgsql/"]
COPY ["Jube.ResilientRedisConnection/Jube.ResilientRedisConnection.csproj", "Jube.ResilientRedisConnection/"]
COPY ["Jube.Tokenisation/Jube.Tokenisation.csproj", "Jube.Tokenisation/"]
COPY ["Jube.ApiTokensCache/Jube.ApiTokensCache.csproj", "Jube.ApiTokensCache/"]
COPY ["Jube.CLI/Jube.CLI.csproj", "Jube.CLI/"]
COPY ["Jube.Cryptography/Jube.Cryptography.csproj", "Jube.Cryptography/"]
COPY ["Jube.Preservation/Jube.Preservation.csproj", "Jube.Preservation/"]
COPY ["Jube.Mfa/Jube.Mfa.csproj", "Jube.Mfa/"]
COPY ["Jube.HttpHeaders/Jube.HttpHeaders.csproj", "Jube.HttpHeaders/"]

RUN dotnet restore "Jube.App/Jube.App.csproj"
COPY . .
WORKDIR "/src/Jube.App"
RUN dotnet build "Jube.App.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Jube.App.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

WORKDIR /src
RUN dotnet publish "Jube.HealthCheck/Jube.HealthCheck.csproj" -c $BUILD_CONFIGURATION -o /app/publish/HealthCheck /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

COPY --from=publish /app/publish/HealthCheck/ /app/HealthCheck/

HEALTHCHECK CMD dotnet /app/HealthCheck/Jube.HealthCheck.dll || exit 1

ENTRYPOINT ["dotnet", "Jube.App.dll"]
