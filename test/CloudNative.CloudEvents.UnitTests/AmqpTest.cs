﻿// Copyright (c) Cloud Native Foundation. 
// Licensed under the Apache 2.0 license.
// See LICENSE file in the project root for full license information.

namespace CloudNative.CloudEvents.UnitTests
{
    using System;
    using System.Net.Mime;
    using CloudNative.CloudEvents.Amqp;
    using global::Amqp;
    using Xunit;
    using static TestHelpers;

    public class AmqpTest
    {
        [Fact]
        public void AmqpStructuredMessageTest()
        {
            // the AMQPNetLite library is factored such
            // that we don't need to do a wire test here


            var jsonEventFormatter = new JsonEventFormatter();
            var cloudEvent = new CloudEvent(CloudEventsSpecVersion.V1_0,
                "com.github.pull.create",
                source: new Uri("https://github.com/cloudevents/spec/pull"),
                subject: "123")
            {
                Id = "A234-1234-1234",
                Time = new DateTimeOffset(2018, 4, 5, 17, 31, 0, TimeSpan.Zero),
                DataContentType = new ContentType(MediaTypeNames.Text.Xml),
                Data = "<much wow=\"xml\"/>"
            };

            var attrs = cloudEvent.GetAttributes();
            attrs["comexampleextension1"] = "value";
            
            var message = new AmqpCloudEventMessage(cloudEvent, ContentMode.Structured, new JsonEventFormatter());
            Assert.True(message.IsCloudEvent());
            var encodedAmqpMessage = message.Encode();

            var message1 = Message.Decode(encodedAmqpMessage);
            Assert.True(message1.IsCloudEvent());
            var receivedCloudEvent = message1.ToCloudEvent();

            Assert.Equal(CloudEventsSpecVersion.Default, receivedCloudEvent.SpecVersion);
            Assert.Equal("com.github.pull.create", receivedCloudEvent.Type);
            Assert.Equal(new Uri("https://github.com/cloudevents/spec/pull"), receivedCloudEvent.Source);
            Assert.Equal("123", receivedCloudEvent.Subject);
            Assert.Equal("A234-1234-1234", receivedCloudEvent.Id);
            AssertTimestampsEqual("2018-04-05T17:31:00Z", receivedCloudEvent.Time.Value);
            Assert.Equal(new ContentType(MediaTypeNames.Text.Xml), receivedCloudEvent.DataContentType);
            Assert.Equal("<much wow=\"xml\"/>", receivedCloudEvent.Data);

            var attr = receivedCloudEvent.GetAttributes();
            Assert.Equal("value", (string)attr["comexampleextension1"]);
        }

        [Fact]
        public void AmqpBinaryMessageTest()
        {
            // the AMQPNetLite library is factored such
            // that we don't need to do a wire test here


            var jsonEventFormatter = new JsonEventFormatter();
            var cloudEvent = new CloudEvent("com.github.pull.create",
                new Uri("https://github.com/cloudevents/spec/pull/123"))
            {
                Id = "A234-1234-1234",
                Time = new DateTimeOffset(2018, 4, 5, 17, 31, 0, TimeSpan.Zero),
                DataContentType = new ContentType(MediaTypeNames.Text.Xml),
                Data = "<much wow=\"xml\"/>"
            };

            var attrs = cloudEvent.GetAttributes();
            attrs["comexampleextension1"] = "value";
            
            var message = new AmqpCloudEventMessage(cloudEvent, ContentMode.Binary, new JsonEventFormatter());
            Assert.True(message.IsCloudEvent());
            var encodedAmqpMessage = message.Encode();

            var message1 = Message.Decode(encodedAmqpMessage);
            Assert.True(message1.IsCloudEvent());
            var receivedCloudEvent = message1.ToCloudEvent();

            Assert.Equal(CloudEventsSpecVersion.Default, receivedCloudEvent.SpecVersion);
            Assert.Equal("com.github.pull.create", receivedCloudEvent.Type);
            Assert.Equal(new Uri("https://github.com/cloudevents/spec/pull/123"), receivedCloudEvent.Source);
            Assert.Equal("A234-1234-1234", receivedCloudEvent.Id);
            AssertTimestampsEqual("2018-04-05T17:31:00Z", receivedCloudEvent.Time.Value);
            Assert.Equal(new ContentType(MediaTypeNames.Text.Xml), receivedCloudEvent.DataContentType);
            Assert.Equal("<much wow=\"xml\"/>", receivedCloudEvent.Data);

            var attr = receivedCloudEvent.GetAttributes();
            Assert.Equal("value", (string)attr["comexampleextension1"]);            
        }

        [Fact]
        public void AmqpNormalizesTimestampsToUtc()
        {
            var cloudEvent = new CloudEvent("com.github.pull.create",
                new Uri("https://github.com/cloudevents/spec/pull/123"))
            {
                Id = "A234-1234-1234",
                // 2018-04-05T18:31:00+01:00 => 2018-04-05T17:31:00Z
                Time = new DateTimeOffset(2018, 4, 5, 18, 31, 0, TimeSpan.FromHours(1)),
                DataContentType = new ContentType(MediaTypeNames.Text.Xml),
                Data = "<much wow=\"xml\"/>"
            };

            var message = new AmqpCloudEventMessage(cloudEvent, ContentMode.Binary, new JsonEventFormatter());
            var encodedAmqpMessage = message.Encode();

            var message1 = Message.Decode(encodedAmqpMessage);
            var receivedCloudEvent = message1.ToCloudEvent();

            AssertTimestampsEqual("2018-04-05T17:31:00Z", receivedCloudEvent.Time.Value);
        }
    }
}