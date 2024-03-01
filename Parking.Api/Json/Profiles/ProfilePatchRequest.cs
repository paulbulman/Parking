namespace Parking.Api.Json.Profiles;

public class ProfilePatchRequest
{
    public ProfilePatchRequest(
        string? alternativeRegistrationNumber,
        string? registrationNumber,
        bool? requestReminderEnabled = null,
        bool? reservationReminderEnabled = null)
    {
        this.AlternativeRegistrationNumber = alternativeRegistrationNumber;
        this.RegistrationNumber = registrationNumber;
        this.RequestReminderEnabled = requestReminderEnabled;
        this.ReservationReminderEnabled = reservationReminderEnabled;
    }

    public string? AlternativeRegistrationNumber { get; }

    public string? RegistrationNumber { get; }

    public bool? RequestReminderEnabled { get; }

    public bool? ReservationReminderEnabled { get; }
}