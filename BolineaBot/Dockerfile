#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

ARG TARGETPLATFORM=linux/arm64/v8
ARG BUILDPLATFORM=linux/amd64
ARG TARGETRID=linux-arm64
ARG BUILD_CONFIGURATION=Release

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS builder
WORKDIR /src/BolineaBot
COPY . .
RUN dotnet restore "BolineaBot.csproj"
RUN dotnet publish "BolineaBot.csproj" -c $BUILD_CONFIGURATION -r $TARGETRID --no-self-contained -o /app/publish /p:UseAppHost=false

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim AS base
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        software-properties-common \
        libopus0 libopus-dev \
        libsodium23 libsodium-dev \
        youtube-dl \
        ffmpeg \
        python3.10 \
    && rm -rf /var/lib/apt/lists/*

FROM base AS final
WORKDIR /app
COPY --from=builder /app/publish .
ENTRYPOINT ["dotnet", "BolineaBot.dll"]
CMD [ "arg0" ]
