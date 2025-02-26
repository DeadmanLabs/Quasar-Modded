﻿using ProtoBuf;
using Quasar.Common.Messages.other;
using Quasar.Common.Models;

namespace Quasar.Common.Messages.FunStuff
{
    [ProtoContract]
    public class DoInputLatency : IMessage
    {
        [ProtoMember(1)]
        public string Message { get; set; }
    }
}
