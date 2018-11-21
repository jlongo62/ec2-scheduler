using Amazon.EC2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EC2ScheduleAgent
{
    public static class EC2Helper
    {

        async static public Task<List<Instance>> GetInstances()
        {

            var client = Factories.EC2Client();

            string nextToken = null;
            DescribeInstancesResponse response = null;
            List<Reservation> reservations = new List<Reservation>();
            List<Instance> instances = null;

            do
            {
                var request = new DescribeInstancesRequest()
                {
                    MaxResults = Constants.LIMIT,
                    NextToken = nextToken
                };

                response = await client.DescribeInstancesAsync(request).ConfigureAwait(false);
                nextToken = response.NextToken;

                reservations.AddRange(response.Reservations);

            } while (response.NextToken != null);

            instances = (from r in reservations
                         from i in r.Instances
                         where i.State.Code != 48
                         select i).ToList();

            return instances;
        }
    }
}
