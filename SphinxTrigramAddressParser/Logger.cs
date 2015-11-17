namespace SphinxTrigramAddressParser
{
    internal abstract class Logger
    {
        public abstract void Write(string msg, MsgType msgType);
    }
}
