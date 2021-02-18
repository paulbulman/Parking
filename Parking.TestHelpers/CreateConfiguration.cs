namespace Parking.TestHelpers
{
    using Model;

    public static class CreateConfiguration
    {
        public static Configuration With(int totalSpaces = 20, int shortLeadTimeSpaces = 4, int nearbyDistance = 5) =>
            new Configuration(
                nearbyDistance: nearbyDistance,
                shortLeadTimeSpaces: shortLeadTimeSpaces,
                totalSpaces: totalSpaces);
    }
}