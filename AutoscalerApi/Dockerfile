#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 8080

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG TARGETARCH
WORKDIR /src
COPY ["AutoscalerApi/AutoscalerApi.csproj", "AutoscalerApi/"]
RUN dotnet restore "AutoscalerApi/AutoscalerApi.csproj" -a $TARGETARCH
COPY . .
WORKDIR "/src/AutoscalerApi"
RUN dotnet build "AutoscalerApi.csproj" -c Release -o /app/build -a $TARGETARCH

FROM build AS publish
ARG TARGETARCH
RUN dotnet publish "AutoscalerApi.csproj" -c Release -o /app/publish -a $TARGETARCH

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AutoscalerApi.dll"]
