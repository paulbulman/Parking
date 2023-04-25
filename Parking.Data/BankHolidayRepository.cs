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
                new BankHoliday(new LocalDate(2023, 1, 2)),
                new BankHoliday(new LocalDate(2023, 4, 7)),
                new BankHoliday(new LocalDate(2023, 4, 10)),
                new BankHoliday(new LocalDate(2023, 5, 1)),
                new BankHoliday(new LocalDate(2023, 5, 8)),
                new BankHoliday(new LocalDate(2023, 5, 29)),
                new BankHoliday(new LocalDate(2023, 8, 28)),
                new BankHoliday(new LocalDate(2023, 12, 25)),
                new BankHoliday(new LocalDate(2023, 12, 26)),

                new BankHoliday(new LocalDate(2024, 1, 1)),
                new BankHoliday(new LocalDate(2024, 3, 29)),
                new BankHoliday(new LocalDate(2024, 4, 1)),
                new BankHoliday(new LocalDate(2024, 5, 6)),
                new BankHoliday(new LocalDate(2024, 5, 27)),
                new BankHoliday(new LocalDate(2024, 8, 26)),
                new BankHoliday(new LocalDate(2024, 12, 25)),
                new BankHoliday(new LocalDate(2024, 12, 26)),

                new BankHoliday(new LocalDate(2025, 1, 1)),
                new BankHoliday(new LocalDate(2025, 4, 18)),
                new BankHoliday(new LocalDate(2025, 4, 21)),
                new BankHoliday(new LocalDate(2025, 5, 5)),
                new BankHoliday(new LocalDate(2025, 5, 26)),
                new BankHoliday(new LocalDate(2025, 8, 25)),
                new BankHoliday(new LocalDate(2025, 12, 25)),
                new BankHoliday(new LocalDate(2025, 12, 26)),
            };
    }
}