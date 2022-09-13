namespace Parking.Data
{
    using System.Collections.Generic;
    using Business.Data;
    using Model;
    using NodaTime;

    public class BankHolidayRepository : IBankHolidayRepository
    {
        public IReadOnlyCollection<BankHoliday> GetBankHolidays() =>
            new[]
            {
                new BankHoliday(new LocalDate(2022, 1, 3)),
                new BankHoliday(new LocalDate(2022, 4, 15)),
                new BankHoliday(new LocalDate(2022, 4, 18)),
                new BankHoliday(new LocalDate(2022, 5, 2)),
                new BankHoliday(new LocalDate(2022, 6, 2)),
                new BankHoliday(new LocalDate(2022, 6, 3)),
                new BankHoliday(new LocalDate(2022, 8, 29)),
                new BankHoliday(new LocalDate(2022, 9, 19)),
                new BankHoliday(new LocalDate(2022, 12, 26)),
                new BankHoliday(new LocalDate(2022, 12, 27)),
                new BankHoliday(new LocalDate(2023, 1, 2)),
                new BankHoliday(new LocalDate(2023, 4, 7)),
                new BankHoliday(new LocalDate(2023, 4, 10)),
                new BankHoliday(new LocalDate(2023, 5, 1)),
                new BankHoliday(new LocalDate(2023, 5, 29)),
                new BankHoliday(new LocalDate(2023, 8, 28)),
                new BankHoliday(new LocalDate(2023, 12, 25)),
                new BankHoliday(new LocalDate(2023, 12, 26)),
            };
    }
}