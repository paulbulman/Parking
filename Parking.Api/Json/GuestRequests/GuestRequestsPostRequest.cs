namespace Parking.Api.Json.GuestRequests;

using NodaTime;

public class GuestRequestsPostRequest(
    LocalDate date,
    string name,
    string visitingUserId,
    string? registrationNumber)
{
    public LocalDate Date { get; } = date;
    public string Name { get; } = name;
    public string VisitingUserId { get; } = visitingUserId;
    public string? RegistrationNumber { get; } = registrationNumber;
}
