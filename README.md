# ec2-scheduler (AWS Serverless Application Project)
Starts and Stop properly tagged AWS EC2 Instances

Requires: https://marketplace.visualstudio.com/items?itemName=AmazonWebServices.AWSToolkitforVisualStudio2017

## Here are some steps to follow from Visual Studio:

To deploy your Serverless application, right click the project in Solution Explorer and select *Publish to AWS Lambda*.<br>

You will need to create a Cloud Formation Stack called 'ec2-scheduler'
You will need to create a S3 Bucket called 'ec2-scheduler'

To view your deployed application open the Stack View window by double-clicking the stack name shown beneath the AWS CloudFormation node in the AWS Explorer tree. 

## Configuring EC2 Instances for scheduling
Each Instance with an 'ON' or 'OFF' tag will be processed by the ec2-scheduler<br>
The values follow AWS cron format - this is a special format(as far as I can tell)<br>
Times are calculated as GMT NOT as your local time.<br>
Examples:<br>
<b>0 15 ? * MON-FRI *</b> Mon-Fri at 15:00 GMT<br>
<b>0 15 ? * * *</b> Every day at 15:00 GMT<br>
<b>30 15 ? * * *</b> Every day at 15:30 GMT<br>

You can create a rule in CloudWatch, and get a screen where you can enter an expression and it will evaluate it for you.<br>
See: https://stackoverflow.com/questions/47183071/aws-lambda-cron-expression-for-5-minutes-before-each-hour/52599451#52599451

## AWS Items Created
Lambdas can be viewed under AWS Lambda<br>
Rules can be viewed under CloudWatch|Events|Rules<br>
Logs can be viewed under CloudWatch|Logs<br>


## Here are some steps to follow to get started from the command line:

Once you have edited your template and code you can deploy your application using the [Amazon.Lambda.Tools Global Tool](https://github.com/aws/aws-extensions-for-dotnet-cli#aws-lambda-amazonlambdatools) from the command line.

Install Amazon.Lambda.Tools Global Tools if not already installed.
```
    dotnet tool install -g Amazon.Lambda.Tools
```

If already installed check if new version is available.
```
    dotnet tool update -g Amazon.Lambda.Tools
```

Execute unit tests
```
    cd "EC2ScheduleAgent/test/EC2ScheduleAgent.Tests"
    dotnet test
```

Deploy application
```
    cd "EC2ScheduleAgent/src/EC2ScheduleAgent"
    dotnet lambda deploy-serverless
```


