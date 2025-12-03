using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services;

public interface ISpeedService
{
    Task<Speed> AddDataAsync(AddSpeedRequest request);
    Task<SpeedQueryResult> QueryDataAsync(QuerySpeedRequest request);
    Task<SpeedDto?> GetLatestDataAsync(int siteId);
    Task<SpeedStatisticsDto> GetStatisticsAsync(int siteId, DateTime startTime, DateTime endTime);
}

