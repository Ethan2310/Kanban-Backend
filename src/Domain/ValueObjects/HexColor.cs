using System.Text.RegularExpressions;

namespace Domain.ValueObjects;

public sealed class HexColor
{
    private static readonly Regex HexPattern = new(@"^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    public string Value { get; }

    public HexColor(string value)
    {
        if (!HexPattern.IsMatch(value))
            throw new ArgumentException($"'{value}' is not a valid hex color. Expected format: #RRGGBB.", nameof(value));

        Value = value;
    }

    public static HexColor? From(string? value) => value is null ? null : new HexColor(value);

    public override string ToString() => Value;

    public override bool Equals(object? obj) => obj is HexColor other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();
}
