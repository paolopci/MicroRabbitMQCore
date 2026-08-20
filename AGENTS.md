# Repository Guidelines

## Project Structure & Module Organization

`Course.slnx` is the solution entry point. `Domain/MicroRabbitMQ.Domain.Core/` contains transport-agnostic domain contracts: commands, events, messages, and the `IEventBus`/`IEventHandler` abstractions. `MicroRabbit.Infra.Bus/` contains the RabbitMQ-oriented infrastructure implementation and references the domain project. Keep new contracts in the domain project; put broker-specific code, configuration, and adapters in infrastructure. Do not edit generated `bin/` or `obj/` files.

## Build, Test, and Development Commands

From the repository root, use:

```powershell
dotnet restore Course.slnx
dotnet build Course.slnx
dotnet test Course.slnx
dotnet format Course.slnx --verify-no-changes
```

`restore` resolves NuGet packages, `build` compiles both projects, and the format command checks style without changing files. There is currently no test project, so `dotnet test` is a readiness check until tests are added. This repository provides libraries rather than a runnable application; add a host or integration-test project before expecting a local service to start.

## Coding Style & Naming Conventions

Use C# with nullable reference types enabled and implicit usings. Follow the existing four-space indentation and block-scoped namespace style. Use PascalCase for public types, members, and generic parameters (`RabbitMQBus`, `IEventHandler<TEvent>`); prefix interfaces with `I`. Keep message contracts small and immutable where practical. Prefer `async` APIs that return `Task`, validate inputs at boundaries, and avoid broker dependencies in `Domain`.

## Testing Guidelines

Add tests in a dedicated `*.Tests` project and include it in `Course.slnx`. Use xUnit and name tests as `MethodName_Scenario_ExpectedResult`, for example `Publish_WhenEventIsValid_DelegatesToBroker`. Unit-test domain behavior without RabbitMQ; cover the infrastructure adapter with focused integration tests using isolated broker settings. Run `dotnet test Course.slnx` before opening a pull request.

## Commit & Pull Request Guidelines

Existing history uses short, imperative, capitalized subjects, such as `Implemented Interfaces and Classes for Domain`. Keep commits focused and describe the affected layer. Pull requests should explain the behavior change, list validation performed, link related issues when available, and call out RabbitMQ configuration or message-contract changes. Include logs or screenshots only when they clarify observable behavior.

## Configuration & Security

Never commit RabbitMQ credentials, connection strings, or `.env` files. Use local environment variables or ignored development configuration, and document any required setting in the pull request.
