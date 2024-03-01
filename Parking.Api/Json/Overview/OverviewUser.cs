namespace Parking.Api.Json.Overview;

using System.ComponentModel.DataAnnotations;

public class OverviewUser(string name, bool isHighlighted)
{
    [Required]
    public string Name { get; } = name;

    [Required]
    public bool IsHighlighted { get; } = isHighlighted;
}