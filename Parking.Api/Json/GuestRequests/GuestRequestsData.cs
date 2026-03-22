namespace Parking.Api.Json.GuestRequests;

using Model;

public class GuestRequestsData(
    string id,
    string date,
    string name,
    string visitingUserId,
    string visitingUserDisplayName,
    string? registrationNumber,
    GuestRequestStatus status)
{
    public string Id { get; } = id;
    public string Date { get; } = date;
    public string Name { get; } = name;
    public string VisitingUserId { get; } = visitingUserId;
    public string VisitingUserDisplayName { get; } = visitingUserDisplayName;
    public string? RegistrationNumber { get; } = registrationNumber;
    public GuestRequestStatus Status { get; } = status;
}
