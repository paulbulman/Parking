namespace ParkingService.Data
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

        [DynamoDBProperty("requests")]
        public Dictionary<string, string> Requests { get; set; }

        [DynamoDBProperty("reservations", typeof(ReservationsConverter))]
        public Dictionary<string, List<string>> Reservations { get; set; }

        [DynamoDBProperty("commuteDistance")]
        public decimal? CommuteDistance { get; set; }
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

        public DynamoDBEntry ToEntry(object value) => throw new NotImplementedException("Updating reservations is currently supported only via the web API.");
    }
}