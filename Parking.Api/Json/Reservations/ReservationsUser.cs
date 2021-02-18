namespace Parking.Api.Json.Reservations
{
    public class ReservationsUser
    {
        public ReservationsUser(string userId, string name)
        {
            this.UserId = userId;
            this.Name = name;
        }

        public string UserId { get; }

        public string Name { get; }
    }
}