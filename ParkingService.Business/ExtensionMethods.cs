namespace ParkingService.Business
{
    using NodaTime;
    using NodaTime.Text;

    public static class ExtensionMethods
    {
        public static string ToEmailDisplayString(this LocalDate localDate) =>
            LocalDatePattern.CreateWithCurrentCulture("ddd dd MMM").Format(localDate);
    }
}