#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#5.0-buster-slim-arm64v8
FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["AutoFreenom/AutoFreenom.csproj", "AutoFreenom/"]
RUN dotnet restore "AutoFreenom/AutoFreenom.csproj"
COPY . .
WORKDIR "/src/AutoFreenom"
RUN dotnet build "AutoFreenom.csproj" -c Release -o /app/build

FROM build AS publish
#-r linux-arm64
RUN dotnet publish "AutoFreenom.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AutoFreenom.dll"]