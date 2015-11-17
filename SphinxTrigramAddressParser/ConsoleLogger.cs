using System;

namespace SphinxTrigramAddressParser
{
    internal class ConsoleLogger : Logger
    {
        public override void Write(string msg, MsgType msgType)
        {
            switch (msgType)
            {
                case MsgType.ErrorMsg:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case MsgType.WarningMsg:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case MsgType.InformationMsg:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
            }
            Console.WriteLine(msg);
            Console.ResetColor();
        }
    }
}
