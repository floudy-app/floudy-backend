using Floudy.API.Models;
using Floudy.API.Services;
using Floudy.API.Storage;
using Floudy.API.Tests.Helpers;
using Xunit;

namespace Floudy.API.Tests.Services;

[Collection("Sequential")]
public class FileServiceTests : IDisposable
{
    private const long TestUserId = 1;
    private readonly FileService service;

    public FileServiceTests()
    {
        GlobalIdManagerHelper.Reset();
        service = new FileService(TestDataHelper.CreateEmptyRepository());
    }

    public void Dispose() => GlobalIdManagerHelper.Reset();

    [Fact]
    public void VerifyMetadata_Dictionary_ValidMetadata_ReturnsTrue() => Assert.True(service.VerifyMetadata(TestDataHelper.ValidMetadata()));

    [Fact]
    public void VerifyMetadata_Dictionary_NullArgument_ReturnsFalse() => Assert.False(service.VerifyMetadata((Dictionary<string, object>?) null));

    [Fact]
    public void VerifyMetadata_Dictionary_MissingRawKey_ReturnsFalse()
    {
        var metadata = TestDataHelper.ValidMetadata();
        metadata.Remove("raw");

        Assert.False(service.VerifyMetadata(metadata));
    }

    [Fact]
    public void VerifyMetadata_Dictionary_RawValueIsNotByteArray_ReturnsFalse()
    {
        var metadata = TestDataHelper.ValidMetadata();
        metadata["raw"] = "not bytes";

        Assert.False(service.VerifyMetadata(metadata));
    }

    [Fact]
    public void VerifyMetadata_Dictionary_SizeIsNotJsonElement_ReturnsFalse()
    {
        var metadata = TestDataHelper.ValidMetadata();
        metadata["size"] = 42;

        Assert.False(service.VerifyMetadata(metadata));
    }

    [Fact]
    public void VerifyMetadata_Dictionary_NegativeSize_ReturnsFalse() => Assert.False(service.VerifyMetadata(TestDataHelper.ValidMetadata(size: -1)));

    [Fact]
    public void VerifyMetadata_Dictionary_ZeroSizeIsAccepted_ReturnsTrue() => Assert.True(service.VerifyMetadata(TestDataHelper.ValidMetadata(size: 0)));

    [Fact]
    public void VerifyMetadata_Dictionary_ZeroUploadedTimestamp_ReturnsFalse() => Assert.False(service.VerifyMetadata(TestDataHelper.ValidMetadata(uploaded_ms: 0)));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void VerifyMetadata_Dictionary_EmptyOrWhitespaceName_ReturnsFalse(string name) => Assert.False(service.VerifyMetadata(TestDataHelper.ValidMetadata(name: name)));

    [Fact]
    public void VerifyMetadata_RawFileMetadata_ValidName_ReturnsTrue() => Assert.True(service.VerifyMetadata(new RawFileMetadata("document.txt")));

    [Fact]
    public void VerifyMetadata_RawFileMetadata_EmptyName_ReturnsFalse() => Assert.False(service.VerifyMetadata(new RawFileMetadata("")));

    [Fact]
    public void UploadFromMetadata_ValidMetadata_ReturnsTrue() => Assert.True(service.UploadFromMetadata(TestDataHelper.ValidMetadata(), TestUserId));

    [Fact]
    public void UploadFromMetadata_ValidMetadata_IncreasesFileCount()
    {
        service.UploadFromMetadata(TestDataHelper.ValidMetadata(), TestUserId);
        Assert.Equal(1, service.Files.CountByUserId(TestUserId));
    }

    [Fact]
    public void UploadFromMetadata_ValidMetadata_PersistsCorrectName()
    {
        service.UploadFromMetadata(TestDataHelper.ValidMetadata(name: "my_photo.png"), TestUserId);
        Assert.Equal("my_photo.png", service.Files.GetByUserId(TestUserId).Single().Name);
    }

    [Fact]
    public void UploadFromMetadata_InvalidMetadata_ReturnsFalse() => Assert.False(service.UploadFromMetadata(null, TestUserId));

    [Fact]
    public void ModifyMetadata_ValidIdAndName_ReturnsTrue()
    {
        service.UploadFromMetadata(TestDataHelper.ValidMetadata(), TestUserId);
        var file_id = service.Files.GetByUserId(TestUserId).Single().ID;

        var result = service.ModifyMetadata(file_id, new RawFileMetadata("new_name.txt"), TestUserId);
        Assert.True(result);
    }

    [Fact]
    public void ModifyMetadata_ValidIdAndName_ChangesFileName()
    {
        service.UploadFromMetadata(TestDataHelper.ValidMetadata(name: "original.txt"), TestUserId);
        var file_id = service.Files.GetByUserId(TestUserId).Single().ID;
        service.ModifyMetadata(file_id, new RawFileMetadata("updated.txt"), TestUserId);

        Assert.Equal("updated.txt", service.Files.GetByUserId(TestUserId).Single().Name);
    }

    [Fact]
    public void ModifyMetadata_NonExistentId_ReturnsFalse() => Assert.False(service.ModifyMetadata(9999, new RawFileMetadata("name.txt"), TestUserId));

    [Fact]
    public void ModifyMetadata_EmptyNewName_ReturnsFalse()
    {
        service.UploadFromMetadata(TestDataHelper.ValidMetadata(), TestUserId);
        var file_id = service.Files.GetByUserId(TestUserId).Single().ID;

        Assert.False(service.ModifyMetadata(file_id, new RawFileMetadata(""), TestUserId));
    }

    [Fact]
    public void DeleteFile_ExistingFile_ReturnsTrue()
    {
        service.UploadFromMetadata(TestDataHelper.ValidMetadata(), TestUserId);
        var file_id = service.Files.GetByUserId(TestUserId).Single().ID;

        Assert.True(service.DeleteFile(file_id, TestUserId));
    }

    [Fact]
    public void DeleteFile_ExistingFile_RemovesItFromRepository()
    {
        service.UploadFromMetadata(TestDataHelper.ValidMetadata(), TestUserId);
        var file_id = service.Files.GetByUserId(TestUserId).Single().ID;
        service.DeleteFile(file_id, TestUserId);

        Assert.Equal(0, service.Files.CountByUserId(TestUserId));
    }

    [Fact]
    public void DeleteFile_ExistingFile_UnregistersIdForReuse()
    {
        service.UploadFromMetadata(TestDataHelper.ValidMetadata(), TestUserId);
        var file_id = service.Files.GetByUserId(TestUserId).Single().ID;
        service.DeleteFile(file_id, TestUserId);

        service.UploadFromMetadata(TestDataHelper.ValidMetadata(name: "second.txt"), TestUserId);

        Assert.Equal(file_id, service.Files.GetByUserId(TestUserId).Single().ID);
    }

    [Fact]
    public void DeleteFile_NonExistentId_ReturnsFalse() => Assert.False(service.DeleteFile(9999, TestUserId));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void GetPage_NonPositiveIndex_ReturnsEmptySequence(int index)
    {
        service.UploadFromMetadata(TestDataHelper.ValidMetadata(), TestUserId);
        Assert.Empty(service.GetPage(index, TestUserId));
    }

    [Fact]
    public void GetPage_PageOneWithFewerThan10Files_ReturnsAllFiles()
    {
        for (var i = 0; i < 5; i++) service.UploadFromMetadata(TestDataHelper.ValidMetadata(name: $"file{i}.txt"), TestUserId);
        Assert.Equal(5, service.GetPage(1, TestUserId).ToList().Count);
    }

    [Fact]
    public void GetPage_PageTwoWith11Files_ReturnsOneFile()
    {
        for (var i = 0; i < 11; i++) service.UploadFromMetadata(TestDataHelper.ValidMetadata(name: $"file{i}.txt"), TestUserId);
        Assert.Single(service.GetPage(2, TestUserId).ToList());
    }

    [Fact]
    public void GetPage_PageBeyondLastPage_ReturnsEmptySequence()
    {
        service.UploadFromMetadata(TestDataHelper.ValidMetadata(), TestUserId);
        Assert.Empty(service.GetPage(999, TestUserId).ToList());
    }

    [Fact]
    public void TotalPageCount_EmptyRepository_ReturnsOne() => Assert.Equal(1, service.TotalPageCount(TestUserId));

    [Fact]
    public void TotalPageCount_ExactlyTenFiles_ReturnsOne()
    {
        for (var i = 0; i < 10; i++) service.UploadFromMetadata(TestDataHelper.ValidMetadata(name: $"f{i}.txt"), TestUserId);
        Assert.Equal(1, service.TotalPageCount(TestUserId));
    }

    [Fact]
    public void TotalPageCount_ElevenFiles_ReturnsTwo()
    {
        for (var i = 0; i < 11; i++) service.UploadFromMetadata(TestDataHelper.ValidMetadata(name: $"f{i}.txt"), TestUserId);
        Assert.Equal(2, service.TotalPageCount(TestUserId));
    }

    [Fact]
    public void TypeStatistics_EmptyRepository_ReturnsEmptyDictionary() => Assert.Empty(service.TypeStatistics(TestUserId));

    [Fact]
    public void TypeStatistics_OneType_ReturnsSingleEntry()
    {
        service.UploadFromMetadata(TestDataHelper.ValidMetadata(type: "text/plain"), TestUserId);
        service.UploadFromMetadata(TestDataHelper.ValidMetadata(name: "b.txt", type: "text/html"), TestUserId);

        var result = service.TypeStatistics(TestUserId);

        Assert.Single(result);
        Assert.Equal(2, result["text"]);
    }

    [Fact]
    public void TypeStatistics_MultipleTypes_ReturnsAllGroups()
    {
        service.UploadFromMetadata(TestDataHelper.ValidMetadata(type: "text/plain"), TestUserId);
        service.UploadFromMetadata(TestDataHelper.ValidMetadata(name: "img.png", type: "image/png"), TestUserId);
        service.UploadFromMetadata(TestDataHelper.ValidMetadata(name: "img2.jpg", type: "image/jpeg"), TestUserId);

        var result = service.TypeStatistics(TestUserId);

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result["text"]);
        Assert.Equal(2, result["image"]);
    }

    [Fact]
    public void UploadStatistics_EmptyRepository_ReturnsEmptyDictionary() => Assert.Empty(service.UploadStatistics(TestUserId));

    [Fact]
    public void UploadStatistics_MultipleFilesInSameMonth_ReturnsSingleEntry()
    {
        var jan = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        service.UploadFromMetadata(TestDataHelper.ValidMetadata(uploaded_ms: new DateTimeOffset(jan).ToUnixTimeMilliseconds()), TestUserId);
        service.UploadFromMetadata(TestDataHelper.ValidMetadata(name: "b.txt", uploaded_ms: new DateTimeOffset(jan.AddDays(5)).ToUnixTimeMilliseconds()), TestUserId);

        var result = service.UploadStatistics(TestUserId);

        Assert.Single(result);
        Assert.Equal(2, result[new DateTime(2024, 1, 1)]);
    }

    [Fact]
    public void UploadStatistics_FilesInDifferentMonths_ReturnsMultipleEntries()
    {
        var jan = new DateTimeOffset(new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc));
        var feb = new DateTimeOffset(new DateTime(2024, 2, 10, 0, 0, 0, DateTimeKind.Utc));

        service.UploadFromMetadata(TestDataHelper.ValidMetadata(uploaded_ms: jan.ToUnixTimeMilliseconds()), TestUserId);
        service.UploadFromMetadata(TestDataHelper.ValidMetadata(name: "b.txt", uploaded_ms: feb.ToUnixTimeMilliseconds()), TestUserId);

        Assert.Equal(2, service.UploadStatistics(TestUserId).Count);
    }

    [Fact]
    public void UploadStatistics_IsOrderedByDateAscending()
    {
        var mar = new DateTimeOffset(new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        var jan = new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        service.UploadFromMetadata(TestDataHelper.ValidMetadata(uploaded_ms: mar.ToUnixTimeMilliseconds()), TestUserId);
        service.UploadFromMetadata(TestDataHelper.ValidMetadata(name: "b.txt", uploaded_ms: jan.ToUnixTimeMilliseconds()), TestUserId);

        var keys = service.UploadStatistics(TestUserId).Keys.ToList();
        Assert.True(keys[0] < keys[1]);
    }

    [Fact]
    public void UploadStatistics_DateKeyIsNormalisedToFirstOfMonth()
    {
        var mid_month = new DateTimeOffset(new DateTime(2024, 5, 17, 0, 0, 0, DateTimeKind.Utc));
        service.UploadFromMetadata(TestDataHelper.ValidMetadata(uploaded_ms: mid_month.ToUnixTimeMilliseconds()), TestUserId);

        var key = service.UploadStatistics(TestUserId).Keys.Single();

        Assert.Equal(1, key.Day);
        Assert.Equal(5, key.Month);
        Assert.Equal(2024, key.Year);
    }
}