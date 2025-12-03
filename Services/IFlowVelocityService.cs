using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services;

public interface IFlowVelocityService
{
    Task<FlowVelocity> AddDataAsync(AddFlowVelocityRequest request);
    Task<FlowVelocityQueryResult> QueryDataAsync(QueryFlowVelocityRequest request);
    Task<FlowVelocityDto?> GetLatestDataAsync(int siteId);
}

