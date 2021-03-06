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
                new BankHoliday(new LocalDate(2021, 4, 2)),
                new BankHoliday(new LocalDate(2021, 4, 5)),
                new BankHoliday(new LocalDate(2021, 5, 3)),
                new BankHoliday(new LocalDate(2021, 5, 31)),
                new BankHoliday(new LocalDate(2021, 8, 30)),
                new BankHoliday(new LocalDate(2021, 12, 27)),
                new BankHoliday(new LocalDate(2021, 12, 28)),
                new BankHoliday(new LocalDate(2022, 1, 3)),
                new BankHoliday(new LocalDate(2022, 4, 15)),
                new BankHoliday(new LocalDate(2022, 4, 18)),
                new BankHoliday(new LocalDate(2022, 5, 2)),
                new BankHoliday(new LocalDate(2022, 6, 2)),
                new BankHoliday(new LocalDate(2022, 6, 3)),
                new BankHoliday(new LocalDate(2022, 8, 29)),
                new BankHoliday(new LocalDate(2022, 12, 26)),
                new BankHoliday(new LocalDate(2022, 12, 27))
            };
    }
}