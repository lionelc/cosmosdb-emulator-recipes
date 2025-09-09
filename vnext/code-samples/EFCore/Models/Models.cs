using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace EFCore.CosmosDB.Test.Models
{
    public enum ValueTypeEnum
    {
        Difference,
        Total
    }

    public static class SetUpFixture
    {
        public static DateTime FirstDayOfCurrentMonth => new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
    }

    [Owned]
    public class MyDataItem
    {
        public string SystemId { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int CategoryId { get; set; }
        public string Subcategory { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ValueType { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }

    public class MessageDocument
    {
        public string Id { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public List<MyDataItem> Data { get; set; } = new List<MyDataItem>();
        public string PartitionKey { get; set; } = string.Empty;
        public string QueryField { get; set; } = "message";
    }

    public class TestDocument
    {
        public string Id { get; set; } = string.Empty;
        public string? QueryField { get; set; }
        public string PartitionKey { get; set; } = string.Empty;
        public string? City { get; set; }
    }

    public class SimpleTestDocument
    {
        public string Id { get; set; } = string.Empty;
        public string Foo { get; set; } = string.Empty;
        public string PartitionKey { get; set; } = string.Empty;
        public string QueryField { get; set; } = "simple";
        
        // System properties that Cosmos DB manages (EF Core handles these automatically)
        public string? ETag { get; set; }
    }
}
