# Agent Instructions

## Coding Style

All instructions in this section are for C# source code.

- DO use primary constructors for classes unless there is special initialization logic that cannot be done in a field initializer. Special initialization logic refers to operations in the constructor body beyond simple field assignment, such as validation, transformation, or computed initialization (e.g., `foo = foo.Validated()`).
- DO NOT create private fields just to hold constructor arguments unless there's a specific need (e.g. `foo = foo.Validated()`). With primary constructors, the parameter is directly accessible throughout the class.
- DO NOT add null checks to (constructor and other) arguments UNLESS the code is in reusable library in a public repository OR packaged and published as a NuGet package.
- DO use camelCase for private fields, no underscore prefix.

  So altogether, DO NOT this:
  
  ```
  public class CommandFactory
  {
      private readonly IAuthenticationService _authService;
      private readonly IVibeApiClient _apiClient;
  
      public CommandFactory(IAuthenticationService authService, IVibeApiClient apiClient)
      {
          _authService = authService ?? throw new ArgumentNullException(nameof(authService));
          _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
      }
  ```
  
  DO this:

  ```
  public class CommandFactory(IAuthenticationService authService, IVibeApiClient apiClient)
  ```

Coding style, continued

- DO use explicit access modifiers on all members, including private fields _and_ interface members.
- DO use explicit types, not `var`, for primitive types that are C# keywords: `bool`, `int`, `float`, `string`, `object`, etc., including nullables (e.g., `int?`, `string?`). This improves readability at a glance.
- DO use collection expressions (e.g., `[]`, `[1, 2, 3]`) for creating collections whenever possible (C# 12+). This is preferred over collection initializers (`new List<T> { item1, item2 }`) and factory methods (`Array.Empty<T>()`).
- DO NOT use fully qualified type names with their full namespaces. Instead, add a `using` statement at the top of the file and reference the type by its short name. For example, use `[JsonIgnore]` with `using System.Text.Json.Serialization;` rather than `[System.Text.Json.Serialization.JsonIgnore]`.
- DO NOT add unnecessary `using` statements. Depending on the project type, some projects (e.g., ASP.NET Core) will have implicit global usings provided by the SDK. Do not duplicate those in individual files.
- DO NOT use the `this.` prefix unnecessarily. Only use it when required to disambiguate (e.g., when a local variable shadows a field name) or for absolute clarity in complex scenarios. Default to using the field or property name directly.

## Agentic working

- When you get a task, DO be diligent about it and do a build, run tests, get a code review etc. if appropriate.
- DO NOT, however, start making additional changes that are outside the scope of the task because of unrelated code review feedback or because
  the change inadvertently breaks all test cases. Instead, DO stop and ask for guidance, UNLESS you were explicitly instructed to keep going.
  The scope of your work should be proportional to the scope of the given task - if it grows, ask if that is intended.
