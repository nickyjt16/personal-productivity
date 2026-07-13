using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ProductivityHub.Api.Tests;

public class ApiTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web); // camelCase + case-insensitive

    private readonly HttpClient _client;

    public ApiTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
        // Isolate each test (tests in a class run sequentially).
        _client.PostAsync("/api/data/clear", null).GetAwaiter().GetResult();
    }

    private async Task<JsonElement> GetArray(string url)
        => await _client.GetFromJsonAsync<JsonElement>(url, Json);

    private static string Id(JsonElement el) => el.GetProperty("id").GetString()!;

    [Fact]
    public async Task Todo_can_be_created_listed_toggled_and_deleted()
    {
        var create = await _client.PostAsJsonAsync("/api/todos",
            new { title = "Write tests", priority = "High" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var todo = await create.Content.ReadFromJsonAsync<JsonElement>(Json);
        var id = Id(todo);

        var list = await GetArray("/api/todos");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("Write tests", list[0].GetProperty("title").GetString());

        var toggled = await (await _client.PostAsync($"/api/todos/{id}/toggle", null))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(toggled.GetProperty("isDone").GetBoolean());

        var del = await _client.DeleteAsync($"/api/todos/{id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        Assert.Equal(0, (await GetArray("/api/todos")).GetArrayLength());
    }

    [Fact]
    public async Task Item_can_be_linked_to_a_project_and_filtered()
    {
        var project = await (await _client.PostAsJsonAsync("/api/projects", new { name = "Proj A" }))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        var pid = Id(project);
        var todo = await (await _client.PostAsJsonAsync("/api/todos", new { title = "Task A" }))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        var tid = Id(todo);

        var link = await _client.PutAsJsonAsync($"/api/todos/{tid}/projects",
            new { projectIds = new[] { pid } });
        Assert.Equal(HttpStatusCode.NoContent, link.StatusCode);

        var filtered = await GetArray($"/api/todos?projectId={pid}");
        Assert.Equal(1, filtered.GetArrayLength());
        Assert.Equal("Proj A", filtered[0].GetProperty("projects")[0].GetProperty("name").GetString());

        var projects = await GetArray("/api/projects?status=open");
        Assert.Equal(1, projects[0].GetProperty("todosTotal").GetInt32());
    }

    [Fact]
    public async Task Search_finds_matches_across_types_case_insensitively()
    {
        await _client.PostAsJsonAsync("/api/todos", new { title = "Alpha task" });
        await _client.PostAsJsonAsync("/api/notes", new { body = "an alpha note" });
        await _client.PostAsJsonAsync("/api/bookmarks", new { url = "https://alpha.example.com" });

        var results = await _client.GetFromJsonAsync<JsonElement>("/api/search?q=ALPHA", Json);
        Assert.True(results.GetProperty("todos").GetArrayLength() >= 1);
        Assert.True(results.GetProperty("notes").GetArrayLength() >= 1);
        Assert.True(results.GetProperty("bookmarks").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Backup_export_then_import_restores_data_and_links()
    {
        var project = await (await _client.PostAsJsonAsync("/api/projects", new { name = "Backup proj" }))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        var pid = Id(project);
        var todo = await (await _client.PostAsJsonAsync("/api/todos", new { title = "Backup todo" }))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        await _client.PutAsJsonAsync($"/api/todos/{Id(todo)}/projects", new { projectIds = new[] { pid } });

        var backup = await _client.GetStringAsync("/api/data/export");

        await _client.PostAsync("/api/data/clear", null);
        Assert.Equal(0, (await GetArray("/api/todos")).GetArrayLength());

        var import = await _client.PostAsync("/api/data/import",
            new StringContent(backup, Encoding.UTF8, "application/json"));
        Assert.True(import.IsSuccessStatusCode);

        var restored = await GetArray($"/api/todos?projectId={pid}");
        Assert.Equal(1, restored.GetArrayLength());
        Assert.Equal("Backup todo", restored[0].GetProperty("title").GetString());
        Assert.Equal("Backup proj", restored[0].GetProperty("projects")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Environment_crud_and_config_checklist()
    {
        var env = await (await _client.PostAsJsonAsync("/api/environments",
            new { name = "Contoso Dev", type = "Dev", ppEnvironmentId = "11111111-1111-1111-1111-111111111111", url = "https://contoso.crm11.dynamics.com" }))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        var eid = Id(env);
        Assert.Equal("Contoso Dev", env.GetProperty("name").GetString());
        Assert.Equal("Dev", env.GetProperty("type").GetString());

        // Add a connection-reference config row, then toggle it "set".
        var cfg = await (await _client.PostAsJsonAsync($"/api/environments/{eid}/configs",
            new { kind = "ConnectionReference", name = "cr_sharepoint", value = "SharePoint - svc account" }))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        var cid = Id(cfg);
        Assert.False(cfg.GetProperty("isSet").GetBoolean());

        var toggled = await (await _client.PostAsync($"/api/environments/{eid}/configs/{cid}/toggle", null))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(toggled.GetProperty("isSet").GetBoolean());

        var list = await GetArray("/api/environments");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal(1, list[0].GetProperty("configs").GetArrayLength());
        Assert.Equal("cr_sharepoint", list[0].GetProperty("configs")[0].GetProperty("name").GetString());

        // Deleting the environment cascades to its config rows.
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/environments/{eid}")).StatusCode);
        Assert.Equal(0, (await GetArray("/api/environments")).GetArrayLength());
    }

    [Fact]
    public async Task Secret_can_be_linked_to_environments_both_ways()
    {
        var env = await (await _client.PostAsJsonAsync("/api/environments", new { name = "Prod", type = "Prod" }))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        var eid = Id(env);

        // Secrets need a master password before they can be created.
        await _client.PostAsJsonAsync("/api/vault/set", new { password = "pw12345" });
        var secret = await (await _client.PostAsJsonAsync("/api/secrets",
            new { name = "Prod app reg", expiresOn = "2027-01-01" })).Content.ReadFromJsonAsync<JsonElement>(Json);
        var sid = Id(secret);

        var link = await _client.PutAsJsonAsync($"/api/secrets/{sid}/environments", new { environmentIds = new[] { eid } });
        Assert.Equal(HttpStatusCode.NoContent, link.StatusCode);

        // Visible from the secret side…
        var secrets = await GetArray("/api/secrets");
        Assert.Equal("Prod", secrets[0].GetProperty("environments")[0].GetProperty("name").GetString());
        // …and the environment side.
        var envs = await GetArray("/api/environments");
        Assert.Equal("Prod app reg", envs[0].GetProperty("secrets")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Archiving_a_project_archives_its_notes()
    {
        var project = await (await _client.PostAsJsonAsync("/api/projects", new { name = "Old proj" }))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        var pid = Id(project);
        var note = await (await _client.PostAsJsonAsync("/api/notes", new { body = "keep for reference" }))
            .Content.ReadFromJsonAsync<JsonElement>(Json);
        var nid = Id(note);
        await _client.PutAsJsonAsync($"/api/notes/{nid}/projects", new { projectIds = new[] { pid } });

        // Archive the project — the linked note follows it into the archive.
        await _client.PutAsJsonAsync($"/api/projects/{pid}", new { name = "Old proj", status = "Archived" });

        Assert.Equal(0, (await GetArray("/api/notes")).GetArrayLength());               // default = open only
        var archived = await GetArray("/api/notes?archived=true");
        Assert.Equal(1, archived.GetArrayLength());
        Assert.True(archived[0].GetProperty("isArchived").GetBoolean());

        // Unarchive brings it back to the default view.
        await _client.PostAsync($"/api/notes/{nid}/archive", null);
        Assert.Equal(1, (await GetArray("/api/notes")).GetArrayLength());
    }

    [Fact]
    public async Task Clear_all_empties_every_section()
    {
        await _client.PostAsJsonAsync("/api/todos", new { title = "x" });
        await _client.PostAsJsonAsync("/api/notes", new { body = "y" });
        await _client.PostAsJsonAsync("/api/projects", new { name = "z" });

        await _client.PostAsync("/api/data/clear", null);

        Assert.Equal(0, (await GetArray("/api/todos")).GetArrayLength());
        Assert.Equal(0, (await GetArray("/api/notes")).GetArrayLength());
        Assert.Equal(0, (await GetArray("/api/projects?status=all")).GetArrayLength());
    }
}
