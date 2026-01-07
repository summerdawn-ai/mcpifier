# Agent Instructions

## Coding Style

- DO follow the code style preferences and rules defined in [.editorconfig](.editorconfig).
- DO run `dotnet format` before submitting your changes to automatically apply code style preferences and rules to your code.
- DO fix any code style (and other) warnings shown by `dotnet build`.

### Additional C# Guidelines

- DO NOT add null checks to (constructor and other) arguments UNLESS the code is in reusable library in a public repository OR packaged and published as a NuGet package.
- DO NOT create private fields just to hold constructor arguments unless there's a specific need (e.g. `foo = foo.Validated()`). With primary constructors, the parameter is directly accessible throughout the class.
- DO NOT use fully qualified type names with their full namespaces. Instead, add a `using` statement at the top of the file and reference the type by its short name. For example, use `[JsonIgnore]` with `using System.Text.Json.Serialization;` rather than `[System.Text.Json.Serialization.JsonIgnore]`.

  So altogether, DO NOT this:
  
  ```csharp
  public class CommandFactory
  {
      private readonly IAuthenticationService _authService;
      private readonly IVibeApiClient _apiClient;
  
      public CommandFactory(IAuthenticationService authService, IVibeApiClient apiClient)
      {
          _authService = authService ?? throw new ArgumentNullException(nameof(authService));
          _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
      }
      // ...
  ```
  
  DO this:

  ```csharp
  public class CommandFactory(IAuthenticationService authService, IVibeApiClient apiClient)
  {
  // ...
  ```

## Agentic working

- When you get a task, DO be diligent about it and do a build, run tests, get a code review etc. if appropriate.
- DO NOT, however, start making additional changes that are outside the scope of the task because of unrelated code review feedback or because
  the change inadvertently breaks all test cases. Instead, DO stop and ask for guidance, UNLESS you were explicitly instructed to keep going.
  The scope of your work should be proportional to the scope of the given task - if it grows, ask if that is intended.
