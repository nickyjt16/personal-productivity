using System.Globalization;
using System.Windows.Media;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Desktop;

// Small helpers for fixed (non-theme) colours used on badges.
public static class Palette
{
    public static Brush FromHex(string hex)
    {
        var c = (Color)ColorConverter.ConvertFromString(hex);
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }

    public static readonly Brush White = FromHex("#FFFFFF");

    public static Brush Priority(Priority p) => p switch
    {
        Core.Data.Entities.Priority.High => FromHex("#DC3545"),
        Core.Data.Entities.Priority.Medium => FromHex("#0DCAF0"),
        _ => FromHex("#6C757D"),
    };

    public static Brush Status(ProjectStatus s) => s switch
    {
        ProjectStatus.Active => FromHex("#0D6EFD"),
        ProjectStatus.Complete => FromHex("#198754"),
        ProjectStatus.Archived => FromHex("#343A40"),
        _ => FromHex("#6C757D"),
    };
}

// Display wrapper for a todo row.
public class TodoRow
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Notes { get; init; }
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
    public bool IsDone { get; set; }
    public required Priority Priority { get; init; }
    public string PriorityText => Priority.ToString();
    public Brush PriorityBrush => Palette.Priority(Priority);
    public DateTimeOffset? DueDate { get; init; }
    public bool HasDue => DueBadge is not null;
    public string DueText => DueBadge?.label ?? "";
    public Brush DueBrush => Palette.FromHex(DueBadge?.color ?? "#6C757D");
    public bool HasProjects => !string.IsNullOrEmpty(ProjectTags);
    public string ProjectTags { get; init; } = "";
    public RecurUnit RecurUnit { get; init; }
    public int RecurInterval { get; init; }
    public bool HasRecur => RecurUnit != RecurUnit.None && RecurInterval > 0;
    public string RecurText => !HasRecur ? "" :
        RecurInterval == 1 ? $"🔁 every {RecurUnit.ToString().ToLowerInvariant()}"
        : $"🔁 every {RecurInterval} {RecurUnit.ToString().ToLowerInvariant()}s";

    private (string label, string color)? DueBadge => DueInfo(DueDate, IsDone);

    public static (string label, string color)? DueInfo(DateTimeOffset? due, bool done)
    {
        if (due is null || done) return null;
        var d = due.Value.Date;
        var today = DateTime.Now.Date;
        if (d < today) return ("Overdue", "#DC3545");
        if (d == today) return ("Due today", "#FFC107");
        return (due.Value.ToString("d", CultureInfo.CurrentCulture), "#6C757D");
    }
}

public class BookmarkRow
{
    public required Guid Id { get; init; }
    public required string Url { get; init; }
    public string? Title { get; init; }
    public string Display => string.IsNullOrWhiteSpace(Title) ? Url : Title!;
    public bool IsRead { get; init; }
    public string ProjectTags { get; init; } = "";
    public bool HasProjects => ProjectTags.Length > 0;
}

public class NoteListRow
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public string Preview { get; init; } = "";
}

public class JournalRow
{
    public required DateOnly Date { get; init; }
    public required string Title { get; init; }
    public string Preview { get; init; } = "";
}

public class ProjectRow
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public required ProjectStatus Status { get; init; }
    public string StatusText => Status.ToString();
    public Brush StatusBrush => Palette.Status(Status);
    public Brush ColorBrush { get; init; } = Palette.White;
    public required string CountsText { get; init; }
    public double ProgressPct { get; init; }
    public bool HasProgress { get; init; }
}

public class LinkRow
{
    public required Guid Id { get; init; }
    public required string Label { get; init; }
    // Shown by ComboBox (we don't use DisplayMemberPath because our custom
    // ComboBox template renders SelectionBoxItem directly).
    public override string ToString() => Label;
}

// ComboBox item with a nullable id (e.g. an "All" option).
public class ComboItem
{
    public Guid? Id { get; init; }
    public required string Label { get; init; }
    public override string ToString() => Label;
}

public class SecretRow
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? ClientId { get; init; }
    public string? Value { get; init; }
    public DateOnly ExpiresOn { get; init; }
    public string? Notes { get; init; }
    public string? NotifyRaw { get; init; }
    public bool HasNotify => !string.IsNullOrWhiteSpace(NotifyRaw);
    public string NotifyText => "Notify: " + string.Join(", ",
        (NotifyRaw ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    public string? Link { get; init; }
    public bool HasLink => !string.IsNullOrWhiteSpace(Link);
    public string ProjectTags { get; init; } = "";
    public bool HasProjects => ProjectTags.Length > 0;
    // A value is stored but the vault is locked, so it can't be shown.
    public bool Locked { get; init; }
    public string LockedNote => "🔒 Value hidden — unlock to view";

    public int DaysLeft => ExpiresOn.DayNumber - DateOnly.FromDateTime(DateTime.Now.Date).DayNumber;
    public string ExpiresText => ExpiresOn.ToString("d", CultureInfo.CurrentCulture);
    public bool HasClientId => !string.IsNullOrWhiteSpace(ClientId);
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
    public string ValueMasked => "••••••••";

    public string BadgeText => DaysLeft < 0 ? $"Expired {-DaysLeft}d ago"
        : DaysLeft == 0 ? "Expires today"
        : DaysLeft <= 7 ? $"Expires in {DaysLeft}d"
        : $"{DaysLeft}d left";

    public Brush BadgeBrush => DaysLeft <= 0 ? Palette.FromHex("#DC3545")
        : DaysLeft <= 7 ? Palette.FromHex("#FFC107")
        : Palette.FromHex("#6C757D");
}
