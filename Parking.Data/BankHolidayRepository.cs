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
                new BankHoliday(new LocalDate(2025, 12, 25)),
                new BankHoliday(new LocalDate(2025, 12, 26)),
                
                new BankHoliday(new LocalDate(2026, 1, 1)),
                new BankHoliday(new LocalDate(2026, 4, 3)),
                new BankHoliday(new LocalDate(2026, 4, 6)),
                new BankHoliday(new LocalDate(2026, 5, 4)),
                new BankHoliday(new LocalDate(2026, 5, 25)),
                new BankHoliday(new LocalDate(2026, 8, 31)),
                new BankHoliday(new LocalDate(2026, 12, 25)),
                new BankHoliday(new LocalDate(2026, 12, 28)),
            };
    }
}