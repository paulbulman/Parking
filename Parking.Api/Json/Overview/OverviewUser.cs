namespace Parking.Api.Json.Overview
{
    using System.ComponentModel.DataAnnotations;

    public class OverviewUser
    {
        public OverviewUser(string name, bool isHighlighted)
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