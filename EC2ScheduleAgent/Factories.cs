using Amazon;
using Amazon.CloudWatchEvents;
using Amazon.EC2;
using System;
using System.Collections.Generic;
using System.Text;
using Amazon.Lambda;

namespace EC2ScheduleAgent
{
    static public class Factories
    {
        public static IAmazonEC2 EC2Client()
        {
#if DEBUG
            var newRegion = RegionEndpoint.GetBySystemName("us-east-2");
            return new Amazon.EC2.AmazonEC2Client(newRegion);

#endif
#if !DEBUG
            return new Amazon.EC2.AmazonEC2Client();

#endif
        }

        public static AmazonCloudWatchEventsClient AmazonCloudWatchEventsClient()
        {
#if DEBUG
            var newRegion = RegionEndpoint.GetBySystemName("us-east-2");
            return new AmazonCloudWatchEventsClient(newRegion);

#endif
#if !DEBUG
            return new AmazonCloudWatchEventsClient();

#endif
        }

        public static AmazonLambdaClient AmazonLambdaClient()
        {
#if DEBUG
            var newRegion = RegionEndpoint.GetBySystemName("us-east-2");
            return new Amazon.Lambda.AmazonLambdaClient(newRegion);

#endif
#if !DEBUG
            return new Amazon.Lambda.AmazonLambdaClient();

#endif
        }
    }
}
