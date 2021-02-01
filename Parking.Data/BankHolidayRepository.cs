﻿namespace Parking.Data
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
                new BankHoliday(new LocalDate(2020, 12, 25)),
                new BankHoliday(new LocalDate(2020, 12, 28)),
                new BankHoliday(new LocalDate(2021, 1, 1))
            };
    }
}