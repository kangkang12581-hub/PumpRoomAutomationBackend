using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services;

public interface IInternalTempService
{
    Task<InternalTemp> AddDataAsync(AddInternalTempRequest request);
    Task<InternalTempQueryResult> QueryDataAsync(QueryInternalTempRequest request);
    Task<InternalTempDto?> GetLatestDataAsync(int siteId);
}
