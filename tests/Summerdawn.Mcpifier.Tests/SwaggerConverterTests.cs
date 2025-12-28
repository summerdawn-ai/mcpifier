using Microsoft.Extensions.Logging;

using Moq;

using Summerdawn.Mcpifier.Services;

namespace Summerdawn.Mcpifier.Tests;

public class SwaggerConverterTests
{
    [Fact]
    public async Task Convert_WithValidSwagger_ReturnsToolDefinitions()
    {
        // Arrange
        var converter = CreateConverter();
        string swaggerJson = """
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/users/{id}": {
              "get": {
                "operationId": "getUserById",
                "summary": "Get user by ID",
                "parameters": [
                  {
                    "name": "id",
                    "in": "path",
                    "required": true,
                    "schema": {
                      "type": "string"
                    },
                    "description": "User ID"
                  }
                ],
                "responses": {
                  "200": {
                    "description": "Success"
                  }
                }
              }
            }
          }
        }
        """;

        // Act
        var tools = (await converter.ConvertAsync(swaggerJson)).Tools;

        // Assert
        Assert.NotNull(tools);
        Assert.Single(tools);

        var tool = tools[0];
        Assert.Equal("get_user_by_id", tool.Mcp.Name);
        Assert.Equal("Get user by ID", tool.Mcp.Description);
        Assert.Equal("GET", tool.Rest.Method);
        Assert.Equal("/users/{id}", tool.Rest.Path);
        Assert.NotNull(tool.Mcp.InputSchema.Properties);
        Assert.True(tool.Mcp.InputSchema.Properties.ContainsKey("id"));
        Assert.Equal("string", tool.Mcp.InputSchema.Properties["id"].Type);
        Assert.Equal("User ID", tool.Mcp.InputSchema.Properties["id"].Description);
        Assert.Contains("id", tool.Mcp.InputSchema.Required);
    }

    [Fact]
    public async Task Convert_WithQueryParameters_BuildsQueryString()
    {
        // Arrange
        var converter = CreateConverter();
        string swaggerJson = """
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/users": {
              "get": {
                "operationId": "listUsers",
                "summary": "List users",
                "parameters": [
                  {
                    "name": "page",
                    "in": "query",
                    "required": false,
                    "schema": {
                      "type": "integer"
                    }
                  },
                  {
                    "name": "limit",
                    "in": "query",
                    "required": true,
                    "schema": {
                      "type": "integer"
                    }
                  }
                ],
                "responses": {
                  "200": {
                    "description": "Success"
                  }
                }
              }
            }
          }
        }
        """;

        // Act
        var tools = (await converter.ConvertAsync(swaggerJson)).Tools;

        // Assert
        Assert.NotNull(tools);
        Assert.Single(tools);

        var tool = tools[0];
        Assert.Equal("list_users", tool.Mcp.Name);
        Assert.NotNull(tool.Rest.Query);
        Assert.Contains("page={page}", tool.Rest.Query);
        Assert.Contains("limit={limit}", tool.Rest.Query);
        Assert.Contains("limit", tool.Mcp.InputSchema.Required);
        Assert.DoesNotContain("page", tool.Mcp.InputSchema.Required);
    }

    [Fact]
    public async Task Convert_WithRequestBody_FlattensProperties()
    {
        // Arrange
        var converter = CreateConverter();
        string swaggerJson = """
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/users": {
              "post": {
                "operationId": "createUser",
                "summary": "Create a user",
                "requestBody": {
                  "required": true,
                  "content": {
                    "application/json": {
                      "schema": {
                        "type": "object",
                        "required": ["name", "email"],
                        "properties": {
                          "name": {
                            "type": "string",
                            "description": "User's name"
                          },
                          "email": {
                            "type": "string",
                            "format": "email",
                            "description": "User's email"
                          },
                          "age": {
                            "type": "integer",
                            "format": "int32"
                          }
                        }
                      }
                    }
                  }
                },
                "responses": {
                  "201": {
                    "description": "Created"
                  }
                }
              }
            }
          }
        }
        """;

        // Act
        var tools = (await converter.ConvertAsync(swaggerJson)).Tools;

        // Assert
        Assert.NotNull(tools);
        Assert.Single(tools);

        var tool = tools[0];
        Assert.Equal("create_user", tool.Mcp.Name);
        Assert.Equal("POST", tool.Rest.Method);
        Assert.NotNull(tool.Rest.Body);
        Assert.Contains("\"name\": {name}", tool.Rest.Body);
        Assert.Contains("\"email\": {email}", tool.Rest.Body);
        Assert.Contains("\"age\": {age}", tool.Rest.Body);

        Assert.NotNull(tool.Mcp.InputSchema.Properties);
        Assert.True(tool.Mcp.InputSchema.Properties.ContainsKey("name"));
        Assert.True(tool.Mcp.InputSchema.Properties.ContainsKey("email"));
        Assert.True(tool.Mcp.InputSchema.Properties.ContainsKey("age"));

        Assert.Equal("string", tool.Mcp.InputSchema.Properties["name"].Type);
        Assert.Equal("email", tool.Mcp.InputSchema.Properties["email"].Format);
        Assert.Equal("integer", tool.Mcp.InputSchema.Properties["age"].Type);

        Assert.Contains("name", tool.Mcp.InputSchema.Required);
        Assert.Contains("email", tool.Mcp.InputSchema.Required);
        Assert.DoesNotContain("age", tool.Mcp.InputSchema.Required);
    }

    [Fact]
    public async Task Convert_WithoutOperationId_GeneratesToolNameFromPath()
    {
        // Arrange
        var converter = CreateConverter();
        string swaggerJson = """
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/users/{id}/posts": {
              "get": {
                "summary": "Get user posts",
                "responses": {
                  "200": {
                    "description": "Success"
                  }
                }
              }
            }
          }
        }
        """;

        // Act
        var tools = (await converter.ConvertAsync(swaggerJson)).Tools;

        // Assert
        Assert.NotNull(tools);
        Assert.Single(tools);

        var tool = tools[0];
        Assert.Equal("get_users_id_posts", tool.Mcp.Name);
    }

    [Fact]
    public async Task Convert_WithMultipleOperations_ReturnsAllTools()
    {
        // Arrange
        var converter = CreateConverter();
        string swaggerJson = """
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/users": {
              "get": {
                "operationId": "listUsers",
                "responses": {
                  "200": {
                    "description": "Success"
                  }
                }
              },
              "post": {
                "operationId": "createUser",
                "responses": {
                  "201": {
                    "description": "Created"
                  }
                }
              }
            },
            "/users/{id}": {
              "get": {
                "operationId": "getUser",
                "parameters": [
                  {
                    "name": "id",
                    "in": "path",
                    "required": true,
                    "schema": {
                      "type": "string"
                    }
                  }
                ],
                "responses": {
                  "200": {
                    "description": "Success"
                  }
                }
              },
              "delete": {
                "operationId": "deleteUser",
                "parameters": [
                  {
                    "name": "id",
                    "in": "path",
                    "required": true,
                    "schema": {
                      "type": "string"
                    }
                  }
                ],
                "responses": {
                  "204": {
                    "description": "No Content"
                  }
                }
              }
            }
          }
        }
        """;

        // Act
        var tools = (await converter.ConvertAsync(swaggerJson)).Tools;

        // Assert
        Assert.NotNull(tools);
        Assert.Equal(4, tools.Count);

        Assert.Contains(tools, t => t.Mcp.Name == "list_users" && t.Rest.Method == "GET");
        Assert.Contains(tools, t => t.Mcp.Name == "create_user" && t.Rest.Method == "POST");
        Assert.Contains(tools, t => t.Mcp.Name == "get_user" && t.Rest.Method == "GET");
        Assert.Contains(tools, t => t.Mcp.Name == "delete_user" && t.Rest.Method == "DELETE");
    }

    [Theory]
    [InlineData("Resources/swagger.json","Resources/mappings.json")]
    [InlineData("Resources/complex-swagger.json", "Resources/complex-mappings.json")]
    public async Task LoadAndConvert_WithGivenSwaggerFile_SavesExpectedMappingsFile(string swaggerPath, string mappingsPath)
    {
        // Arrange
        var converter = CreateConverter();
        string tempOutputFile = Path.Combine(Path.GetTempPath(), $"mappings_{Guid.NewGuid()}.json");

        try
        {
            // Act
            await converter.LoadAndConvertAsync(swaggerPath, tempOutputFile);

            // Assert
            string expectedMappingsJson = await File.ReadAllTextAsync(mappingsPath);
            string actualMappingsJson = await File.ReadAllTextAsync(tempOutputFile);

            Assert.Equal(expectedMappingsJson, actualMappingsJson);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempOutputFile))
            {
                File.Delete(tempOutputFile);
            }
        }
    }
    
    private static SwaggerConverter CreateConverter()
    {
        var mockFactory = new Mock<IHttpClientFactory>();
        var mockLogger = new Mock<ILogger<SwaggerConverter>>();
        return new SwaggerConverter(mockFactory.Object, mockLogger.Object);
    }
}
