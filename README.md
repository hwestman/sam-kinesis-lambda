# sam-kinesis-lambda


Deploy
- kinesis stream
- lambda
- dynamodb table
- sqs queue
```
sam build
sam deploy
```

## Run locally using vs code launcher
---------------------------------
> `Sam HandleKinesisRecord local event` - 
Pass a dummy event from the events folder to the lambda function


>`Sam put` - 
Put record on kinesis stream


>`Sam put kinesis record` - 
A variant of put


>`Sam consume` - 
Consume records from a live stream



