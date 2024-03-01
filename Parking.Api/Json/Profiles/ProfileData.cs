namespace Parking.Api.Json.Profiles;

public class ProfileData(
    string? registrationNumber,
    string? alternativeRegistrationNumber,
    bool requestReminderEnabled,
    bool reservationReminderEnabled)
{
    public string? RegistrationNumber { get; } = registrationNumber;

    public string? AlternativeRegistrationNumber { get; } = alternativeRegistrationNumber;

    public bool RequestReminderEnabled { get; } = requestReminderEnabled;

    public bool ReservationReminderEnabled { get; } = reservationReminderEnabled;
}