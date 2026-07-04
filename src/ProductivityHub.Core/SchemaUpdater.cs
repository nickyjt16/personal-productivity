using Microsoft.EntityFrameworkCore;

namespace ProductivityHub.Core.Data;

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

            CREATE TABLE IF NOT EXISTS "Secrets" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Secrets" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "ClientId" TEXT NULL,
                "Value" TEXT NULL,
                "ExpiresOn" TEXT NOT NULL,
                "Notes" TEXT NULL,
                "NotifyList" TEXT NULL,
                "Link" TEXT NULL,
                "CreatedAt" INTEGER NOT NULL,
                "UpdatedAt" INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS "SecretProjects" (
                "SecretId" TEXT NOT NULL,
                "ProjectId" TEXT NOT NULL,
                CONSTRAINT "PK_SecretProjects" PRIMARY KEY ("SecretId", "ProjectId"),
                CONSTRAINT "FK_SecretProjects_Secrets_SecretId" FOREIGN KEY ("SecretId") REFERENCES "Secrets" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_SecretProjects_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_SecretProjects_ProjectId" ON "SecretProjects" ("ProjectId");

            CREATE TABLE IF NOT EXISTS "VaultConfig" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_VaultConfig" PRIMARY KEY,
                "Salt" TEXT NOT NULL,
                "Iterations" INTEGER NOT NULL,
                "Verifier" TEXT NOT NULL,
                "Hint" TEXT NULL,
                "CreatedAt" INTEGER NOT NULL
            );
            """;

        await db.Database.ExecuteSqlRawAsync(sql, ct);

        // Columns added to existing tables after their initial creation.
        await AddColumnIfMissingAsync(db, "Secrets", "NotifyList", "TEXT", ct);
        await AddColumnIfMissingAsync(db, "Secrets", "Link", "TEXT", ct);
        await AddColumnIfMissingAsync(db, "Todos", "RecurUnit", "INTEGER NOT NULL DEFAULT 0", ct);
        await AddColumnIfMissingAsync(db, "Todos", "RecurInterval", "INTEGER NOT NULL DEFAULT 0", ct);
    }

    // Idempotent ALTER TABLE ADD COLUMN — SQLite can't do "IF NOT EXISTS" for
    // columns, so we check pragma_table_info first.
    private static async Task AddColumnIfMissingAsync(AppDbContext db, string table, string column, string type,
        CancellationToken ct)
    {
        var columns = await db.Database
            .SqlQueryRaw<string>($"SELECT name AS \"Value\" FROM pragma_table_info('{table}')")
            .ToListAsync(ct);
        if (!columns.Contains(column))
            await db.Database.ExecuteSqlRawAsync($"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {type}", ct);
    }
}
