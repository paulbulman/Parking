namespace Parking.Model
{
    public class Configuration
    {
        public Configuration(decimal nearbyDistance, int shortLeadTimeSpaces, int totalSpaces)
        {
            NearbyDistance = nearbyDistance;
            ShortLeadTimeSpaces = shortLeadTimeSpaces;
            TotalSpaces = totalSpaces;
        }

        public decimal NearbyDistance { get; }
        
        public int ShortLeadTimeSpaces { get; }
        
        public int TotalSpaces { get; }
    }
}