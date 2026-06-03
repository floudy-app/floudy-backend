using Floudy.API.Storage;
using Floudy.API.Storage.Entities;
using Floudy.API.Tests.Helpers;
using Xunit;

namespace Floudy.API.Tests.Storage;

[Collection("Sequential")]
public class FileRepositoryTests
{
    private const long TestUserId = 1;

    private static FileRepository CreateEmpty() => TestDataHelper.CreateEmptyRepository();

    private static RawFileEntity File1() => new() { ID = 1, Name = "alpha.txt", Type = "text/plain", UploadDate = DateTime.UtcNow, Content = [1, 2, 3], UserId = TestUserId };
    private static RawFileEntity File2() => new() { ID = 2, Name = "beta.txt", Type = "text/plain", UploadDate = DateTime.UtcNow, Content = [4, 5, 6], UserId = TestUserId };
    private static RawFileEntity File3() => new() { ID = 3, Name = "gamma.txt", Type = "text/plain", UploadDate = DateTime.UtcNow, Content = [7, 8, 9], UserId = TestUserId };

    [Fact]
    public void CountByUserId_EmptyRepository_ReturnsZero()
    {
        var repo = CreateEmpty();
        Assert.Equal(0, repo.CountByUserId(TestUserId));
    }

    [Fact]
    public void CountByUserId_AfterMultipleAdds_ReturnsCorrectCount()
    {
        var repo = CreateEmpty();

        repo.Add(File1());
        repo.Add(File2());
        repo.Add(File3());

        Assert.Equal(3, repo.CountByUserId(TestUserId));
    }

    [Fact]
    public void GetById_AfterAdd_ReturnsCorrectFile()
    {
        var repo = CreateEmpty();
        var file = File1();
        repo.Add(file);

        Assert.Equal(file.ID, repo.GetById(1)!.ID);
    }

    [Fact]
    public void BelongsToUser_AfterAdd_ReturnsTrue()
    {
        var repo = CreateEmpty();
        repo.Add(File1());

        Assert.True(repo.BelongsToUser(1, TestUserId));
    }

    [Fact]
    public void BelongsToUser_IdNeverAdded_ReturnsFalse()
    {
        var repo = CreateEmpty();
        Assert.False(repo.BelongsToUser(99, TestUserId));
    }

    [Fact]
    public void BelongsToUser_AfterRemove_ReturnsFalse()
    {
        var repo = CreateEmpty();
        repo.Add(File1());
        repo.Remove(1);

        Assert.False(repo.BelongsToUser(1, TestUserId));
    }

    [Fact]
    public void Remove_ExistingFile_ReturnsTrue()
    {
        var repo = CreateEmpty();
        repo.Add(File1());

        Assert.True(repo.Remove(1));
    }

    [Fact]
    public void Remove_NonExistentId_ReturnsFalse()
    {
        var repo = CreateEmpty();
        Assert.False(repo.Remove(999));
    }

    [Fact]
    public void Update_ExistingFile_MutatesNameInPlace()
    {
        var repo = CreateEmpty();
        repo.Add(File1());
        repo.Update(new RawFileEntity { ID = 1, Name = "renamed.txt" });

        Assert.Equal("renamed.txt", repo.GetById(1)!.Name);
    }

    [Fact]
    public void GetByUserId_EmptyRepository_ReturnsEmptySequence()
    {
        var repo = CreateEmpty();
        Assert.Empty(repo.GetByUserId(TestUserId));
    }

    [Fact]
    public void GetByUserId_WithFiles_ReturnsOnlyMatchingUserFiles()
    {
        var repo = CreateEmpty();
        var file1 = File1();
        var file2 = File2();

        repo.Add(file1);
        repo.Add(file2);

        var all = repo.GetByUserId(TestUserId).ToList();

        Assert.Equal(2, all.Count);
        Assert.Contains(all, x => x.ID == file1.ID);
        Assert.Contains(all, x => x.ID == file2.ID);
        Assert.All(all, file => Assert.Equal(TestUserId, file.UserId));
    }
}
