using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;

using EC2ScheduleAgent;
using System.Text;
using Amazon;
using Amazon.EC2.Model;
using Amazon.EC2;
using System.IO;

namespace EC2ScheduleAgent.Tests
{
    public class FunctionTest
    {
        public FunctionTest()
        {
        }

        [Fact]
        public void Serialize()
        {
            var request = new ControlRequest()
            {
                Action = ControlRequest.EnumAction.OFF,
                InstanceID = "i-03b0fee8530f0a900"
            };

            var sz = Newtonsoft.Json.JsonConvert.SerializeObject(request);

            System.Diagnostics.Trace.WriteLine(sz);
        }

        [Fact]
        static public void Deserialize()
        {
            var ser = new Amazon.Lambda.Serialization.Json.JsonSerializer();
            var ms = new System.IO.MemoryStream(Encoding.UTF8.GetBytes("{\"InstanceID\":\"i-03b0fee8530f0a900\",\"Action\":0}"));
            var request =  ser.Deserialize<ControlRequest>(ms);
            Xunit.Assert.Equal("i-03b0fee8530f0a900", request.InstanceID);
        }
        [Fact]
        async static public Task TestUpdateSchedule()
        {
            var json = @"
                {
                'FunctionName': 'aaa-UpdateSchedule-194Z09O3850DV',
                'FunctionVersion': '$LATEST',
                'LogGroupName': '/aws/lambda/aaa-UpdateSchedule-194Z09O3850DV',
                'LogStreamName': '2018/10/04/[$LATEST]0a8fe19fa9b349e294c5129c037cf417',
                'MemoryLimitInMB': 256,
                'AwsRequestId': '6257450b-c808-11e8-9547-53345a670d4d',
                'InvokedFunctionArn': 'arn:aws:lambda:us-east-2:327425660322:function:aaa-UpdateSchedule-194Z09O3850DV',
                'RemainingTime': '00:00:29.6130000',
                'ClientContext': null,
                'Logger': { }
                }
            ";

            var context = Newtonsoft.Json.JsonConvert.DeserializeObject<TestLambdaContext>(json);

            await Lambdas.UpdateSchedule(null, context).ConfigureAwait(false);

            var testLogger = context.Logger as TestLambdaLogger;
            Assert.Contains("UpdateSchedule complete", testLogger.Buffer.ToString(), StringComparison.CurrentCulture);
        }

        [Fact]
        async public Task TestControlInstanceOff()
        {

            var context = new TestLambdaContext();
            var function = new Lambdas();
            var request = new ControlRequest() { InstanceID = "i-003106a788f774c9e", Action = ControlRequest.EnumAction.OFF };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(request);
            await function.ControlInstance(StreamFromString(json), context).ConfigureAwait(false);

        }

        [Fact]
        async public Task TestControlInstanceOn()
        {

            var context = new TestLambdaContext();
            var function = new Lambdas();
            var request = new ControlRequest() { InstanceID = "i-003106a788f774c9e", Action = ControlRequest.EnumAction.ON };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(request);
            await function.ControlInstance(StreamFromString(json), context).ConfigureAwait(false);

        }

        [Fact]
        async static public Task ListLambdas()
        {
            var region = RegionEndpoint.GetBySystemName("us-east-2");
            var client = new Amazon.Lambda.AmazonLambdaClient(region);
            var response = await client.ListFunctionsAsync().ConfigureAwait(false);
            foreach (var item in response.Functions)
            {
                System.Diagnostics.Trace.WriteLine(item.FunctionArn);
            }

        }

        [Fact]
        async public Task CreateInstances()
        {
            var client = Factories.EC2Client();


            string amiID = "ami-0ca3e3965ada31684";
            string keyPairName = "joseph.longo@live.com";

            List<string> groups = new List<string>() { "sg-0d621e5d5ab57d3a8" };

            var tagOn = new Tag() { Key = "ONx", Value = "0 14 ? * MON-FRI *" };
            var tagOff = new Tag() { Key = "OFF", Value = "0 15 ? * MON-FRI *" };
            var launchRequest = new RunInstancesRequest()
            {
                Placement = new Placement()
                {
                    AvailabilityZone = "us-east-2c",
                    GroupName = null,
                    Tenancy = "default"
                },
                ImageId = amiID,
                InstanceType = InstanceType.T2Micro,
                MinCount = 1,
                MaxCount = 1,
                KeyName = keyPairName,
                SecurityGroupIds = groups,
                TagSpecifications = new List<TagSpecification>()
                {
                    new TagSpecification()
                        {
                            ResourceType = "instance",
                            Tags = new List<Tag>()
                            {
                                tagOn, tagOff
                            }                        
                    }
                }
            };




            for (int i = 0; i < 100; i++)
            {
                RunInstancesResponse launchResponse = await client.RunInstancesAsync(launchRequest).ConfigureAwait(false);

            }

        }
        public static Stream StreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
