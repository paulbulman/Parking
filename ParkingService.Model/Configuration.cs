namespace ParkingService.Model
{
    public class Configuration
    {
        public Configuration(decimal nearbyDistance, int reservableSpaces, int totalSpaces)
        {
            NearbyDistance = nearbyDistance;
            ReservableSpaces = reservableSpaces;
            TotalSpaces = totalSpaces;
        }

        public decimal NearbyDistance { get; }
        
        public int ReservableSpaces { get; }
        
        public int TotalSpaces { get; }
    }
}