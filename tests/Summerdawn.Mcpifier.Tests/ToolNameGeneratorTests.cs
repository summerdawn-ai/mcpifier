using Summerdawn.Mcpifier.Services;

namespace Summerdawn.Mcpifier.Tests;

public class ToolNameGeneratorTests
{
    [Theory]
    [InlineData("GetUserById", "get_user_by_id")]
    [InlineData("listAllUsers", "list_all_users")]
    [InlineData("CreateNewUser", "create_new_user")]
    [InlineData("getUserById", "get_user_by_id")]
    public void GenerateFromOperationId_WithVariousCases_ReturnsSnakeCase(string operationId, string expected)
    {
        // Act
        string result = ToolNameGenerator.GenerateFromOperationId(operationId);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Get user by ID", "get_user_id")]
    [InlineData("List all users", "list_all_users")]
    [InlineData("Create a new user", "create_new_user")]
    [InlineData("Delete user", "delete_user")]
    [InlineData("Fetch user details", "fetch_user_details")]
    public void GenerateFromSummary_WithValidSummaries_ExtractsName(string summary, string expected)
    {
        // Act
        string? result = ToolNameGenerator.GenerateFromSummary(summary);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("/users", "GET", "list_users")]
    [InlineData("/users/{id}", "GET", "get_user")]
    [InlineData("/users", "POST", "create_user")]
    [InlineData("/users/{id}", "DELETE", "delete_user")]
    [InlineData("/users/{id}/posts", "GET", "get_user_posts")]
    [InlineData("/categories/{id}", "GET", "get_category")]
    [InlineData("/addresses/{id}", "PUT", "update_address")]
    [InlineData("/items", "GET", "list_items")]
    public void GenerateFromPathAndType_WithVariousPaths_GeneratesSemanticNames(string path, string method, string expected)
    {
        // Arrange
        var httpMethod = new HttpMethod(method);

        // Act
        string result = ToolNameGenerator.GenerateFromPathAndType(path, httpMethod);

        // Assert
        Assert.Equal(expected, result);
    }
}
