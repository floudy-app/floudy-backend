using Floudy.API.Utility;
using Xunit;

namespace Floudy.API.Tests.Utility;

[Collection("Sequential")]
public class DataSizeConverterTests
{
    [Fact]
    public void ConvertToOptimalSize_ZeroBytes_ReturnsBytesUnit()
    {
        var result = DataSizeConverter.ConvertToOptimalSize(0);

        Assert.Equal(DataSize.B, result.Unit);
        Assert.Equal(0d, result.Value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(512)]
    [InlineData(1023)]
    public void ConvertToOptimalSize_BelowOneKilobyte_ReturnsBytesUnit(long byte_size)
    {
        var result = DataSizeConverter.ConvertToOptimalSize(byte_size);

        Assert.Equal(DataSize.B, result.Unit);
        Assert.Equal((double)byte_size, result.Value);
    }

    [Fact]
    public void ConvertToOptimalSize_ExactlyOneKilobyte_ReturnsKilobytesUnit()
    {
        var result = DataSizeConverter.ConvertToOptimalSize(1024);

        Assert.Equal(DataSize.KB, result.Unit);
        Assert.Equal(1d, result.Value);
    }

    [Fact]
    public void ConvertToOptimalSize_TwoKilobytes_ReturnsValueTwo()
    {
        var result = DataSizeConverter.ConvertToOptimalSize(2048);

        Assert.Equal(DataSize.KB, result.Unit);
        Assert.Equal(2d, result.Value);
    }

    [Fact]
    public void ConvertToOptimalSize_ExactlyOneMegabyte_ReturnsMegabytesUnit()
    {
        var result = DataSizeConverter.ConvertToOptimalSize(1024 * 1024);

        Assert.Equal(DataSize.MB, result.Unit);
        Assert.Equal(1d, result.Value);
    }

    [Fact]
    public void ConvertToOptimalSize_ExactlyOneGigabyte_ReturnsGigabytesUnit()
    {
        var result = DataSizeConverter.ConvertToOptimalSize(1024 * 1024 * 1024);

        Assert.Equal(DataSize.GB, result.Unit);
        Assert.Equal(1d, result.Value);
    }

    [Fact]
    public void ConvertToOptimalSize_ExactlyOneTerabyte_ReturnsTerabytesUnit()
    {
        var one_tb = 1024L * 1024 * 1024 * 1024;
        var result = DataSizeConverter.ConvertToOptimalSize(one_tb);

        Assert.Equal(DataSize.TB, result.Unit);
        Assert.Equal(1d, result.Value);
    }
}

[Collection("Sequential")]
public class FileSizeTests
{
    [Fact]
    public void Constructor_SetsValueAndUnit()
    {
        var file_size = new FileSize(3.14, DataSize.MB);

        Assert.Equal(3.14, file_size.Value);
        Assert.Equal(DataSize.MB, file_size.Unit);
    }

    [Theory]
    [InlineData(1d,    DataSize.B,  "1B")]
    [InlineData(1d,    DataSize.KB, "1KB")]
    [InlineData(1d,    DataSize.MB, "1MB")]
    [InlineData(1d,    DataSize.GB, "1GB")]
    [InlineData(1d,    DataSize.TB, "1TB")]
    [InlineData(1.5d,  DataSize.KB, "1.5KB")]
    [InlineData(1.25d, DataSize.MB, "1.25MB")]
    [InlineData(2.0d,  DataSize.GB, "2GB")]
    public void ToString_FormatsValueAndUnit(double value, DataSize unit, string expected)
    {
        var file_size = new FileSize(value, unit);
        Assert.Equal(expected, file_size.ToString());
    }
}
