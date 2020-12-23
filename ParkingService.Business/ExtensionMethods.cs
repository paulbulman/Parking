namespace ParkingService.Business
{
    using System.Linq;
    using Model;
    using NodaTime;
    using NodaTime.Text;

    public static class ExtensionMethods
    {
        public static string ToEmailDisplayString(this LocalDate localDate) =>
            LocalDatePattern.CreateWithCurrentCulture("ddd dd MMM").Format(localDate);

        public static string ToEmailDisplayString(this DateInterval dateInterval) =>
            $"{dateInterval.Start.ToEmailDisplayString()} - {dateInterval.End.ToEmailDisplayString()}";

        public static bool IsActive(this RequestStatus requestStatus) =>
            new[] {RequestStatus.Allocated, RequestStatus.Requested}.Contains(requestStatus);
    }
}