namespace Parking.Business.Data
{
    using System.Collections.Generic;
    using Model;

    public interface IBankHolidayRepository
    {
        IReadOnlyCollection<BankHoliday> GetBankHolidays();
    }
}