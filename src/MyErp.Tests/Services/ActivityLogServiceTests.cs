using Moq;
using MyErp.Application.Abstractions;
using MyErp.Application.Services;
using MyErp.Domain.Entities;
using Xunit;

namespace MyErp.Tests.Services;

public class ActivityLogServiceTests
{
    private readonly Mock<IActivityLogRepository> _activityLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ActivityLogService _sut;

    public ActivityLogServiceTests()
    {
        _sut = new ActivityLogService(_activityLogRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task LogAsync_應該寫入一筆操作紀錄並立刻SaveChanges()
    {
        ActivityLog? added = null;
        _activityLogRepository.Setup(r => r.Add(It.IsAny<ActivityLog>())).Callback<ActivityLog>(l => added = l);

        await _sut.LogAsync("DELETE /api/categories/5", "alice");

        Assert.NotNull(added);
        Assert.Equal("DELETE /api/categories/5", added!.Api);
        Assert.Equal("alice", added.CreatedBy);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_應該把查詢條件轉成DTO清單()
    {
        var logs = new List<ActivityLog>
        {
            new() { Id = 1, Api = "POST /api/categories", CreatedBy = "alice", CreatedAt = DateTime.UtcNow },
        };
        _activityLogRepository
            .Setup(r => r.SearchAsync("alice", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        var result = await _sut.SearchAsync("alice", null, null);

        Assert.Single(result);
        Assert.Equal("POST /api/categories", result[0].Api);
        Assert.Equal("alice", result[0].CreatedBy);
    }
}
