# Mcpifier

Mcpifier is a zero-code MCP (Model Context Protocol) gateway that exposes an existing REST API as an MCP server.

## Overview

Mcpifier can be used as a library, ASP.NET Core middleware, or a command-line server and tool. It supports automatic tool generation from Swagger/OpenAPI specifications using conventions that map REST endpoints to MCP tools, or full customization using JSON configuration files.

### Features

- **Zero Code Required**: Define tools through JSON configuration or generate them from a Swagger/OpenAPI specification file
- **No API Changes**: Works with existing REST APIs without modifications
- **Fully Customizable**: Generate mappings, then edit as needed before serving
- **Standards-Based**: Implements MCP protocol and JSON-RPC 2.0 including MCP Authorization
- **Multiple Transports**: Supports both HTTP and stdio communication
- **Flexible Architecture**: Use as a library, ASP.NET Core middleware, or command-line server and tool
- **Cross-Platform**: Runs on Windows, Linux, and macOS
- **Open Source**: Fully open source under the MIT License

## Getting Started

Detailed installation, usage examples, configuration, and quickstarts live in the individual project READMEs:

- [Core library and configuration structure](src/Summerdawn.Mcpifier/README.md)
- [ASP.NET Core integration](src/Summerdawn.Mcpifier.AspNetCore/README.md)
- [Command-line server and tool](src/Summerdawn.Mcpifier.Server/README.md)

These contain platform-specific guidance on installation (NuGet or .NET tool), typical usage patterns, and examples.

## Architecture

Mcpifier acts as a gateway between MCP clients (e.g., Claude Desktop, VS Code) and REST APIs. It translates MCP tool calls into HTTP requests against configured REST endpoints, returning structured JSON results. The core library can be embedded, or you can use the server project for out-of-the-box running.

    [MCP Client] ⇄ [Mcpifier (Server/Library)] ⇄ [REST API]

### Dependencies

- All projects target .NET 8 and are compatible with trimming and AOT compilation.
- `Summerdawn.Mcpifier.AspNetCore` is split into a separate library and package because it depends on the `Microsoft.AspNetCore.App` framework.

### Current Limitations

- **No Streaming Support**: Responses must be complete; streaming responses are not supported
- **No Server-Sent Events (SSE)**: Only request-response patterns are supported
- **JSON Only**: Binary data, file uploads, and non-JSON content types are not supported

## Repository Structure

The repository is structured as follows:

- [.github](.github/): GitHub Actions CI/CD workflows
- [src](src/)
  - [Summerdawn.Mcpifier](src/Summerdawn.Mcpifier/): Core library providing MCP protocol implementation, Swagger/OpenAPI converter, REST API service, and stdio server
  - [Summerdawn.Mcpifier.AspNetCore](src/Summerdawn.Mcpifier.AspNetCore/): ASP.NET Core integration and HTTP server
  - [Summerdawn.Mcpifier.Server](src/Summerdawn.Mcpifier.Server/): Command-line server and tool
- [tests](tests/): Test projects for all projects

## Versioning and Releases

All projects are built and released concurrently using [the repository's 'release.yml' workflow](.github/workflows/release.yml). They use identical version numbers and are versioned using [Semantic Versioning](https://semver.org/). 

Each project is published as a separate NuGet package, and the command-line server is also published to each GitHub release as a number of standalone, AOT-compiled binaries:

- [GitHub Releases](https://github.com/summerdawn-ai/mcpifier/releases) including server binaries
- NuGet packages:
  - [Summerdawn.Mcpifier](https://www.nuget.org/packages/Summerdawn.Mcpifier)
  - [Summerdawn.Mcpifier.AspNetCore](https://www.nuget.org/packages/Summerdawn.Mcpifier.AspNetCore)
  - [Summerdawn.Mcpifier.Server](https://www.nuget.org/packages/Summerdawn.Mcpifier.Server)

## Development

This repository contains multiple coordinated projects that are built, tested, and released together.

### Prerequisites

- .NET SDK 8.0 or later
- A supported OS (Windows, Linux, or macOS)

### Getting the Code

Clone the repository and restore dependencies:

```
git clone https://github.com/summerdawn-ai/mcpifier.git

cd mcpifier
dotnet restore
```

### Building from Source

The solution can be built using standard .NET CLI commands. All projects are expected to build successfully together:

```
dotnet build
```

Native AOT and trimming are configured as part of the CI pipeline rather than local defaults.

### Running Tests

All tests live under the `tests` directory and are executed as part of every CI run. To run the full test suite locally:

```
dotnet test
```

### Packaging and Publishing

Refer to [the repository's 'release.yml' workflow](.github/workflows/release.yml) for the exact commands used to pack the libraries as NuGet packages and publish the server as platform-dependent, standalone, AOT-compiled binaries.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on how to submit issues and pull requests. This covers coding standards, test practices, and the review process.

## Security

See [SECURITY.md](SECURITY.md) for guidance on reporting security issues.

## Resources

### Model Context Protocol

- [Model Context Protocol specification](https://modelcontextprotocol.io/specification/2025-06-18)
- [MCP Authorization documentation](https://modelcontextprotocol.io/docs/tutorials/security/authorization)

### .NET

- [.NET Native AOT deployment documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot)

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
