namespace ByteFlow.Core.Extensions
{
    public static class StringExtensions
    {
        public static bool IsLengthInRange(this string str, int min, int max)
            => str.Length >= min && str.Length <= max;
    }
}
