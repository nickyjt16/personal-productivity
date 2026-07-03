namespace ProductivityHub.Api.Models;

// Lightweight project reference embedded in todo/note/bookmark responses.
public record ProjectRef(Guid Id, string Name, string Color);

// Sets the full set of projects an item belongs to.
public record SetProjectsRequest(List<Guid> ProjectIds);
