namespace AramBenchSwap.Core
{
    public sealed class LcuConnection
    {
        public LcuConnection(string processName, int processId, int port, string password, string protocol)
        {
            ProcessName = processName;
            ProcessId = processId;
            Port = port;
            Password = password;
            Protocol = protocol;
        }

        public string ProcessName { get; private set; }
        public int ProcessId { get; private set; }
        public int Port { get; private set; }
        public string Password { get; private set; }
        public string Protocol { get; private set; }
        public string BaseUrl { get { return Protocol + "://127.0.0.1:" + Port; } }
    }
}
