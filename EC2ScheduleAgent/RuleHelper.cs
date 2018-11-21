using Amazon.CloudWatchEvents;
using Amazon.CloudWatchEvents.Model;
using Amazon.EC2.Model;
using Amazon.Lambda;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EC2ScheduleAgent
{
    static public class RuleHelper
    {

        async static public Task RemoveDeadRules(List<Instance> instances, ILambdaContext context)
        {
            var client = Factories.AmazonCloudWatchEventsClient();

            context.Logger.LogLine($"Remove dead schedule items...");

            var rules = await RuleHelper.GetRules().ConfigureAwait(false);

            foreach (var rule in rules)
            {
                context.Logger.LogLine($"Find InstanceId+Tag:" + rule.Name);

                var result = RuleHelper.ParseRule(rule, out string instanceId, out string action);

                var instance = from i in instances
                               where i.InstanceId == instanceId
                               from t in i.Tags
                               where t.Key == action
                               select i;

                if (instance.Count() != 1)
                {
                    context.Logger.LogLine($"InstanceId+Tag not found. Deleting Rule:" + rule.Name);

                    var listTargetsByRuleRequest = new ListTargetsByRuleRequest()
                    {
                        Rule = rule.Name,
                    };

                    var listTargetsByRuleResponse = await client.ListTargetsByRuleAsync(listTargetsByRuleRequest).ConfigureAwait(false);

                    var ids = (from t in listTargetsByRuleResponse.Targets select t.Id).ToList<string>();

                    var removeTargetsRequest = new RemoveTargetsRequest()
                    {
                        Rule = rule.Name,
                        Ids = ids
                    };

                    var removeTargetsResponse = await client.RemoveTargetsAsync(removeTargetsRequest).ConfigureAwait(false);

                    var deleteRuleRequest = new DeleteRuleRequest()
                    {
                        Name = rule.Name

                    };

                    var deleteRuleResponse = await client.DeleteRuleAsync(deleteRuleRequest).ConfigureAwait(false);
                }


            }

            context.Logger.LogLine($"Remove dead schedule items complete.");
        }
        async static public Task CreateRule(Instance instance, Amazon.EC2.Model.Tag tag, ILambdaContext context)
        {
            try
            {

                var client = Factories.AmazonCloudWatchEventsClient();

                var putRuleRequest = new PutRuleRequest
                {
                    Name = tag.Key + "_" + instance.InstanceId,
                    Description = "Turns machine with InstanceID " + tag.Key + ", by the schedule specified in the instance tags.",
                    RoleArn = null,
                    //ScheduleExpression = "rate(3 minutes)",
                    //  EventPattern = "0 15 10 ? * MON-FRI",
                    ScheduleExpression = "cron(" + tag.Value + ")",
                    State = RuleState.ENABLED,

                };

                var putRuleResponse = await client.PutRuleAsync(putRuleRequest).ConfigureAwait(false);

                var functionConfiguration = await GetTargetFunction(context).ConfigureAwait(false);

                string payload = RuleHelper.CreateRulePayload(instance, tag);

                var targets = new List<Target>
                {
                    new Target
                    {
                        //RoleArn = functionConfiguration.Role,
                        Arn = functionConfiguration.FunctionArn,
                        Input = payload,
                        Id = putRuleRequest.Name
                    }
                };

                var putTargetRequest = new PutTargetsRequest
                {
                    Rule = putRuleRequest.Name,
                    Targets = targets
                };

                var putTargetResponse = await client.PutTargetsAsync(putTargetRequest).ConfigureAwait(false);

                var log = Newtonsoft.Json.JsonConvert.SerializeObject(putTargetResponse);
                context.Logger.LogLine("putTargetResponse:");
                context.Logger.LogLine(log);

            }
            catch (Exception)
            {

                throw;
            }
        }

        async static Task<List<Rule>> GetRules()


        {

            var client = Factories.AmazonCloudWatchEventsClient();
            var listRulesResponse = await client.ListRulesAsync().ConfigureAwait(false);

            string nextToken = null;
            ListRulesResponse response = null;
            List<Rule> allrules = new List<Rule>();

            do
            {

                var request = new ListRulesRequest()
                {
                    Limit = Constants.LIMIT,
                    NextToken = nextToken
                };

                response = await client.ListRulesAsync(request).ConfigureAwait(false);
                nextToken = response.NextToken;

                allrules.AddRange(response.Rules);

            } while (response.NextToken != null);

            var rules = (from r in allrules
                         where r.Name.StartsWith("OFF_", StringComparison.CurrentCulture) | r.Name.StartsWith("ON_", StringComparison.CurrentCulture)
                         select r).ToList();

            return rules;
        }
        static bool ParseRule(Rule rule, out string instanceId, out string action)
        {
            instanceId = null;
            action = null;

            try
            {
                if (rule.Name.StartsWith("ON_", StringComparison.CurrentCulture) | rule.Name.StartsWith("OFF_", StringComparison.CurrentCulture))
                {
                    action = rule.Name.Split('_')[0];
                    instanceId = rule.Name.Split('_')[1];
                }
                return true;
            }
            catch (Exception)

            {
                return false;
            }
        }
        static string CreateRulePayload(Instance instance, Tag tag)
        {
            var controlRequest = new ControlRequest()
            {
                Action = (ControlRequest.EnumAction)Enum.Parse(typeof(ControlRequest.EnumAction), tag.Key.ToUpper(CultureInfo.CurrentCulture)),
                InstanceID = instance.InstanceId
            };

            var payload = Newtonsoft.Json.JsonConvert.SerializeObject(controlRequest);
            return payload;
        }
        async static Task<FunctionConfiguration> GetTargetFunction(ILambdaContext context)
        {
            //arn:aws:lambda:us-east-2:327425660322:function:aaa-ControlInstance-JLQTS4DLS7S
            //arn:aws:lambda:us-east-2:327425660322:function:aaa-UpdateSchedule-17X4YM2Z2U5K5

            var client = Factories.AmazonLambdaClient();
            var request = new ListFunctionsRequest()
            {
                FunctionVersion = FunctionVersion.ALL,
            };
            var response = await client.ListFunctionsAsync().ConfigureAwait(false);

            //var sz1 = Newtonsoft.Json.JsonConvert.SerializeObject(context);
            //context.Logger.LogLine("context:");
            //context.Logger.LogLine(sz1);


            var prefix = context.InvokedFunctionArn.Replace(context.FunctionName, "", StringComparison.CurrentCulture);


            FunctionConfiguration function = (from f in response.Functions
                                              where f.FunctionArn.StartsWith(prefix, StringComparison.CurrentCulture) 
                                                  & f.FunctionName.Contains("ControlInstance", StringComparison.CurrentCulture)
                                              select f).FirstOrDefault();

            var sz = Newtonsoft.Json.JsonConvert.SerializeObject(function);
            context.Logger.LogLine("FunctionConfiguration:");
            context.Logger.LogLine(sz);

            return function;
        }
    }
}
