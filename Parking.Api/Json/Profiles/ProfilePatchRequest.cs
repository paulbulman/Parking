namespace Parking.Api.Json.Profiles;

public class ProfilePatchRequest(
    string? alternativeRegistrationNumber,
    string? registrationNumber,
    bool? requestReminderEnabled = null,
    bool? reservationReminderEnabled = null)
{
    public string? AlternativeRegistrationNumber { get; } = alternativeRegistrationNumber;

    public string? RegistrationNumber { get; } = registrationNumber;

    public bool? RequestReminderEnabled { get; } = requestReminderEnabled;

    public bool? ReservationReminderEnabled { get; } = reservationReminderEnabled;
}