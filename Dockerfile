FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /app

COPY *.sln .
COPY Bitbucket.Metrics.Models/*.csproj Bitbucket.Metrics.Models/
COPY Bitbucket.Metrics.Repository/*.csproj Bitbucket.Metrics.Repository/
COPY Bitbucket.Metrics.Services/*.csproj Bitbucket.Metrics.Services/
COPY Bitbucket.Metrics.Api/*.csproj Bitbucket.Metrics.Api/

RUN dotnet restore Bitbucket.Metrics.sln

COPY . .
RUN dotnet publish Bitbucket.Metrics.Api/Bitbucket.Metrics.Api.csproj -c Release -o /release

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=80
COPY --from=build /release ./
ENTRYPOINT ["dotnet", "Bitbucket.Metrics.Api.dll"]