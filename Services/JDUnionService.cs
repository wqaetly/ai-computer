using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ai_computer.Models.JDUnion;

namespace ai_computer.Services;

/// <summary>
/// 京东联盟API服务
/// </summary>
public class JDUnionService
{
    private readonly HttpClient _httpClient;
    private readonly JDUnionConfig _config;

    public JDUnionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _config = new JDUnionConfig();
    }

    public JDUnionService(HttpClient httpClient, JDUnionConfig config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    /// <summary>
    /// 搜索商品
    /// </summary>
    public async Task<GoodsQueryResponse?> SearchGoodsAsync(GoodsQueryRequest request)
    {
        try
        {
            const string method = "jd.union.open.goods.query";

            // 构建业务参数
            var goodsReqJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            // 构建系统参数
            var sysParams = BuildSystemParams(method);
            sysParams["param_json"] = goodsReqJson;

            // 生成签名
            var sign = GenerateSign(sysParams, _config.SecretKey);
            sysParams["sign"] = sign;

            // 发送请求
            var response = await _httpClient.PostAsync(_config.ApiBaseUrl,
                new FormUrlEncodedContent(sysParams));

            if (!response.IsSuccessStatusCode)
            {
                return new GoodsQueryResponse
                {
                    Code = (int)response.StatusCode,
                    Message = $"HTTP请求失败: {response.ReasonPhrase}"
                };
            }

            var json = await response.Content.ReadAsStringAsync();

            // 解析响应
            var apiResponse = JsonSerializer.Deserialize<JDApiGoodsResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.QueryResult?.QueryResultJson != null)
            {
                var result = JsonSerializer.Deserialize<GoodsQueryResponse>(
                    apiResponse.QueryResult.QueryResultJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result;
            }

            return new GoodsQueryResponse
            {
                Code = 500,
                Message = "响应数据格式错误"
            };
        }
        catch (Exception ex)
        {
            return new GoodsQueryResponse
            {
                Code = 500,
                Message = $"查询商品异常: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 生成推广链接
    /// </summary>
    public async Task<PromotionResponse?> GeneratePromotionLinkAsync(PromotionRequest request)
    {
        try
        {
            const string method = "jd.union.open.promotion.common.get";

            // 构建业务参数
            var promotionReqJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            // 构建系统参数
            var sysParams = BuildSystemParams(method);
            sysParams["param_json"] = promotionReqJson;

            // 生成签名
            var sign = GenerateSign(sysParams, _config.SecretKey);
            sysParams["sign"] = sign;

            // 发送请求
            var response = await _httpClient.PostAsync(_config.ApiBaseUrl,
                new FormUrlEncodedContent(sysParams));

            if (!response.IsSuccessStatusCode)
            {
                return new PromotionResponse
                {
                    Code = (int)response.StatusCode,
                    Message = $"HTTP请求失败: {response.ReasonPhrase}"
                };
            }

            var json = await response.Content.ReadAsStringAsync();

            // 解析响应
            var apiResponse = JsonSerializer.Deserialize<JDApiPromotionResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.GetResult?.GetResultJson != null)
            {
                var result = JsonSerializer.Deserialize<PromotionResponse>(
                    apiResponse.GetResult.GetResultJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result;
            }

            return new PromotionResponse
            {
                Code = 500,
                Message = "响应数据格式错误"
            };
        }
        catch (Exception ex)
        {
            return new PromotionResponse
            {
                Code = 500,
                Message = $"生成推广链接异常: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 构建系统参数
    /// </summary>
    private Dictionary<string, string> BuildSystemParams(string method)
    {
        return new Dictionary<string, string>
        {
            { "method", method },
            { "app_key", _config.AppKey },
            { "timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "format", "json" },
            { "v", "1.0" },
            { "sign_method", "md5" }
        };
    }

    /// <summary>
    /// 生成MD5签名
    /// </summary>
    private string GenerateSign(Dictionary<string, string> parameters, string secret)
    {
        // 参数排序
        var sortedParams = parameters.OrderBy(p => p.Key);

        // 拼接参数：secret + key1value1key2value2... + secret
        var sb = new StringBuilder();
        sb.Append(secret);

        foreach (var param in sortedParams)
        {
            if (!string.IsNullOrEmpty(param.Value))
            {
                sb.Append(param.Key).Append(param.Value);
            }
        }

        sb.Append(secret);

        // MD5加密并转大写
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        var result = BitConverter.ToString(bytes).Replace("-", "").ToUpper();

        return result;
    }
}
