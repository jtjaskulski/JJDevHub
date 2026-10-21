namespace JJDevHub.EventBus.Kafka
{

    public static class TopicNames
    {
        public static string ForEvent(string eventName)
        {
            var split = eventName.Split('.', 3);
            if (split.Length < 2 || split[0].Length == 0 || split[1].Length == 0)
            {
                throw new ArgumentException("Event name needs two segments.", nameof(eventName));
            }

            return $"{split[0]}.{split[1]}";
        }

        public static string DeadLetter(string topic) => $"{topic}.dlq";
    }
}
