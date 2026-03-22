namespace Parking.Api.Json.GuestRequests;

public class GuestRequestsPutRequest(
    string name,
    string visitingUserId,
    string? registrationNumber)
{
    public string Name { get; } = name;
    public string VisitingUserId { get; } = visitingUserId;
    public string? RegistrationNumber { get; } = registrationNumber;
}
