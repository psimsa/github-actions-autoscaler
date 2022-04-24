#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["AutoscalerApi/AutoscalerApi.csproj", "AutoscalerApi/"]
RUN dotnet restore "AutoscalerApi/AutoscalerApi.csproj"
COPY . .
WORKDIR "/src/AutoscalerApi"
RUN dotnet build "AutoscalerApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AutoscalerApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AutoscalerApi.dll"]