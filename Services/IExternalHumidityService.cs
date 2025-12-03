using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services;

public interface IExternalHumidityService
{
    Task<ExternalHumidity> AddDataAsync(AddExternalHumidityRequest request);
    Task<ExternalHumidityQueryResult> QueryDataAsync(QueryExternalHumidityRequest request);
    Task<ExternalHumidityDto?> GetLatestDataAsync(int siteId);
}
