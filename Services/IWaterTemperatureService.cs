using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Models.Entities;
namespace PumpRoomAutomationBackend.Services;
public interface IWaterTemperatureService
{
    Task<WaterTemperature> AddDataAsync(AddWaterTemperatureRequest request);
    Task<WaterTemperatureQueryResult> QueryDataAsync(QueryWaterTemperatureRequest request);
    Task<WaterTemperatureDto?> GetLatestDataAsync(int siteId);
}
