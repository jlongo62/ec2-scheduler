using System;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Lambda.Core;
using System.Threading.Tasks;
using Amazon.CloudWatchEvents;
using Amazon.CloudWatchEvents.Model;
using System.Collections.Generic;
using Amazon;
using System.Linq;
using Amazon.Lambda.APIGatewayEvents;
using System.Net;
using Amazon.Lambda.Model;
using Amazon.Lambda;
using System.Globalization;

namespace EC2ScheduleAgent
{
    public class Lambdas
    {

        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public Lambdas()
        {
        }

        async public Task ControlInstance(System.IO.Stream stream, ILambdaContext context)
        {

            try
            {

                var ser = new Amazon.Lambda.Serialization.Json.JsonSerializer();
                var controlRequest = ser.Deserialize<ControlRequest>(stream);

                context.Logger.LogLine($"Instance: " + controlRequest.InstanceID + "-->" + controlRequest.Action);

                var client = Factories.EC2Client();

                var request = new DescribeInstanceStatusRequest()
                {
                    IncludeAllInstances = true,
                    InstanceIds = new List<string> { controlRequest.InstanceID }
                };

                var response = await client.DescribeInstanceStatusAsync(request).ConfigureAwait(false);
                var state = response.InstanceStatuses.First().InstanceState;
                var instanceId = controlRequest.InstanceID;


                switch (controlRequest.Action)
                {
                    case ControlRequest.EnumAction.ON:
                        {
                            if (state.Code == 80)
                            {
                                context.Logger.LogLine($"Instance State:" + state.Name + ", Starting instance...");

                                var startInstancesRequest = new StartInstancesRequest()
                                {
                                    InstanceIds = new List<string>() { instanceId }
                                };
                                var startInstancesResponse = await client.StartInstancesAsync(startInstancesRequest).ConfigureAwait(false);
                                context.Logger.LogLine($"StartingInstances State:" + startInstancesResponse.StartingInstances.First());
                            }
                            else
                            {
                                context.Logger.LogLine($"Instance State:" + state.Name + ", Ignoring request.");
                            }
                        }
                        break;
                    case ControlRequest.EnumAction.OFF:
                        {
                            if (state.Code == 16)
                            {
                                context.Logger.LogLine($"Instance State:" + state.Name + ", Stopping instance...");

                                var stopInstancesRequest = new StopInstancesRequest()
                                {
                                    InstanceIds = new List<string>() { instanceId }
                                };
                                var stopInstancesResponse = await client.StopInstancesAsync(stopInstancesRequest).ConfigureAwait(false);
                                context.Logger.LogLine($"StoppingInstances State:" + stopInstancesResponse.StoppingInstances.First());
                            }
                            else
                            {
                                context.Logger.LogLine($"Instance State:" + state.Name + ", Ignoring request.");
                            }
                        }
                        break;
                    default:
                        break;
                }

            }
            catch (AmazonEC2Exception ex)
            {
                context.Logger.LogLine("Caught Exception: " + ex.Message);
                context.Logger.LogLine("Response Status Code: " + ex.StatusCode);
                context.Logger.LogLine("Error Code: " + ex.ErrorCode);
                context.Logger.LogLine("Error Type: " + ex.ErrorType);
                context.Logger.LogLine("Request ID: " + ex.RequestId);
                context.Logger.LogLine("Request ID: " + ex.ToString());
            }
            finally
            {
                context.Logger.LogLine("ControlInstance complete.");

            }
        }
        static async public Task UpdateSchedule(object input, ILambdaContext context)
        {

            try
            {

                context.Logger.LogLine($"Beginning to enumerate instances...");

                var instances = await EC2Helper.GetInstances().ConfigureAwait(false);

                await RuleHelper.RemoveDeadRules(instances, context).ConfigureAwait(false);

                foreach (var instance in instances)
                {
                    foreach (var tag in instance.Tags)
                    {
                        switch (tag.Key)
                        {
                            case "ON":
                            case "OFF":
                                context.Logger.LogLine(string.Format(CultureInfo.CurrentCulture, "InstanceID: {0} Key: {1} Value {2}", instance.InstanceId, tag.Key, tag.Value));
                                await RuleHelper.CreateRule(instance, tag, context).ConfigureAwait(false);
                                break;
                            default:
                                break;
                        }
                    }
                }

            }
            catch (AmazonEC2Exception ex)
            {
                context.Logger.LogLine("Caught Exception: " + ex.Message);
                context.Logger.LogLine("Response Status Code: " + ex.StatusCode);
                context.Logger.LogLine("Error Code: " + ex.ErrorCode);
                context.Logger.LogLine("Error Type: " + ex.ErrorType);
                context.Logger.LogLine("Request ID: " + ex.RequestId);
                context.Logger.LogLine("Request ID: " + ex.ToString());
            }
            finally
            {
                context.Logger.LogLine("UpdateSchedule complete.");
                context.Logger.LogLine(new string('-', 45));

            }

        }
        static public string LogEvent(System.IO.Stream stream, ILambdaContext context)
        {
            var sr = new System.IO.StreamReader(stream);
            var sz = sr.ReadToEnd();
            context.Logger.Log(sz);

            return sz;
        }

    }
}

