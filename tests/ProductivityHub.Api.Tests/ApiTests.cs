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
