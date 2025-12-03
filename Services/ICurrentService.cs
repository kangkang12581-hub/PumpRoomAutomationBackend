using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services;

public interface ICurrentService
{
    Task<Current> AddDataAsync(AddCurrentRequest request);
    Task<CurrentQueryResult> QueryDataAsync(QueryCurrentRequest request);
    Task<CurrentDto?> GetLatestDataAsync(int siteId);
}
