namespace Floudy.API.Utility
{
    public readonly struct FileSize(double value, DataSize unit)
    {
        public double Value { get; } = value;

        public DataSize Unit { get; } = unit;

        public override string ToString() => $"{Value:0.##}{Unit}";
    }

    public enum DataSize
    {
        B,
        Bytes = B,

        KB,
        KiloBytes = KB,

        MB,
        MegaBytes = MB,

        GB,
        GigaBytes = GB,

        TB,
        TeraBytes = TB,
    }
}
