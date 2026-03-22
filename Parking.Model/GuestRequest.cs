namespace Parking.Model;

using NodaTime;

public class GuestRequest(
    string id,
    LocalDate date,
    string name,
    string visitingUserId,
    string? registrationNumber,
    GuestRequestStatus status)
{
    public string Id { get; } = id;

    public LocalDate Date { get; } = date;

    public string Name { get; } = name;

    public string VisitingUserId { get; } = visitingUserId;

    public string? RegistrationNumber { get; } = registrationNumber;

    public GuestRequestStatus Status { get; } = status;
}
