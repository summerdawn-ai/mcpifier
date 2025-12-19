using Microsoft.Extensions.Logging;

using Moq;

using Summerdawn.Mcpify.Services;

namespace Summerdawn.Mcpify.Tests;

public class SwaggerToMappingConverterTests
{
    private SwaggerToMappingConverter CreateConverter()
    {
        var mockLogger = new Mock<ILogger<SwaggerToMappingConverter>>();
        return new SwaggerToMappingConverter(mockLogger.Object);
    }

    [Fact]
    public async Task ConvertAsync_WithValidSwagger_ReturnsToolDefinitions()
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

        string tempFile = Path.Combine(Path.GetTempPath(), $"swagger-{Guid.NewGuid()}.json");
        try
        {
            await File.WriteAllTextAsync(tempFile, swaggerJson);

            // Act
            var tools = await converter.ConvertAsync(tempFile);

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
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ConvertAsync_WithQueryParameters_BuildsQueryString()
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

        string tempFile = Path.Combine(Path.GetTempPath(), $"swagger-{Guid.NewGuid()}.json");
        try
        {
            await File.WriteAllTextAsync(tempFile, swaggerJson);

            // Act
            var tools = await converter.ConvertAsync(tempFile);

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
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ConvertAsync_WithRequestBody_FlattensProperties()
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

        string tempFile = Path.Combine(Path.GetTempPath(), $"swagger-{Guid.NewGuid()}.json");
        try
        {
            await File.WriteAllTextAsync(tempFile, swaggerJson);

            // Act
            var tools = await converter.ConvertAsync(tempFile);

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
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ConvertAsync_WithoutOperationId_GeneratesToolNameFromPath()
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

        string tempFile = Path.Combine(Path.GetTempPath(), $"swagger-{Guid.NewGuid()}.json");
        try
        {
            await File.WriteAllTextAsync(tempFile, swaggerJson);

            // Act
            var tools = await converter.ConvertAsync(tempFile);

            // Assert
            Assert.NotNull(tools);
            Assert.Single(tools);
            
            var tool = tools[0];
            Assert.Equal("get_users_id_posts", tool.Mcp.Name);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ConvertAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var converter = CreateConverter();
        string nonExistentFile = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid()}.json");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => converter.ConvertAsync(nonExistentFile));
    }

    [Fact]
    public async Task ConvertAsync_WithMultipleOperations_ReturnsAllTools()
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

        string tempFile = Path.Combine(Path.GetTempPath(), $"swagger-{Guid.NewGuid()}.json");
        try
        {
            await File.WriteAllTextAsync(tempFile, swaggerJson);

            // Act
            var tools = await converter.ConvertAsync(tempFile);

            // Assert
            Assert.NotNull(tools);
            Assert.Equal(4, tools.Count);
            
            Assert.Contains(tools, t => t.Mcp.Name == "list_users" && t.Rest.Method == "GET");
            Assert.Contains(tools, t => t.Mcp.Name == "create_user" && t.Rest.Method == "POST");
            Assert.Contains(tools, t => t.Mcp.Name == "get_user" && t.Rest.Method == "GET");
            Assert.Contains(tools, t => t.Mcp.Name == "delete_user" && t.Rest.Method == "DELETE");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
