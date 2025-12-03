using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PumpRoomAutomationBackend.Services;

/// <summary>
/// 短信服务接口
/// SMS Service Interface
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// 发送短信
    /// Send SMS
    /// </summary>
    /// <param name="mobiles">手机号码列表（逗号分隔）</param>
    /// <param name="content">短信内容</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendSmsAsync(string mobiles, string content);

    /// <summary>
    /// 发送语音
    /// Send Voice
    /// </summary>
    /// <param name="mobiles">手机号码列表（逗号分隔，最多30个）</param>
    /// <param name="content">语音内容</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendVoiceAsync(string mobiles, string content);
}

/// <summary>
/// 短信服务实现
/// SMS Service Implementation
/// </summary>
public class SmsService : ISmsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmsService> _logger;
    private readonly string _serverUrl;
    private readonly string _appId;
    private readonly string _appSecret;

    public SmsService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<SmsService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;

        // 从配置读取短信平台参数
        var smsConfig = _configuration.GetSection("SmsPlatform");
        _serverUrl = smsConfig["ServerUrl"] ?? "";
        _appId = smsConfig["AppId"] ?? "";
        _appSecret = smsConfig["AppSecret"] ?? "";

        if (string.IsNullOrEmpty(_serverUrl) || string.IsNullOrEmpty(_appId) || string.IsNullOrEmpty(_appSecret))
        {
            _logger.LogWarning("⚠️ 短信平台配置不完整，短信功能将不可用");
        }
    }

    /// <summary>
    /// 生成签名
    /// Generate signature
    /// </summary>
    /// <summary>
    /// 生成签名
    /// 签名规则：Base64(app_id + MD5(timestamp) + MD5(app_secret + mobiles + URLEncode(content, "UTF-8")))
    /// </summary>
    /// <param name="timestamp">请求时间 yyyyMMddHHmmss</param>
    /// <param name="mobiles">手机号码数组，逗号分割</param>
    /// <param name="urlEncodedContent">URL编码后的短信内容（已编码）</param>
    private string GenerateSign(string timestamp, string mobiles, string urlEncodedContent)
    {
        try
        {
            // 步骤1: MD5(timestamp)
            string md5Timestamp = ComputeMd5(timestamp);
            _logger.LogDebug("🔐 [签名生成] 步骤1 - MD5(timestamp): timestamp={Timestamp}, MD5={Md5Timestamp}", timestamp, md5Timestamp);

            // 步骤2: MD5(app_secret + mobiles + URLEncode(content, "UTF-8"))
            // 注意：urlEncodedContent 已经是 URL 编码后的内容
            string secretString = _appSecret + mobiles + urlEncodedContent;
            string md5Secret = ComputeMd5(secretString);
            _logger.LogDebug("🔐 [签名生成] 步骤2 - MD5(app_secret + mobiles + URLEncode(content)): 原始字符串长度={Length}, MD5={Md5Secret}", secretString.Length, md5Secret);

            // 步骤3: Base64(app_id + MD5(timestamp) + MD5(app_secret + mobiles + URLEncode(content, "UTF-8")))
            string signString = _appId + md5Timestamp + md5Secret;
            byte[] signBytes = Encoding.UTF8.GetBytes(signString);
            string sign = Convert.ToBase64String(signBytes);
            _logger.LogDebug("🔐 [签名生成] 步骤3 - Base64编码: 签名字符串长度={Length}, 最终签名={Sign}", signString.Length, sign);

            return sign;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 生成签名失败");
            throw;
        }
    }

    /// <summary>
    /// 计算MD5哈希值
    /// Compute MD5 hash
    /// </summary>
    private string ComputeMd5(string input)
    {
        using (var md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }

    public async Task<bool> SendSmsAsync(string mobiles, string content)
    {
        if (string.IsNullOrEmpty(_serverUrl) || string.IsNullOrEmpty(_appId) || string.IsNullOrEmpty(_appSecret))
        {
            _logger.LogWarning("⚠️ 短信平台配置不完整，跳过发送短信");
            return false;
        }

        try
        {
            _logger.LogInformation("📱 开始发送短信: 号码={Mobiles}, 内容长度={Length}", mobiles, content.Length);

            // 生成时间戳 yyyyMMddHHmmss
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            // URLEncode(content, "UTF-8")
            // 在 .NET Core 中，使用 Uri.EscapeDataString 替代 HttpUtility.UrlEncode
            string urlEncodedContent = Uri.EscapeDataString(content);

            // 生成签名
            string sign = GenerateSign(timestamp, mobiles, urlEncodedContent);

            // 构建请求数据
            var requestData = new
            {
                appid = _appId,
                timestamp = timestamp,
                mobiles = mobiles,
                content = urlEncodedContent,
                sign = sign
            };

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var requestJson = JsonSerializer.Serialize(requestData, new JsonSerializerOptions { WriteIndented = true });
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            string sendUrl = $"{_serverUrl.TrimEnd('/')}/sdk/send";
            
            // 打印请求参数（入参）- 类似OPC UA日志格式
            _logger.LogInformation("📖 开始发送短信: URL = {Url}", sendUrl);
            _logger.LogInformation("📖 开始发送短信: appid = {AppId}", _appId);
            _logger.LogInformation("📖 开始发送短信: timestamp = {Timestamp}", timestamp);
            _logger.LogInformation("📖 开始发送短信: mobiles = {Mobiles}", mobiles);
            _logger.LogInformation("📖 开始发送短信: content = {Content}", urlEncodedContent);
            _logger.LogInformation("📖 开始发送短信: sign = {Sign}", sign);
            
            Console.WriteLine($"📖 开始发送短信: URL = {sendUrl}");
            Console.WriteLine($"📖 开始发送短信: appid = {_appId}");
            Console.WriteLine($"📖 开始发送短信: timestamp = {timestamp}");
            Console.WriteLine($"📖 开始发送短信: mobiles = {mobiles}");
            Console.WriteLine($"📖 开始发送短信: content = {urlEncodedContent}");
            Console.WriteLine($"📖 开始发送短信: sign = {sign}");

            var response = await httpClient.PostAsync(sendUrl, requestContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            // 打印响应结果（返回）
            _logger.LogInformation("📖 短信接口响应: HTTP状态码 = {StatusCode}", response.StatusCode);
            _logger.LogInformation("📖 短信接口响应: 响应内容 = {ResponseContent}", responseContent);
            Console.WriteLine($"📖 短信接口响应: HTTP状态码 = {response.StatusCode}");
            Console.WriteLine($"📖 短信接口响应: 响应内容 = {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    if (result.TryGetProperty("code", out var codeElement))
                    {
                        string code = codeElement.GetString() ?? "";
                        string msg = result.TryGetProperty("msg", out var msgElement) 
                            ? msgElement.GetString() ?? "未知错误" 
                            : "未知错误";
                        
                        // 格式化响应结果用于打印
                        var formattedResponse = JsonSerializer.Serialize(new { code, msg }, new JsonSerializerOptions { WriteIndented = true });
                        
                        if (code == "1")
                        {
                            // 正例：{"code":1,"msg":"ok"}
                            _logger.LogInformation("✅ 短信发送成功: code = {Code}, msg = {Msg}, mobiles = {Mobiles}", code, msg, mobiles);
                            Console.WriteLine($"✅ 短信发送成功: code = {code}, msg = {msg}, mobiles = {mobiles}");
                            return true;
                        }
                        else
                        {
                            // 反例：{"code":0,"msg":"验签失败"}
                            _logger.LogWarning("⚠️ 短信发送失败: code = {Code}, msg = {Msg}, mobiles = {Mobiles}", code, msg, mobiles);
                            Console.WriteLine($"⚠️ 短信发送失败: code = {code}, msg = {msg}, mobiles = {mobiles}");
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ 短信接口解析响应失败: 响应内容 = {Content}", responseContent);
                    Console.WriteLine($"❌ 短信接口解析响应失败: 响应内容 = {responseContent}");
                }
            }
            else
            {
                // HTTP状态码不是成功时，打印响应内容
                _logger.LogWarning("⚠️ 短信接口HTTP请求失败: HTTP状态码 = {StatusCode}, 响应内容 = {ResponseContent}", 
                    (int)response.StatusCode, responseContent);
                Console.WriteLine($"⚠️ 短信接口HTTP请求失败: HTTP状态码 = {(int)response.StatusCode}, 响应内容 = {responseContent}");
            }

            _logger.LogWarning("⚠️ 短信发送失败: 状态码={StatusCode}, 响应={Content}", response.StatusCode, responseContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送短信异常: 号码={Mobiles}", mobiles);
            return false;
        }
    }

    public async Task<bool> SendVoiceAsync(string mobiles, string content)
    {
        if (string.IsNullOrEmpty(_serverUrl) || string.IsNullOrEmpty(_appId) || string.IsNullOrEmpty(_appSecret))
        {
            _logger.LogWarning("⚠️ 短信平台配置不完整，跳过发送语音");
            return false;
        }

        // 检查手机号码数量（最多30个）
        var mobileList = mobiles.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (mobileList.Length > 30)
        {
            _logger.LogWarning("⚠️ 语音发送最多支持30个号码，当前有{Count}个", mobileList.Length);
            mobiles = string.Join(",", mobileList.Take(30));
        }

        try
        {
            _logger.LogInformation("📞 开始发送语音: 号码={Mobiles}, 内容长度={Length}", mobiles, content.Length);

            // 生成时间戳 yyyyMMddHHmmss
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            // URLEncode(content, "UTF-8")
            // 在 .NET Core 中，使用 Uri.EscapeDataString 替代 HttpUtility.UrlEncode
            string urlEncodedContent = Uri.EscapeDataString(content);

            // 生成签名
            string sign = GenerateSign(timestamp, mobiles, urlEncodedContent);

            // 构建请求数据
            var requestData = new
            {
                appid = _appId,
                timestamp = timestamp,
                mobiles = mobiles,
                content = urlEncodedContent,
                sign = sign
            };

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var requestJson = JsonSerializer.Serialize(requestData, new JsonSerializerOptions { WriteIndented = true });
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            string sendUrl = $"{_serverUrl.TrimEnd('/')}/sdk/voiceSend";
            
            // 打印请求参数（入参）- 类似OPC UA日志格式
            _logger.LogInformation("📖 开始发送语音: URL = {Url}", sendUrl);
            _logger.LogInformation("📖 开始发送语音: appid = {AppId}", _appId);
            _logger.LogInformation("📖 开始发送语音: timestamp = {Timestamp}", timestamp);
            _logger.LogInformation("📖 开始发送语音: mobiles = {Mobiles}", mobiles);
            _logger.LogInformation("📖 开始发送语音: content = {Content}", urlEncodedContent);
            _logger.LogInformation("📖 开始发送语音: sign = {Sign}", sign);
            
            Console.WriteLine($"📖 开始发送语音: URL = {sendUrl}");
            Console.WriteLine($"📖 开始发送语音: appid = {_appId}");
            Console.WriteLine($"📖 开始发送语音: timestamp = {timestamp}");
            Console.WriteLine($"📖 开始发送语音: mobiles = {mobiles}");
            Console.WriteLine($"📖 开始发送语音: content = {urlEncodedContent}");
            Console.WriteLine($"📖 开始发送语音: sign = {sign}");

            var response = await httpClient.PostAsync(sendUrl, requestContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            // 打印响应结果（返回）
            _logger.LogInformation("📖 语音接口响应: HTTP状态码 = {StatusCode}", response.StatusCode);
            _logger.LogInformation("📖 语音接口响应: 响应内容 = {ResponseContent}", responseContent);
            Console.WriteLine($"📖 语音接口响应: HTTP状态码 = {response.StatusCode}");
            Console.WriteLine($"📖 语音接口响应: 响应内容 = {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    if (result.TryGetProperty("code", out var codeElement))
                    {
                        string code = codeElement.GetString() ?? "";
                        string msg = result.TryGetProperty("msg", out var msgElement) 
                            ? msgElement.GetString() ?? "未知错误" 
                            : "未知错误";
                        
                        // 格式化响应结果用于打印
                        var formattedResponse = JsonSerializer.Serialize(new { code, msg }, new JsonSerializerOptions { WriteIndented = true });
                        
                        if (code == "1")
                        {
                            // 正例：{"code":1,"msg":"ok"}
                            _logger.LogInformation("✅ 语音发送成功: code = {Code}, msg = {Msg}, mobiles = {Mobiles}", code, msg, mobiles);
                            Console.WriteLine($"✅ 语音发送成功: code = {code}, msg = {msg}, mobiles = {mobiles}");
                            return true;
                        }
                        else
                        {
                            // 反例：{"code":0,"msg":"验签失败"}
                            _logger.LogWarning("⚠️ 语音发送失败: code = {Code}, msg = {Msg}, mobiles = {Mobiles}", code, msg, mobiles);
                            Console.WriteLine($"⚠️ 语音发送失败: code = {code}, msg = {msg}, mobiles = {mobiles}");
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ 语音接口解析响应失败: 响应内容 = {Content}", responseContent);
                    Console.WriteLine($"❌ 语音接口解析响应失败: 响应内容 = {responseContent}");
                }
            }
            else
            {
                // HTTP状态码不是成功时，打印响应内容
                _logger.LogWarning("⚠️ 语音接口HTTP请求失败: HTTP状态码 = {StatusCode}, 响应内容 = {ResponseContent}", 
                    (int)response.StatusCode, responseContent);
                Console.WriteLine($"⚠️ 语音接口HTTP请求失败: HTTP状态码 = {(int)response.StatusCode}, 响应内容 = {responseContent}");
            }

            _logger.LogWarning("⚠️ 语音发送失败: 状态码={StatusCode}, 响应={Content}", response.StatusCode, responseContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送语音异常: 号码={Mobiles}", mobiles);
            return false;
        }
    }
}

