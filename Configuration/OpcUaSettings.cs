namespace PumpRoomAutomationBackend.Configuration;

/// <summary>
/// OPC UA 配置设置
/// OPC UA Configuration Settings
/// </summary>
public class OpcUaSettings
{
    public const string SectionName = "OpcUaSettings";
    
    /// <summary>
    /// OPC UA 服务器地址
    /// </summary>
    public string Url { get; set; } = "opc.tcp://192.168.30.102:4840";
    
    /// <summary>
    /// 连接超时时间（毫秒）
    /// </summary>
    public int Timeout { get; set; } = 10000;
    
    /// <summary>
    /// 安全策略
    /// </summary>
    public string SecurityPolicy { get; set; } = "None";
    
    /// <summary>
    /// 安全模式
    /// </summary>
    public string SecurityMode { get; set; } = "None";
    
    /// <summary>
    /// 是否匿名连接
    /// </summary>
    public bool Anonymous { get; set; } = true;
    
    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// 会话超时时间（毫秒）
    /// </summary>
    public int SessionTimeout { get; set; } = 30000;
    
    /// <summary>
    /// 请求超时时间（毫秒）
    /// </summary>
    public int RequestTimeout { get; set; } = 10000;
    
    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 5;
    
    /// <summary>
    /// 重试延迟（毫秒）
    /// </summary>
    public int RetryDelay { get; set; } = 3000;
}

