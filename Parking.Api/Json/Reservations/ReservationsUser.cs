namespace Parking.Api.Json.Reservations
{
    using System.ComponentModel.DataAnnotations;

    public class ReservationsUser
    {
        public ReservationsUser(string userId, string name)
        {
            this.UserId = userId;
            this.Name = name;
        }

        [Required]
        public string UserId { get; }

        [Required]
        public string Name { get; }
    }
}