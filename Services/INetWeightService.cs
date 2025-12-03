using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services;

public interface INetWeightService
{
    Task<NetWeight> AddDataAsync(AddNetWeightRequest request);
    Task<NetWeightQueryResult> QueryDataAsync(QueryNetWeightRequest request);
    Task<NetWeightDto?> GetLatestDataAsync(int siteId);
}
