using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using System.IO;
using System.Text;
using System;
using Amazon;
using System.Threading;
using Amazon.Lambda.KinesisEvents;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using System.ComponentModel.Design;

// aws kinesis put-record \
//   --stream-name kinesis-stream \
//   --cli-binary-format raw-in-base64-out \
//   --data '{"key":"user1", "score": 100}' \
//   --partition-key 1 

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
//[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace KinesisTestConsumer
{
    public class KRecord{
        public int Index { get; set; }
        public string Message { get; set; }

    }

    public class Function
    {
        // public const string stream_name = "kinesis-stream";
        // public static RegionEndpoint region = RegionEndpoint.EUWest1;
        // public const string stream_name = "TestSensorDataStream";
        // public const string stream_name = "TestSensorDataStream";
        public string stream_name = "kinesis-stream";
        public static RegionEndpoint region = RegionEndpoint.EUWest1;
        public static AmazonKinesisClient _client = null!;
        private readonly AmazonDynamoDBClient _dynamoDBClient;
        private readonly DynamoDBContext _dbContext;
        private readonly string DatabaseName;
        public Function(){
            AmazonDynamoDBConfig clientConfig = new AmazonDynamoDBConfig();
            _dynamoDBClient = new AmazonDynamoDBClient(clientConfig);
            _dbContext = new DynamoDBContext(_dynamoDBClient);

            if (Environment.GetEnvironmentVariable("TABLE_NAME") == null){
                DatabaseName = "Records";
            }else{
                DatabaseName = Environment.GetEnvironmentVariable("TABLE_NAME");
            }
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public void HandleKinesisRecord(KinesisEvent kinesisEvent)
        {

            Console.WriteLine($"<<<---------------------- Beginning to process {kinesisEvent.Records.Count} records ----------------------");
            // throw new Exception("error");

            foreach (var record in kinesisEvent.Records)
            {
                // string recordData = Encoding.UTF8.GetString(Encoding.UTF8.GetString(record.Data.ToArray()));
                string recordData = Encoding.UTF8.GetString(record.Kinesis.Data.ToArray());
                // Console.WriteLine(recordData);
                // var o = JsonSerializer.Deserialize<KRecord>(recordData);
                // Console.WriteLine($"Partition key: {record.Kinesis.PartitionKey}, Index: {o.Index}, Message: {o.Message}, EventId: {record.EventId}");
                var Id = Guid.NewGuid();
                var recordItem = new Record{
                        Id = Id,
                        Payload = recordData,
                        Created = DateTime.UtcNow,
                        PartitionKey = record.Kinesis.PartitionKey,
                        ShardId = record.EventId.Split(':').First(),
                        EventId = record.EventId,
                        SequenceNumber = record.Kinesis.SequenceNumber
                    };

                Console.WriteLine(JsonSerializer.Serialize(recordItem));

            }
            Thread.Sleep(new Random().Next(3000,3500));
            Console.WriteLine("---------------------- Stream processing complete ---------------------->>>");
        }
        public void WriteToDynamo(Record record){
            var batch = _dbContext.CreateBatchWrite<Record>();
                
            batch.AddPutItem(record);
            Console.WriteLine("Writing to dynamo");
            try
            {
                batch.ExecuteAsync().Wait();
                Console.WriteLine("Wrote to dynamo");
            }
            catch (System.Exception e)
            {
                Console.WriteLine($"Exception + {e.Message}");
                throw;
            }
        }


        public void PutKinesisRecord(){
            
            _client = new AmazonKinesisClient(region);
            var key = Guid.NewGuid();
            for (int i = 0; i <= 20000; i++)
            {
                if(i % 10 == 0){
                    key = Guid.NewGuid();
                }
                var o = new KRecord{ Index = i, Message = $"This message was created at {DateTime.UtcNow}" };

                byte[] oByte = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(o));
                using (MemoryStream ms = new MemoryStream(oByte))
                {
                    //create config that points to AWS region

                    //create put request
                    PutRecordRequest requestRecord = new PutRecordRequest();
                    //list name of Kinesis stream
                    requestRecord.StreamName = stream_name;
                    //give partition key that is used to place record in particular shard
                    requestRecord.PartitionKey = key.ToString();
                    //add record as memorystream
                    requestRecord.Data = ms;

                    //PUT the record to Kinesis
                    try
                    {
                        PutRecordResponse responseRecord = _client.PutRecordAsync(requestRecord).Result;    
                        Console.WriteLine($"Message: {o.Message}, Index: {o.Index}");
                        Console.WriteLine($"Shardid: {responseRecord.ShardId}");
                        Console.WriteLine($"Sequence: {responseRecord.SequenceNumber}");
                        Console.WriteLine("--------------\n");
                    }
                    catch (System.Exception ex)
                    {
                        
                        throw;
                    }

                    //show shard ID and sequence number to user
                    
                }
                
            }
            
            
        }



        private string GetRecordContents(KinesisEvent.Record streamRecord)
        {
            using (var reader = new StreamReader(streamRecord.Data, Encoding.ASCII))
            {
                return reader.ReadToEnd();
            }
        }
        
        protected async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            _client = new AmazonKinesisClient(region);
            var o = new { Key = "1", Message = "Hello" };

            byte[] oByte = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(o));
            using (MemoryStream ms = new MemoryStream(oByte))
            {
                //create config that points to AWS region

                //create put request
                PutRecordRequest requestRecord = new PutRecordRequest();
                //list name of Kinesis stream
                requestRecord.StreamName = "lambda-stream";
                //give partition key that is used to place record in particular shard
                requestRecord.PartitionKey = "key";
                //add record as memorystream
                requestRecord.Data = ms;

                //PUT the record to Kinesis
                PutRecordResponse responseRecord = await _client.PutRecordAsync(requestRecord);

                //show shard ID and sequence number to user
                var shardId = "Shard ID: " + responseRecord.ShardId;
                var sequence = "Sequence #:" + responseRecord.SequenceNumber;
            }
            return new APIGatewayProxyResponse();
        }
        private static async Task Consume()
        {

            var stream_name = "kinesis-stream";
            region = RegionEndpoint.USEast1;
            
            // Get iterator for shard
            _client = new AmazonKinesisClient(region);

            // Describe stream to get list of shards
            var describeRequest = new DescribeStreamRequest()
            {
                StreamName = stream_name
            };
            var describeResponse = await _client.DescribeStreamAsync(describeRequest);
            List<Shard> shards = describeResponse.StreamDescription.Shards;
            var shard = shards.FirstOrDefault().ShardId;
            var iteratorRequest = new GetShardIteratorRequest()
            {
                StreamName = stream_name,
                ShardId = shard,
                ShardIteratorType = Amazon.Kinesis.ShardIteratorType.TRIM_HORIZON
            };

            // Retrieve and display records for shard
            GetShardIteratorResponse iteratorResponse;
            try
            {
                iteratorResponse = await _client.GetShardIteratorAsync(iteratorRequest);
            }
            catch (System.Exception)
            {
                
                throw;
            }
            Console.WriteLine("here");
            string iterator = iteratorResponse.ShardIterator;

            while (iterator != null)
            {
                // Get records from iterator

                var getRequest = new GetRecordsRequest()
                {
                    Limit = 100,
                    ShardIterator = iterator
                };

                var getResponse = await _client.GetRecordsAsync(getRequest);
                var records = getResponse.Records;

                // Display records

                if (records.Count > 0)
                {
                    var record = records.FirstOrDefault();
                    // foreach (var record in records)
                    // {
                        var recordDisplay = Encoding.UTF8.GetString(record.Data.ToArray());
                        Console.WriteLine($"Record: {recordDisplay}, Partition Key: {record.PartitionKey}, Shard: {shard}, seq: {record.SequenceNumber}");
                    // }
                }
                iterator = getResponse.NextShardIterator;
                Thread.Sleep(100);
            }
        }
    }
}
