namespace ProductivityHub.Api.Models;

// Lightweight project reference embedded in todo/note/bookmark responses.
public record ProjectRef(Guid Id, string Name, string Color);

// Sets the full set of projects an item belongs to.
public record SetProjectsRequest(List<Guid> ProjectIds);

// Lightweight references between secrets and environments.
public record EnvRef(Guid Id, string Name, ProductivityHub.Core.Data.Entities.EnvironmentType Type);
public record SecretRef(Guid Id, string Name);

// Sets the full set of environments a secret applies to.
public record SetEnvironmentsRequest(List<Guid> EnvironmentIds);
