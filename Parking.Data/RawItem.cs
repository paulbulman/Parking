namespace Parking.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Amazon.DynamoDBv2.DataModel;
    using Amazon.DynamoDBv2.DocumentModel;
    using NodaTime;
    using NodaTime.Text;

    [DynamoDBTable("override-this-per-environment")]
    public class RawItem
    {
        [Obsolete("Prefer static Create methods, to enforce nullability checks.", error: true)]
        // ReSharper disable once UnusedMember.Global - required by DynamoDBContext
#pragma warning disable CS8618
        public RawItem()
#pragma warning restore CS8618
        {
        }

        private RawItem(string primaryKey, string sortKey)
        {
            this.PrimaryKey = primaryKey;
            this.SortKey = sortKey;
        }

        public static RawItem CreateConfiguration(Dictionary<string, string> configuration) =>
            new RawItem("GLOBAL", "CONFIGURATION")
            {
                Configuration = configuration
            };

        public static RawItem CreateRequests(
            string primaryKey,
            string sortKey,
            Dictionary<string, string> requests) => new RawItem(primaryKey, sortKey)
            {
                Requests = requests
            };

        public static RawItem CreateReservations(
            string primaryKey,
            string sortKey,
            Dictionary<string, List<string>> reservations) => new RawItem(primaryKey, sortKey)
            {
                Reservations = reservations
            };

        public static RawItem CreateSchedules(Dictionary<string, string> schedules) =>
            new RawItem("GLOBAL", "SCHEDULES")
            {
                Schedules = schedules
            };

        public static RawItem CreateTrigger(string key) =>
            new RawItem("TRIGGER", key)
            {
                // We need some non-key data to be able to save the record,
                // so just arbitrarily duplicate the sort key to a field.
                Trigger = key
            };

        public static RawItem CreateGuests(
            string primaryKey,
            string sortKey,
            Dictionary<string, List<GuestData>> guests) => new RawItem(primaryKey, sortKey)
            {
                Guests = guests
            };

        public static RawItem CreateUser(
            string primaryKey,
            string sortKey,
            Instant? deletedTimestamp,
            string? alternativeRegistrationNumber,
            decimal? commuteDistance,
            string emailAddress,
            string firstName,
            string lastName,
            string? registrationNumber,
            bool? requestReminderEnabled,
            bool? reservationReminderEnabled) => new RawItem(primaryKey, sortKey)
            {
                DeletedTimestamp = deletedTimestamp,
                AlternativeRegistrationNumber = alternativeRegistrationNumber,
                CommuteDistance = commuteDistance,
                EmailAddress = emailAddress,
                FirstName = firstName,
                LastName = lastName,
                RegistrationNumber = registrationNumber,
                RequestReminderEnabled = requestReminderEnabled,
                ReservationReminderEnabled = reservationReminderEnabled
            };

        [DynamoDBHashKey]
        [DynamoDBGlobalSecondaryIndexRangeKey("SK-PK-index")]
        [DynamoDBProperty("PK")]
        public string PrimaryKey { get; set; }

        [DynamoDBRangeKey]
        [DynamoDBGlobalSecondaryIndexHashKey("SK-PK-index")]
        [DynamoDBProperty("SK")]
        public string SortKey { get; set; }

        [DynamoDBProperty("deletedTimestamp", typeof(TimestampConverter))]
        public Instant? DeletedTimestamp { get; set; }

        [DynamoDBProperty("alternativeRegistrationNumber")]
        public string? AlternativeRegistrationNumber { get; set; }

        [DynamoDBProperty("commuteDistance")]
        public decimal? CommuteDistance { get; set; }

        [DynamoDBProperty("configuration")]
        public Dictionary<string, string>? Configuration { get; set; }

        [DynamoDBProperty("emailAddress")]
        public string? EmailAddress { get; set; }

        [DynamoDBProperty("firstName")]
        public string? FirstName { get; set; }

        [DynamoDBProperty("lastName")]
        public string? LastName { get; set; }

        [DynamoDBProperty("registrationNumber")]
        public string? RegistrationNumber { get; set; }

        [DynamoDBProperty("requests")]
        public Dictionary<string, string>? Requests { get; set; }

        [DynamoDBProperty("requestReminderEnabled")]
        public bool? RequestReminderEnabled { get; set; }

        [DynamoDBProperty("reservations", typeof(ReservationsConverter))]
        public Dictionary<string, List<string>>? Reservations { get; set; }

        [DynamoDBProperty("reservationReminderEnabled")]
        public bool? ReservationReminderEnabled { get; set; }

        [DynamoDBProperty("schedules")]
        public Dictionary<string, string>? Schedules { get; set; }

        [DynamoDBProperty("trigger")]
        public string? Trigger { get; set; }

        [DynamoDBProperty("guests", typeof(GuestsConverter))]
        public Dictionary<string, List<GuestData>>? Guests { get; set; }
    }

    public class ReservationsConverter : IPropertyConverter
    {
        public object FromEntry(DynamoDBEntry entry) =>
            entry.AsDocument().ToDictionary(
                dailyData => dailyData.Key,
                dailyData => dailyData.Value
                    .AsDynamoDBList()
                    .Entries
                    .Where(e => e is Primitive)
                    .Select(e => e.AsString())
                    .ToList());

        public DynamoDBEntry ToEntry(object value)
        {
            if (!(value is Dictionary<string, List<string>> dailyData))
            {
                throw new ArgumentException("Could not convert raw value to dictionary", nameof(value));
            }

            return new Document(
                dailyData.ToDictionary(
                    day => day.Key,
                    day => (DynamoDBEntry)new DynamoDBList(day.Value.Select(userId => new Primitive(userId)))));
        }
    }

    public class GuestData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string VisitingUserId { get; set; } = string.Empty;
        public string? RegistrationNumber { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class GuestsConverter : IPropertyConverter
    {
        public object FromEntry(DynamoDBEntry entry) =>
            entry.AsDocument().ToDictionary(
                dailyData => dailyData.Key,
                dailyData => dailyData.Value
                    .AsDynamoDBList()
                    .Entries
                    .Where(e => e is Document)
                    .Select(e =>
                    {
                        var doc = e.AsDocument();
                        return new GuestData
                        {
                            Id = doc.ContainsKey("id") ? doc["id"].AsString() : string.Empty,
                            Name = doc.ContainsKey("name") ? doc["name"].AsString() : string.Empty,
                            VisitingUserId = doc.ContainsKey("visitingUserId") ? doc["visitingUserId"].AsString() : string.Empty,
                            RegistrationNumber = doc.ContainsKey("registrationNumber") ? doc["registrationNumber"].AsString() : null,
                            Status = doc.ContainsKey("status") ? doc["status"].AsString() : string.Empty,
                        };
                    })
                    .ToList());

        public DynamoDBEntry ToEntry(object value)
        {
            if (value is not Dictionary<string, List<GuestData>> dailyData)
            {
                throw new ArgumentException("Could not convert raw value to dictionary", nameof(value));
            }

            return new Document(
                dailyData.ToDictionary(
                    day => day.Key,
                    day => (DynamoDBEntry)new DynamoDBList(day.Value.Select(guest =>
                    {
                        var doc = new Document();
                        doc["id"] = new Primitive(guest.Id);
                        doc["name"] = new Primitive(guest.Name);
                        doc["visitingUserId"] = new Primitive(guest.VisitingUserId);
                        if (guest.RegistrationNumber != null)
                        {
                            doc["registrationNumber"] = new Primitive(guest.RegistrationNumber);
                        }
                        doc["status"] = new Primitive(guest.Status);
                        return (DynamoDBEntry)doc;
                    }))));
        }
    }

    public class TimestampConverter : IPropertyConverter
    {
        public object FromEntry(DynamoDBEntry entry) => InstantPattern.ExtendedIso.Parse(entry.AsString()).Value;

        public DynamoDBEntry ToEntry(object value)
        {
            if (!(value is Instant instant))
            {
                throw new ArgumentException("Could not convert raw value to instant", nameof(value));
            }

            return InstantPattern.ExtendedIso.Format(instant);
        }
    }
}