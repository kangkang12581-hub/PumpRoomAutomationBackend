using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services;

public interface IInternalHumidityService
{
    Task<InternalHumidity> AddDataAsync(AddInternalHumidityRequest request);
    Task<InternalHumidityQueryResult> QueryDataAsync(QueryInternalHumidityRequest request);
    Task<InternalHumidityDto?> GetLatestDataAsync(int siteId);
}

