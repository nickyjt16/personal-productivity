using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data;

namespace ProductivityHub.Core;

public static class NoteArchive
{
    // Archives every (not-yet-archived) note linked to the project — called when
    // a project is archived so its notes follow it out of the default view.
    // The caller is responsible for SaveChanges.
    public static async Task<int> ArchiveNotesForProjectAsync(AppDbContext db, Guid projectId,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var notes = await db.Notes
            .Where(n => !n.IsArchived && n.ProjectLinks.Any(l => l.ProjectId == projectId))
            .ToListAsync(ct);
        foreach (var n in notes)
        {
            n.IsArchived = true;
            n.ArchivedAt = now;
        }
        return notes.Count;
    }
}
