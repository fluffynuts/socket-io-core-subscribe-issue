using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NUnit.Framework;
using Socket.Io.Client.Core;
using Socket.Io.Client.Core.Json;
using Socket.Io.Client.Core.Model;

namespace SocketIoCoreTests
{
    [TestFixture]
    public class SocketIoClientCoreTests
    {
        [Test]
        public async Task NoAcksOrHandlerNotifications()
        {
            // Arrange
            var serializer = new NewtonSoftJsonSerializer();
            var logger = new NullLogger<SocketIoClient>();
            var opts = new SocketIoClientOptions(
                serializer,
                logger,
                Encoding.UTF8
            );
            var client = new SocketIoClient(opts);

            client.Events.OnConnect.Subscribe(
                args => Log($"[1] Connected: {args}"));
            client.Events.OnPacket.Subscribe(packet =>
            {
                Log($"[1] Received packet: {packet}");
            });
            string clientId1 = null;
            client.Events.OnHandshake.Subscribe(res =>
            {
                clientId1 = res.Sid;
            });
            // Act
            await client.OpenAsync(new Uri("http://localhost:3300"));
            var data1 = new Payload()
            {
                Id = 42,
                Name = "Douglas Adams"
            };
            var data2 = new Payload()
            {
                Id = 86,
                Name = "Carl Sagan"
            };

            while (clientId1 is null)
            {
                Thread.Sleep(50);
            }

            client.On("/consume")
                .Subscribe(message =>
                {
                    Log($"Received notification: {message.EventName}, data: {message.Data}");
                });

            Log($"- subscribe to 'test' topic {clientId1} -");
            client.Emit("/subscribe", new SubscribeRequest(
                    clientId1,
                    "test",
                    0,
                    10,
                    0
                )
            ).Subscribe(ack =>
            {
                Log($"subscribe ack: {ack}");
            });


            Log("- about to publish - ");
            client.Emit("/publish", new PublishRequest
                {
                    Topic = "test",
                    Channel = 1,
                    Message = JsonConvert.SerializeObject(data1)
                }
            ).Subscribe(ack =>
            {
                Log($"publish ack: {JsonConvert.SerializeObject(ack)}");
            });

            client.Emit("/publish", new PublishRequest
                {
                    Topic = "test",
                    Channel = 1,
                    Message = JsonConvert.SerializeObject(data1)
                }
            ).Subscribe(ack =>
            {
                Log($"publish ack: {JsonConvert.SerializeObject(ack)}");
            });

            // Assert
            Log("- waiting for 2s just in case -");
            await Task.Delay(2000);
            Log("- test ends -");
        }

        private static void Log(string message)
        {
            Console.Error.WriteLine(message);
        }

        public class Payload
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class PublishRequest
        {
            [JsonProperty("topic")]
            public string Topic { get; set; }

            [JsonProperty("channel")]
            public int Channel { get; set; }

            [JsonProperty("uuid")]
            public Guid UUID { get; set; } = Guid.NewGuid();

            [JsonProperty("message")]
            public string Message { get; set; }
        }

        public class SubscribeRequest
        {
            [JsonProperty("id")]
            public string Id { get; }

            [JsonProperty("clientId")]
            public string ClientId { get; }

            [JsonProperty("topic")]
            public string Topic { get; }

            [JsonProperty("channel")]
            public int Channel { get; }

            [JsonProperty("offset")]
            public int Offset { get; }

            [JsonProperty("batchSize")]
            public int BatchSize { get; }

            public SubscribeRequest(
                string clientId,
                string topic,
                int channel,
                int offset,
                int batchSize
            )
            {
                ClientId = clientId;
                Topic = topic;
                Channel = channel;
                Offset = offset;
                BatchSize = batchSize;
            }
        }

        public class NewtonSoftJsonSerializer
            : IJsonSerializer
        {
            public string Serialize<T>(T data)
            {
                return JsonConvert.SerializeObject(data);
            }

            public T Deserialize<T>(string json)
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
        }
    }
}