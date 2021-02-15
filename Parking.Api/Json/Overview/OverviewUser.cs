namespace Parking.Api.Json.Overview
{
    public class OverviewUser
    {
        public OverviewUser(string name, bool isHighlighted)
        {
            this.Name = name;
            this.IsHighlighted = isHighlighted;
        }

        public string Name { get; }
        
        public bool IsHighlighted { get; }
    }
}