namespace Floudy.API.Utility
{
    public static class GlobalIdManager
    {
        public static long BaseValue { get; set; } = 1;

        private static readonly Stack<long> available = new();
        private static readonly HashSet<long> available_hash = [];

        static GlobalIdManager() => Reset();

        public static void Reset()
        {
            available.Clear();
            available_hash.Clear();

            available.Push(BaseValue);
            available_hash.Add(BaseValue);
        }

        public static long NextAvailable() => available.Peek();

        public static long Register()
        {
            var value = available.Pop();
            available_hash.Remove(value);

            if (available.Count == 0)
            {
                available.Push(value + 1);
                available_hash.Add(BaseValue = value + 1);
            }

            return value;
        }

        public static bool Unregister(long value)
        {
            if (value <= 0 || value > BaseValue || available_hash.Contains(value)) return false;

            available.Push(value);
            available_hash.Add(value);
            return true;
        }
    }

    public static class DataSizeConverter
    {
        public static FileSize ConvertToOptimalSize(long byte_size)
        {
            double kb_size = 1024,
                   mb_size = kb_size * kb_size,
                   gb_size = mb_size * kb_size,
                   tb_size = gb_size * kb_size;

            if (byte_size < kb_size) return new FileSize(byte_size, DataSize.B);
            if (byte_size < mb_size) return new FileSize(byte_size / kb_size, DataSize.KB);
            if (byte_size < gb_size) return new FileSize(byte_size / mb_size, DataSize.MB);
            if (byte_size < tb_size) return new FileSize(byte_size / gb_size, DataSize.GB);

            return new FileSize(byte_size / tb_size, DataSize.TB);
        }
    }
}
