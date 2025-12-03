using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services;

public interface IMotorWindingTempService
{
    Task<MotorWindingTemp> AddDataAsync(AddMotorWindingTempRequest request);
    Task<MotorWindingTempQueryResult> QueryDataAsync(QueryMotorWindingTempRequest request);
    Task<MotorWindingTempDto?> GetLatestDataAsync(int siteId);
}
