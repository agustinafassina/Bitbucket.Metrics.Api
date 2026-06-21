## TemplateApi (.NET 10) ✨
Starter Web API template using .NET 10 with example controllers, services, mapping and middleware. It provides a foundation for building Web APIs with JWT authentication, global exception handling, and Swagger.<br>
Status: template ready for customization. <br>
Forked from https://github.com/agustinafassina/TemplateApi.Net8

## Requirements ✅
- 🧰 .NET 10 SDK
- 🐳 (Optional) Docker

## Solution structure 📦 (MVC + OOP)
- **Template.Api**: entry point, controllers, configs (Swagger, Mapperly), middleware, validators, request contracts.
- **Template.Services**: application services and interfaces; registered via `AddApplicationServices()`.
- **Template.Repository**: data access (repositories); registered via `AddRepositories()`.
- **Template.Models**: DTOs and shared models.
- **Template.Unittests**: xUnit unit tests for `Template.Services` (project file: `TemplateApi.Tests.csproj`).

## Unit tests 🧪
From the repository root (same folder as `TemplateApi.sln`):

```
dotnet test
```

Run only the test project:

```
dotnet test Template.Unittests/TemplateApi.Tests.csproj
```

Optional: verbose output or filter by display name:

```
dotnet test --verbosity normal
dotnet test --filter "FullyQualifiedName~ItemServiceTests"
```

## Dependency Injection (DI) 🔌
Repositories and services are registered with extension methods so `Program.cs` stays clean:
- `builder.Services.AddRepositories();` — registers all repository interfaces/implementations.
- `builder.Services.AddApplicationServices();` — registers all application services (depends on repositories).

Lifetimes: repositories use **Singleton** for in-memory store; when you switch to Entity Framework, use **Scoped** for `DbContext` and repository implementations.

## Run locally ▶️
1. Restore and build:
```
dotnet build
```

2. Run the API:
```
dotnet run --project Template.Api
```

By default, when running in Development, Swagger should be available at `http://localhost:{port}/swagger`.

## CI/CD — GitHub Actions (AWS OIDC · ECR) 🚀

Workflow: [`.github/workflows/aws-ecr-ecs.yml`](.github/workflows/aws-ecr-ecs.yml)

| Branch / event | What runs |
|----------------|-----------|
| PR → `main` | Lint (`dotnet format`), build, unit tests, Semgrep, TruffleHog |
| Push → `main` | Same + Docker build and push to ECR |

Setup (OIDC trust policy, IAM, GitHub variables): [`.github/aws/README.md`](.github/aws/README.md).

Azure DevOps pipelines remain under [`azure-pipelines/`](azure-pipelines/).

## Docker 🐳
Build the image and run the container:
```
docker build -f Dockerfile -t templateapi:latest .
docker run -d -p 8787:80 -e "ASPNETCORE_ENVIRONMENT=Development" --name templateapi templateapi:latest
# Swagger: http://localhost:8787/swagger/index.html
```

## Authentication (JWT / Auth0 example) 🔐
The template includes support for JWT authentication. Configure the values in `appsettings.json` or via environment variables. Example JSON configuration:
```
"Auth0App1": {
  "Issuer": "https://your-domain.auth0.com/",
  "Audience": "Your-Audience"
},
"Auth0App2": {
  "Issuer": "https://another-issuer/",
  "Audience": "Another-Audience"
}
```

And in controllers you can use:
```
[Authorize(AuthenticationSchemes = "Auth0App1")]
[Authorize(AuthenticationSchemes = "Auth0App2")]
```

## Request validation ✅
The API uses **FluentValidation** for request DTOs (e.g. `ItemCreateDto`). Validators live in `Template.Api/Validators/` and are registered in DI; controllers inject `IValidator<T>` and validate before calling services.

## Configuration ⚙️
- Use `appsettings.json` and `appsettings.Development.json` for environment-specific values.
- Environment variables prefixed with `ASPNETCORE_` affect host behavior.

## Example: Version endpoint ⚡
This template exposes a simple version endpoint in `ItemController`.

- Request:

```
GET /api/v1/item/version
```

- curl example:

```bash
curl -i http://localhost:5000/api/v1/item/version
```

- Response 200 (example):
```json
"v.1.0.0"
```

## Contributing 🤝
1. Fork the repo
2. Create a branch (`feature/name`)
3. Open a Pull Request describing your changes

## License 📜
This repository is a template created by Agustina Fassina.