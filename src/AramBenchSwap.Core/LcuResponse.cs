namespace AramBenchSwap.Core
{
    public sealed class LcuResponse
    {
        public LcuResponse(int statusCode, string body)
        {
            StatusCode = statusCode;
            Body = body ?? string.Empty;
        }

        public int StatusCode { get; private set; }
        public string Body { get; private set; }
    }
}
