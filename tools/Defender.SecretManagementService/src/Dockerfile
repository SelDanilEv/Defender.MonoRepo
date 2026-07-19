FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /repo

# Shared build props for Defender.Common
COPY ["src/Directory.Build.props", "src/Directory.Build.props"]
COPY ["src/Directory.Packages.props", "src/Directory.Packages.props"]

# Dependencies
COPY ["src/Defender.Common/src/Defender.Common", "src/Defender.Common/src/Defender.Common"]

# Service source
COPY ["tools/Defender.SecretManagementService/src", "tools/Defender.SecretManagementService/src"]

WORKDIR "/repo/tools/Defender.SecretManagementService/src/WebApi"
RUN dotnet restore "WebApi.csproj"
RUN dotnet build "WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
RUN groupadd --system --gid 10001 appgroup \
    && useradd --system --uid 10001 --gid appgroup --no-create-home appuser
COPY --from=publish --chown=10001:10001 /app/publish .

USER 10001:10001
CMD ASPNETCORE_URLS=http://*:$PORT dotnet WebApi.dll
