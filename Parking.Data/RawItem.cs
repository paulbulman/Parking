namespace Parking.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Amazon.DynamoDBv2.DataModel;
    using Amazon.DynamoDBv2.DocumentModel;

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

        public static RawItem CreateUser(
            string primaryKey,
            string sortKey,
            string? alternativeRegistrationNumber,
            decimal? commuteDistance,
            string emailAddress,
            string firstName,
            string lastName,
            string? registrationNumber,
            bool? requestReminderEnabled,
            bool? reservationReminderEnabled) => new RawItem(primaryKey, sortKey)
            {
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
}