namespace AramBenchSwap.Core
{
    public sealed class SwapResult
    {
        private SwapResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public bool Success { get; private set; }
        public string Message { get; private set; }

        public static SwapResult Ok()
        {
            return new SwapResult(true, "Swapped.");
        }

        public static SwapResult Failure(string message)
        {
            return new SwapResult(false, message);
        }
    }
}
