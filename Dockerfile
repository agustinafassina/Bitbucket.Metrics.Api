FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /app

COPY *.sln .
COPY Template.Models/*.csproj Template.Models/
COPY Template.Repository/*.csproj Template.Repository/
COPY Template.Services/*.csproj Template.Services/
COPY Template.Api/*.csproj Template.Api/

RUN dotnet restore TemplateApi.sln

COPY . .
RUN dotnet publish Template.Api/Template.Api.csproj -c Release -o /release

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=80
COPY --from=build /release ./
ENTRYPOINT ["dotnet", "Template.Api.dll"]