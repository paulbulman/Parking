namespace Parking.Api.Json.DailyDetails
{
    using System.ComponentModel.DataAnnotations;

    public class DailyDetailsUser
    {
        public DailyDetailsUser(string name, bool isHighlighted)
        {
            this.Name = name;
            this.IsHighlighted = isHighlighted;
        }
        
        [Required]
        public string Name { get; }

        [Required]
        public bool IsHighlighted { get; }
    }
}