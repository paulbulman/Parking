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
        [DynamoDBHashKey]
        [DynamoDBGlobalSecondaryIndexRangeKey("SK-PK-index")]
        [DynamoDBProperty("PK")]
        public string PrimaryKey { get; set; }

        [DynamoDBRangeKey]
        [DynamoDBGlobalSecondaryIndexHashKey("SK-PK-index")]
        [DynamoDBProperty("SK")]
        public string SortKey { get; set; }

        [DynamoDBProperty("alternativeRegistrationNumber")]
        public string AlternativeRegistrationNumber { get; set; }

        [DynamoDBProperty("commuteDistance")]
        public decimal? CommuteDistance { get; set; }

        [DynamoDBProperty("emailAddress")]
        public string EmailAddress { get; set; }

        [DynamoDBProperty("firstName")]
        public string FirstName { get; set; }

        [DynamoDBProperty("lastName")]
        public string LastName { get; set; }

        [DynamoDBProperty("registrationNumber")]
        public string RegistrationNumber { get; set; }

        [DynamoDBProperty("requests")]
        public Dictionary<string, string> Requests { get; set; }

        [DynamoDBProperty("reservations", typeof(ReservationsConverter))]
        public Dictionary<string, List<string>> Reservations { get; set; }
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