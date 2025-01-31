#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim-arm64v8 AS base
WORKDIR /app

RUN apt-get clean && apt-get update
RUN apt-get install -y software-properties-common
RUN apt-get install -y libopus0 libopus-dev libsodium23 libsodium-dev youtube-dl ffmpeg python3.10

FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim-arm64v8 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BolineaBot/BolineaBot.csproj", "BolineaBot/"]
RUN dotnet restore "./BolineaBot/BolineaBot.csproj"
COPY . .
WORKDIR "/src/BolineaBot"
RUN dotnet build "./BolineaBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BolineaBot.csproj" --runtime "linux-arm64" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BolineaBot.dll"]
CMD [ "arg0" ]