FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /app

COPY *.sln .
COPY BitbucketApi.csproj .

RUN dotnet restore *.sln

COPY . .

WORKDIR /app
RUN dotnet publish -c Release -o /release

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=80
COPY --from=build /release ./
ENTRYPOINT ["dotnet", "BitbucketApi.dll"]
