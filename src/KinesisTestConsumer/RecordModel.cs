namespace KinesisTestConsumer;

using System;
using Amazon.DynamoDBv2.DataModel;

[DynamoDBTable("Records")]
public class Record
{
    [DynamoDBHashKey]
    public Guid Id {get; set;}
    public DateTime Created {get; set;}
    public String Payload {get; set;}
    public String PartitionKey {get; set;}
    public String ShardId {get; set;}
    public String SequenceNumber {get; set;}
    public String EventId {get; set;}
} 
