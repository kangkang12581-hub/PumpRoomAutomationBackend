using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services;

public interface IExternalTempService
{
    Task<ExternalTemp> AddDataAsync(AddExternalTempRequest request);
    Task<ExternalTempQueryResult> QueryDataAsync(QueryExternalTempRequest request);
    Task<ExternalTempDto?> GetLatestDataAsync(int siteId);
}
