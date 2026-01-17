# See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-alpine AS base
WORKDIR /app
EXPOSE 8080

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
ARG TARGETARCH
WORKDIR /src
COPY ["src/GithubActionsAutoscaler/GithubActionsAutoscaler.csproj", "src/GithubActionsAutoscaler/"]
RUN dotnet restore "src/GithubActionsAutoscaler/GithubActionsAutoscaler.csproj" -a $TARGETARCH
COPY . .
WORKDIR "/src/src/GithubActionsAutoscaler"
RUN dotnet build "GithubActionsAutoscaler.csproj" -c Release -o /app/build -a $TARGETARCH

FROM build AS publish
ARG TARGETARCH
RUN dotnet publish "GithubActionsAutoscaler.csproj" -c Release -o /app/publish -a $TARGETARCH

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GithubActionsAutoscaler.dll"]
