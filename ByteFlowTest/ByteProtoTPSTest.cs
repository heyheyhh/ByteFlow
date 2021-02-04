using ByteFlow.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ByteFlowTest
{
    public class ByteProtoTPSTest
    {
        public static (double packTime, double unpackTime) Test(int enumCount)
        {
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
                    Sub = new SubEntity2()
                    {
                        Name = "DOL",
                    },
                },
                List = new List<string>() { "test list item" },
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

            var stopWatch = Stopwatch.StartNew();
            for (int i = 0; i < enumCount; i++)
            {
                ByteProto.Packet(entity);
            }
            stopWatch.Stop();
            var ms = stopWatch.ElapsedMilliseconds / (double)enumCount;

            var bytes = ByteProto.Packet(entity);
            stopWatch.Restart();
            for (int i = 0; i < enumCount; i++)
            {
                ByteProto.Unpacket(bytes);
            }
            stopWatch.Stop();
            var ms2 = stopWatch.ElapsedMilliseconds / (double)enumCount;
            return (ms, ms2);
        }
    }
}
