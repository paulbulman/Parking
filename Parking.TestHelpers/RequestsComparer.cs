namespace Parking.TestHelpers
{
    using System.Collections.Generic;
    using Model;

    public class RequestsComparer : IEqualityComparer<Request>
    {
        public bool Equals(Request? first, Request? second) =>
            first != null &&
            second != null &&
            first.UserId == second.UserId &&
            first.Date == second.Date &&
            first.Status == second.Status;

        public int GetHashCode(Request request) => request.Date.GetHashCode();
    }
}