
using Google.Cloud.PubSub.V1;
using CRDOrderService.Controllers;
using Newtonsoft.Json; 


namespace CRDOrderService.Services{
    public class PullMessagesAsyncSample
    {
        public async Task<int> PullMessagesAsync(string projectId, string subscriptionId, bool acknowledge)
        {
            SubscriptionName subscriptionName = SubscriptionName.FromProjectSubscription(projectId, subscriptionId);
            SubscriberClient subscriber = await SubscriberClient.CreateAsync(subscriptionName);
            // SubscriberClient runs your message handle function on multiple
            // threads to maximize throughput.
            int messageCount = 0;
            
            Console.WriteLine("start " + subscriptionId);

            Task startTask = subscriber.StartAsync((PubsubMessage message, CancellationToken cancel) =>
            {

                string jsonMessage = message.Data.ToStringUtf8();
                Console.WriteLine("jsonMessage: " + jsonMessage);

                MyMessageData data = JsonConvert.DeserializeObject<MyMessageData>(jsonMessage); 

                if (DemoController._emitters.TryGetValue(data.Id, out var emitter)){
                    emitter.SetResult(data.Value);
                    emitter.TrySetCanceled();
                }else{
                    Console.WriteLine("invalid id " + data.Id);
                }

                Interlocked.Increment(ref messageCount);
                return Task.FromResult(acknowledge ? SubscriberClient.Reply.Ack : SubscriberClient.Reply.Nack);
            });

            Console.WriteLine("end " + subscriptionId);
            return messageCount;
        }
    }

    public class MyMessageData
    {
        public string Id { get; set; }
        public string Value { get; set; }
        // Add other properties as needed
    }

}