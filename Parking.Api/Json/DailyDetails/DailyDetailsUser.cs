namespace Parking.Api.Json.DailyDetails;

using System.ComponentModel.DataAnnotations;

public class DailyDetailsUser(string name, bool isHighlighted)
{
    [Required]
    public string Name { get; } = name;

    [Required]
    public bool IsHighlighted { get; } = isHighlighted;
}