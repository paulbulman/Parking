namespace ParkingService.Model
{
    using NodaTime;

    public class BankHoliday
    {
        public BankHoliday(LocalDate date) => Date = date;

        public LocalDate Date { get; }
    }
}