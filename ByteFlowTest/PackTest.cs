using ByteFlow.Protocol;
using ByteFlow.Serializers;
using System;
using System.Collections.Generic;
using System.Text;

namespace ByteFlowTest
{
    public class PackTest
    {
        public static void Test()
        {
            var simple = new SimpleEntity()
            {
                Id = 1,
                Time = DateTimeOffset.Now
            };
            var simpleBytes = ByteProto.Packet(simple);
            var desSimple = ByteProto.Unpacket(simpleBytes);

            var entity = new Entity()
            {
                Index = 1,
                Id = DateTimeOffset.Now.Ticks,
                Name = "福建首次",
                SubEntity = new SubEntity()
                {
                    Type = 2,
                    Balance = 2021.84,
                    Currency = "CNY",
                },
                List = new List<Guid?>() { Guid.NewGuid(), null, Guid.NewGuid() },
                Arr = new string[] { "Test arr item" },
                Subs = new List<SubEntity>()
                {
                    new SubEntity()
                    {
                        Type = 3,
                        Balance = 2022.84,
                        Currency = "USD",
                    },
                    new SubEntity()
                    {
                        Type = 4,
                        Balance = 2023.84,
                        Currency = "CNY",
                    }
                },
                SubIds = new List<byte>() { 2, 3, 4 },
                Duration = TimeSpan.FromSeconds(80),
                DateTime = DateTimeOffset.Now,
                Checked = true,
                Uid = Guid.NewGuid(),
            };

            var bytes = ByteProto.Packet(entity);
            var des = ByteProto.Unpacket(bytes);

            var json = SerializerFactory.GetTextJsonSerializer().Serialize(entity);
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            //var (packTime, unpackTime) = ByteProtoTPSTest.Test(100000);

            Console.WriteLine("Hello World!");
        }
    }
}