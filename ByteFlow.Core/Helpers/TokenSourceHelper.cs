using System;
using System.Threading;

namespace ByteFlow.Helpers
{
    public static class TokenSourceHelper
    {
        public static void Cancel(ref CancellationTokenSource? tokenSource)
        {
            try
            {
                tokenSource?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                tokenSource = null;
                // Ignore
            }
        }

        public static void Cancel(ref CancellationTokenSource? tokenSource, bool throwOnFirstException)
        {
            try
            {
                tokenSource?.Cancel(throwOnFirstException);
            }
            catch (ObjectDisposedException)
            {
                tokenSource = null;
                // Ignore
            }
        }

        
        public static void Dispose(ref CancellationTokenSource? tokenSource, bool cancelFirst = true)
        {
            if (tokenSource is null)
            {
                return;
            }

            try
            {
                if (cancelFirst)
                {
                    tokenSource.Cancel();
                }
                tokenSource.Dispose();
            }
            catch (Exception)
            {
                // Ignore
            }
            finally
            {
                tokenSource = null;
            }
        }

        public static void Create(ref CancellationTokenSource? tokenSource)
        {
            Dispose(ref tokenSource);
            tokenSource = new CancellationTokenSource();
        }
        
        public static void Create(ref CancellationTokenSource? tokenSource, out CancellationToken token)
        {
            Dispose(ref tokenSource);
            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;
        }
    }
}
