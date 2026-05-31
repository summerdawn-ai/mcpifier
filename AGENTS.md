# Agent Instructions

## General rules

- DO follow the code style preferences and rules defined in [.editorconfig](.editorconfig).
- DO run `dotnet format` before submitting your changes to automatically apply code style preferences and rules to your code.
- DO fix any code style (and other) warnings shown by `dotnet build`.

### Agentic working

- When you get a task, DO be diligent about it and do a build, run tests, get a code review etc. if appropriate.
- DO NOT, however, start making additional changes that are outside the scope of the task because of unrelated code review feedback or because
  the change inadvertently breaks all test cases. Instead, DO stop and ask for guidance, UNLESS you were explicitly instructed to keep going.
  The scope of your work should be proportional to the scope of the given task - if it grows, ask if that is intended.

## C# specific rules

### Coding style

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
  }
  ```

### XML documentation comments

- DO add XML documentation to public types and public members when it improves clarity; document non-public members when they are especially central, reused, non-obvious, or implement substantial domain logic.
- DO use multi-line XML doc tags with the opening and closing tags on their own lines:
  ```csharp
  /// <summary>
  /// Summary text.
  /// </summary>
  ```
- DO NOT use single-line summary tags (`/// <summary>Text.</summary>`).
- DO follow standard wording conventions in summary text:
  - Data/model types: **Represents ...**
  - Enums and enum members: **Specifies ...** or **Indicates ...**
  - Mutable properties: **Gets or sets the ...**
  - Read-only properties: **Gets the ...**
  - Methods and behavioral types: a concise verb that matches their role (e.g. *Computes ...*, *Fetches ...*, *Determines whether ...*)
- DO use third-person, present tense, complete sentences that describe purpose rather than implementation. Avoid repeating the member name or obvious type information.
- DO add `<param>` and `<returns>` when they add meaningful value. Avoid documenting parameters on non-public members unless especially useful; avoid documenting exceptions unless they are particularly relevant to callers.
- DO add `<remarks>` when a summary alone is insufficient to describe important behavior, constraints, or multi-step protocols.
- DO NOT duplicate inherited documentation. Prefer automatic inheritance where available; use `<inheritdoc>` only when required by build warnings or tooling.

### Inline comments

- DO use inline comments inside method bodies to explain *intent*, *rationale*, invariants, and non-obvious protocol decisions - not to restate what the code already says clearly.
- DO add inline comments in larger methods to delineate logical phases or steps (e.g. `// --- Phase 1: read snapshots ---`), especially in service, builder, orchestration, or heuristic-heavy code.
- DO add a brief leading comment or XML doc to non-public methods when the overall purpose would otherwise have to be inferred from a long implementation.
- DO NOT add comments that simply repeat the method name or the type of statement being executed.

### General comments

- DO keep all comments accurate, concise, and non-redundant.
- DO update comments whenever the behavior they describe changes.
- DO NOT leave comments describing old behavior after a refactor.
