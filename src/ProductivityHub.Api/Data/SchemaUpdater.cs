using Microsoft.EntityFrameworkCore;

namespace ProductivityHub.Api.Data;

// EnsureCreated() only builds the schema when the database file doesn't yet
// exist, so it won't add new tables to a database created by an earlier version.
// This applies idempotent CREATE TABLE IF NOT EXISTS statements for tables added
// after the initial release, preserving existing data.
//
// Column types match EF's conventions for this model: Guid/string -> TEXT,
// enum -> INTEGER, and DateTimeOffset -> INTEGER (we store it via
// DateTimeOffsetToBinaryConverter, see AppDbContext).
public static class SchemaUpdater
{
    public static async Task ApplyAsync(AppDbContext db, CancellationToken ct = default)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS "Projects" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Projects" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "Description" TEXT NULL,
                "Color" TEXT NOT NULL,
                "Status" INTEGER NOT NULL,
                "CreatedAt" INTEGER NOT NULL,
                "UpdatedAt" INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS "TodoProjects" (
                "TodoItemId" TEXT NOT NULL,
                "ProjectId" TEXT NOT NULL,
                CONSTRAINT "PK_TodoProjects" PRIMARY KEY ("TodoItemId", "ProjectId"),
                CONSTRAINT "FK_TodoProjects_Todos_TodoItemId" FOREIGN KEY ("TodoItemId") REFERENCES "Todos" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_TodoProjects_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_TodoProjects_ProjectId" ON "TodoProjects" ("ProjectId");

            CREATE TABLE IF NOT EXISTS "NoteProjects" (
                "NoteId" TEXT NOT NULL,
                "ProjectId" TEXT NOT NULL,
                CONSTRAINT "PK_NoteProjects" PRIMARY KEY ("NoteId", "ProjectId"),
                CONSTRAINT "FK_NoteProjects_Notes_NoteId" FOREIGN KEY ("NoteId") REFERENCES "Notes" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_NoteProjects_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_NoteProjects_ProjectId" ON "NoteProjects" ("ProjectId");

            CREATE TABLE IF NOT EXISTS "BookmarkProjects" (
                "BookmarkId" TEXT NOT NULL,
                "ProjectId" TEXT NOT NULL,
                CONSTRAINT "PK_BookmarkProjects" PRIMARY KEY ("BookmarkId", "ProjectId"),
                CONSTRAINT "FK_BookmarkProjects_Bookmarks_BookmarkId" FOREIGN KEY ("BookmarkId") REFERENCES "Bookmarks" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_BookmarkProjects_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_BookmarkProjects_ProjectId" ON "BookmarkProjects" ("ProjectId");
            """;

        await db.Database.ExecuteSqlRawAsync(sql, ct);
    }
}
