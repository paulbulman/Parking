using System.Collections.Generic;
using ParkingService.Model;

namespace ParkingService.Business.Data
{
    public interface IBankHolidayRepository
    {
        IReadOnlyCollection<BankHoliday> GetBankHolidays();
    }
}