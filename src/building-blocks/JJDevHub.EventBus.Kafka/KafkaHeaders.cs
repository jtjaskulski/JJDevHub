using System.Diagnostics;
using System.Globalization;
using System.Text;

using JJDevHub.EventBus;

using Confluent.Kafka;

namespace JJDevHub.EventBus.Kafka
{

    public static class KafkaHeaders
    {
        public const string EventId = "event-id";

        public const string EventType = "event-type";

        public const string OccurredOn = "occurred-on";

        public const string TraceParent = "traceparent";

        public const string Error = "error";

        private const int ErrorLimit = 2000;

        public static Message<string, string> Create(IntegrationEnvelope envelope)
        {
            var headers = new Headers
            {
                { EventId, Bytes(envelope.EventId.ToString()) },
                { EventType, Bytes(envelope.Name) },
                { OccurredOn, Bytes(envelope.OccurredOn.ToString("O", CultureInfo.InvariantCulture)) },
            };
            var traceParent = Activity.Current?.Id;
            if (!string.IsNullOrWhiteSpace(traceParent))
            {
                headers.Add(TraceParent, Bytes(traceParent));
            }

            return new Message<string, string>
            {
                Key = envelope.PartitionKey,
                Value = envelope.Payload,
                Headers = headers,
            };
        }

        public static bool TryRead(Message<string, string> message, out IntegrationEnvelope? envelope)
        {
            envelope = null;
            if (message.Headers is null)
            {
                return false;
            }

            if (!TryValue(message.Headers, EventId, out var eventId) || !Guid.TryParse(eventId, out var id))
            {
                return false;
            }

            if (!TryValue(message.Headers, EventType, out var name))
            {
                return false;
            }

            if (!TryValue(message.Headers, OccurredOn, out var occurredOn)
                || !DateTimeOffset.TryParse(
                    occurredOn,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsed))
            {
                return false;
            }

            envelope = new IntegrationEnvelope(id, name, message.Key ?? "", message.Value ?? "", parsed);
            return true;
        }

        public static Message<string, string> CopyWithError(Message<string, string> message, string error)
        {
            var headers = new Headers();
            if (message.Headers is not null)
            {
                CopyHeaders(message.Headers, headers);
            }

            headers.Add(Error, Bytes(Trim(error)));
            return new Message<string, string>
            {
                Key = message.Key,
                Value = message.Value,
                Headers = headers,
            };
        }

        public static Activity? StartConsumerActivity(Headers? headers)
        {
            if (headers is null || !TryValue(headers, TraceParent, out var traceParent))
            {
                return null;
            }

            if (!ActivityContext.TryParse(traceParent, null, out var parent))
            {
                return null;
            }

            return new Activity("KafkaConsumer")
                .SetParentId(parent.TraceId, parent.SpanId, parent.TraceFlags)
                .Start();
        }

        private static void CopyHeaders(Headers source, Headers target)
        {
            foreach (var header in source)
            {
                if (header.Key == Error)
                {
                    continue;
                }

                var value = header.GetValueBytes();
                if (value is null)
                {
                    continue;
                }

                target.Add(header.Key, value);
            }
        }

        private static bool TryValue(Headers headers, string key, out string value)
        {
            value = "";
            if (!headers.TryGetLastBytes(key, out var bytes) || bytes is null || bytes.Length == 0)
            {
                return false;
            }

            value = Encoding.UTF8.GetString(bytes);
            return true;
        }

        private static string Trim(string error)
        {
            if (error.Length <= ErrorLimit)
            {
                return error;
            }

            return error[..ErrorLimit];
        }

        private static byte[] Bytes(string value) => Encoding.UTF8.GetBytes(value);
    }
}
