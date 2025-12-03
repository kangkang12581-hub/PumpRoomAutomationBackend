using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PumpRoomAutomationBackend.Services;

/// <summary>
/// 摄像头快照结果
/// Camera snapshot result
/// </summary>
public class CameraSnapshotResult
{
    public string CameraType { get; set; } = string.Empty;
    public string CameraIp { get; set; } = string.Empty;
    public bool Success { get; set; }
    public byte[]? ImageData { get; set; }
    public int SizeBytes { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// 站点快照结果
/// Site snapshot result
/// </summary>
public class SiteSnapshotResult
{
    public int SiteId { get; set; }
    public DateTime Timestamp { get; set; }
    public List<CameraSnapshotResult> Snapshots { get; set; } = new();
    public int TotalCameras { get; set; }
    public int SuccessfulSnapshots { get; set; }
    public int FailedSnapshots { get; set; }
}

/// <summary>
/// 摄像头服务接口
/// Camera Service Interface
/// </summary>
public interface ICameraService
{
    /// <summary>
    /// 从指定摄像头获取截图
    /// Get snapshot from specific camera
    /// </summary>
    /// <param name="cameraIp">摄像头IP地址</param>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <returns>图片字节数组，失败返回null</returns>
    Task<byte[]?> GetCameraSnapshotAsync(string cameraIp, string? username = null, string? password = null);
    
    /// <summary>
    /// 获取站点所有摄像头的截图
    /// Get snapshots from all cameras for a specific site
    /// </summary>
    /// <param name="siteId">站点ID</param>
    /// <param name="internalCameraIp">机内摄像头IP</param>
    /// <param name="internalCameraUsername">机内摄像头用户名</param>
    /// <param name="internalCameraPassword">机内摄像头密码</param>
    /// <param name="globalCameraIp">全局摄像头IP</param>
    /// <param name="globalCameraUsername">全局摄像头用户名</param>
    /// <param name="globalCameraPassword">全局摄像头密码</param>
    /// <returns>站点快照结果</returns>
    Task<SiteSnapshotResult> GetSiteSnapshotsAsync(
        int siteId,
        string? internalCameraIp = null,
        string? internalCameraUsername = null,
        string? internalCameraPassword = null,
        string? globalCameraIp = null,
        string? globalCameraUsername = null,
        string? globalCameraPassword = null);
}

/// <summary>
/// 摄像头服务实现
/// Camera Service Implementation
/// </summary>
public class CameraService : ICameraService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CameraService> _logger;
    private readonly string _hikVisionServiceUrl;

    public CameraService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<CameraService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        
        // HikVision 服务地址
        _hikVisionServiceUrl = _configuration["HikVision:ServiceUrl"] ?? "http://192.168.10.96:5500";
    }

    public async Task<byte[]?> GetCameraSnapshotAsync(string cameraIp, string? username = null, string? password = null)
    {
        try
        {
            if (string.IsNullOrEmpty(cameraIp))
            {
                _logger.LogWarning("⚠️ 摄像头IP地址为空");
                return null;
            }

            _logger.LogInformation("📷 开始获取摄像头截图: IP={CameraIp}", cameraIp);

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30); // 增加超时时间到30秒

            // 策略：直接尝试从海康威视摄像头获取JPEG截图
            // 海康威视标准HTTP API: http://ip/ISAPI/Streaming/channels/101/picture
            var directSnapshotUrl = $"http://{cameraIp}/ISAPI/Streaming/channels/101/picture";
            
            _logger.LogDebug("📷 尝试直接从摄像头获取截图: {Url}", directSnapshotUrl);
            
            // 设置基本认证
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
            }
            
            try
            {
                var directResponse = await httpClient.GetAsync(directSnapshotUrl);
                
                if (directResponse.IsSuccessStatusCode)
                {
                    var imageData = await directResponse.Content.ReadAsByteArrayAsync();
                    _logger.LogInformation("✅ 直接从摄像头获取截图成功: IP={CameraIp}, 大小={Size} bytes", 
                        cameraIp, imageData.Length);
                    return imageData;
                }
                else
                {
                    _logger.LogDebug("⚠️ 直接获取截图失败: {StatusCode}，尝试通过HikVision服务", 
                        directResponse.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("直接获取截图失败: {Message}，尝试通过HikVision服务", ex.Message);
            }

            // 备用方案1：通过 HikVision 流服务获取快照（使用已有连接）
            _logger.LogDebug("📷 尝试方案1: 配置摄像头并从流中获取快照");
            
            // 重置认证头
            httpClient.DefaultRequestHeaders.Authorization = null;
            
            try
            {
                // 步骤1: 配置摄像头
                var configUrl = $"{_hikVisionServiceUrl}/api/stream/config";
                var configData = new
                {
                    ip = cameraIp,
                    username = username ?? "admin",
                    password = password ?? "admin123"
                };
                
                var configJson = JsonSerializer.Serialize(configData);
                var configContent = new StringContent(configJson, Encoding.UTF8, "application/json");
                
                _logger.LogDebug("📷 配置摄像头: {CameraIp}", cameraIp);
                var configResponse = await httpClient.PostAsync(configUrl, configContent);
                
                if (configResponse.IsSuccessStatusCode)
                {
                    _logger.LogDebug("✅ 摄像头配置成功");
                    
                    // 步骤2: 启动流
                    var startUrl = $"{_hikVisionServiceUrl}/api/stream/start";
                    _logger.LogDebug("📷 启动视频流");
                    var startResponse = await httpClient.PostAsync(startUrl, null);
                    
                    if (startResponse.IsSuccessStatusCode)
                    {
                        // 等待流启动并有帧数据（2秒）
                        await Task.Delay(2000);
                        
                        // 步骤3: 获取快照
                        var snapshotUrl = $"{_hikVisionServiceUrl}/api/stream/snapshot";
                        _logger.LogDebug("📷 获取快照");
                        
                        var snapshotResponse = await httpClient.GetAsync(snapshotUrl);
                        
                        if (snapshotResponse.IsSuccessStatusCode)
                        {
                            var imageData = await snapshotResponse.Content.ReadAsByteArrayAsync();
                            _logger.LogInformation("✅ 通过HikVision流服务获取截图成功: IP={CameraIp}, 大小={Size} bytes", 
                                cameraIp, imageData.Length);
                            return imageData;
                        }
                        else
                        {
                            _logger.LogDebug("⚠️ 获取快照失败: {StatusCode}", snapshotResponse.StatusCode);
                        }
                    }
                    else
                    {
                        _logger.LogDebug("⚠️ 启动流失败: {StatusCode}", startResponse.StatusCode);
                    }
                }
                else
                {
                    _logger.LogDebug("⚠️ 配置摄像头失败: {StatusCode}", configResponse.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("方案1失败: {Message}，尝试方案2", ex.Message);
            }
            
            // 备用方案2：尝试直接截图API（可能很慢）
            _logger.LogDebug("📷 尝试方案2: 直接截图API");
            
            var hikVisionSnapshotUrl = $"{_hikVisionServiceUrl}/api/snapshot/direct";
            var snapshotRequestData = new
            {
                ip = cameraIp,
                username = username ?? "admin",
                password = password ?? "admin123",
                channel = 1
            };

            var snapshotJson = JsonSerializer.Serialize(snapshotRequestData);
            var snapshotContent = new StringContent(snapshotJson, Encoding.UTF8, "application/json");
            
            var snapshotResponse2 = await httpClient.PostAsync(hikVisionSnapshotUrl, snapshotContent);
            
            if (snapshotResponse2.IsSuccessStatusCode)
            {
                var imageData = await snapshotResponse2.Content.ReadAsByteArrayAsync();
                _logger.LogInformation("✅ 通过HikVision直接截图获取成功: IP={CameraIp}, 大小={Size} bytes", 
                    cameraIp, imageData.Length);
                return imageData;
            }
            else
            {
                var errorContent = await snapshotResponse2.Content.ReadAsStringAsync();
                _logger.LogWarning("⚠️ 通过HikVision服务获取截图失败: IP={CameraIp}, 状态码={StatusCode}, 错误={Error}", 
                    cameraIp, snapshotResponse2.StatusCode, errorContent);
                return null;
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("❌ 获取摄像头截图超时: IP={CameraIp}, 建议检查摄像头网络连接", cameraIp);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取摄像头截图异常: IP={CameraIp}", cameraIp);
            return null;
        }
    }

    public async Task<SiteSnapshotResult> GetSiteSnapshotsAsync(
        int siteId,
        string? internalCameraIp = null,
        string? internalCameraUsername = null,
        string? internalCameraPassword = null,
        string? globalCameraIp = null,
        string? globalCameraUsername = null,
        string? globalCameraPassword = null)
    {
        var result = new SiteSnapshotResult
        {
            SiteId = siteId,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("📷 开始获取站点 {SiteId} 的所有摄像头截图", siteId);

            // 构建摄像头列表
            var cameras = new List<object>();

            if (!string.IsNullOrEmpty(internalCameraIp))
            {
                cameras.Add(new
                {
                    camera_type = "internal",
                    ip = internalCameraIp,
                    username = internalCameraUsername ?? "admin",
                    password = internalCameraPassword ?? "",
                    channel = 1
                });
            }

            if (!string.IsNullOrEmpty(globalCameraIp))
            {
                cameras.Add(new
                {
                    camera_type = "global",
                    ip = globalCameraIp,
                    username = globalCameraUsername ?? "admin",
                    password = globalCameraPassword ?? "",
                    channel = 1
                });
            }

            if (cameras.Count == 0)
            {
                _logger.LogWarning("⚠️ 站点 {SiteId} 没有配置摄像头", siteId);
                return result;
            }

            // 调用 HikVision 服务的站点截图接口
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(60); // 增加超时时间

            var requestData = new
            {
                site_id = siteId,
                cameras = cameras
            };

            var requestJson = JsonSerializer.Serialize(requestData);
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var siteSnapshotUrl = $"{_hikVisionServiceUrl}/api/snapshot/site";
            _logger.LogDebug("📷 调用站点截图接口: {Url}", siteSnapshotUrl);

            var response = await httpClient.PostAsync(siteSnapshotUrl, requestContent);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);

                result.TotalCameras = responseData.GetProperty("total_cameras").GetInt32();
                result.SuccessfulSnapshots = responseData.GetProperty("successful_snapshots").GetInt32();
                result.FailedSnapshots = responseData.GetProperty("failed_snapshots").GetInt32();

                // 解析快照数据
                var snapshots = responseData.GetProperty("snapshots");
                foreach (var snapshot in snapshots.EnumerateArray())
                {
                    var snapshotResult = new CameraSnapshotResult
                    {
                        CameraType = snapshot.GetProperty("camera_type").GetString() ?? "",
                        CameraIp = snapshot.GetProperty("camera_ip").GetString() ?? "",
                        Success = snapshot.GetProperty("success").GetBoolean(),
                        SizeBytes = snapshot.GetProperty("size_bytes").GetInt32()
                    };

                    // 获取 base64 编码的图片数据
                    if (snapshot.TryGetProperty("image_data", out var imageDataElement) && 
                        imageDataElement.ValueKind == JsonValueKind.String)
                    {
                        var base64Data = imageDataElement.GetString();
                        if (!string.IsNullOrEmpty(base64Data))
                        {
                            snapshotResult.ImageData = Convert.FromBase64String(base64Data);
                        }
                    }

                    // 获取错误信息
                    if (snapshot.TryGetProperty("error", out var errorElement) && 
                        errorElement.ValueKind == JsonValueKind.String)
                    {
                        snapshotResult.Error = errorElement.GetString();
                    }

                    result.Snapshots.Add(snapshotResult);
                }

                _logger.LogInformation("✅ 站点 {SiteId} 截图完成: 成功 {Success}/{Total}", 
                    siteId, result.SuccessfulSnapshots, result.TotalCameras);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("❌ 站点截图接口调用失败: {StatusCode}, 错误: {Error}", 
                    response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取站点 {SiteId} 截图异常", siteId);
        }

        return result;
    }
}

