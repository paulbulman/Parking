namespace Parking.Api.Json.Profiles;

public class ProfileData
{
    public ProfileData(
        string? registrationNumber,
        string? alternativeRegistrationNumber,
        bool requestReminderEnabled,
        bool reservationReminderEnabled)
    {
        this.RegistrationNumber = registrationNumber;
        this.AlternativeRegistrationNumber = alternativeRegistrationNumber;
        this.RequestReminderEnabled = requestReminderEnabled;
        this.ReservationReminderEnabled = reservationReminderEnabled;
    }

    public string? RegistrationNumber { get; }

    public string? AlternativeRegistrationNumber { get; }

    public bool RequestReminderEnabled { get; }

    public bool ReservationReminderEnabled { get; }
}