namespace JJDevHub.EventBus.Kafka
{

    public sealed class KafkaOptions
    {
        public string BootstrapServers { get; set; } = "";

        public string ConsumerGroup { get; set; } = "";

        public string[] SubscribedTopics { get; set; } = [];

        public string[] PublishedTopics { get; set; } = [];

        public int MaxDeliveryAttempts { get; set; } = 5;
    }
}
