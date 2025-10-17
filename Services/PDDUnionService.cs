using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ai_computer.Models.PDD;

namespace ai_computer.Services;

/// <summary>
/// 拼多多联盟API服务
/// </summary>
public class PDDUnionService
{
    private readonly HttpClient _httpClient;
    private readonly PDDConfig _config;

    public PDDUnionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _config = new PDDConfig();
    }

    public PDDUnionService(HttpClient httpClient, PDDConfig config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    /// <summary>
    /// 搜索商品 (使用免授权接口 pdd.ddk.goods.search)
    /// </summary>
    public async Task<GoodsSearchResponse?> SearchGoodsAsync(GoodsSearchRequest request)
    {
        try
        {
            const string method = "pdd.ddk.goods.search";

            // 确保使用配置中的PID
            if (string.IsNullOrEmpty(request.Pid))
            {
                request.Pid = _config.Pid;
            }

            // 构建请求参数
            var parameters = new Dictionary<string, string>
            {
                { "type", method },
                { "client_id", _config.ClientId },
                { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() },
                { "data_type", "JSON" }
            };

            // 添加业务参数
            if (!string.IsNullOrEmpty(request.Keyword))
                parameters["keyword"] = request.Keyword;

            if (request.Page > 0)
                parameters["page"] = request.Page.ToString();

            if (request.PageSize > 0)
                parameters["page_size"] = request.PageSize.ToString();

            if (request.SortType >= 0)
                parameters["sort_type"] = request.SortType.ToString();

            if (request.WithCoupon.HasValue)
                parameters["with_coupon"] = request.WithCoupon.Value.ToString().ToLower();

            if (!string.IsNullOrEmpty(request.Pid))
                parameters["pid"] = request.Pid;

            if (request.OptId.HasValue)
                parameters["opt_id"] = request.OptId.Value.ToString();

            if (request.CatId.HasValue)
                parameters["cat_id"] = request.CatId.Value.ToString();

            if (request.MerchantType.HasValue)
                parameters["merchant_type"] = request.MerchantType.Value.ToString();

            if (request.IsBrandGoods.HasValue)
                parameters["is_brand_goods"] = request.IsBrandGoods.Value.ToString().ToLower();

            // 如果有价格范围，添加range_list
            if (request.RangeList != null && request.RangeList.Count > 0)
            {
                var rangeListJson = JsonSerializer.Serialize(request.RangeList, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                parameters["range_list"] = rangeListJson;
            }

            // 生成签名
            var sign = GenerateSign(parameters, _config.ClientSecret);
            parameters["sign"] = sign;

            // 构建URL（使用GET方式）
            var queryString = string.Join("&", parameters.Select(p =>
                $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
            var requestUrl = $"{_config.ApiBaseUrl}?{queryString}";

            Console.WriteLine($"[PDDUnion] 使用API: {method}");
            Console.WriteLine($"[PDDUnion] 请求URL: {requestUrl}");

            // 发送请求
            var response = await _httpClient.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[PDDUnion] HTTP请求失败: {response.StatusCode} - {response.ReasonPhrase}");
                return new GoodsSearchResponse
                {
                    ErrorResponse = new ErrorResponse
                    {
                        ErrorCode = (int)response.StatusCode,
                        ErrorMsg = $"HTTP请求失败: {response.ReasonPhrase}"
                    }
                };
            }

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[PDDUnion] API响应: {json.Substring(0, Math.Min(500, json.Length))}...");

            // 解析响应
            var apiResponse = JsonSerializer.Deserialize<GoodsSearchResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return apiResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PDDUnion] 查询商品异常: {ex.Message}");
            Console.WriteLine($"[PDDUnion] 异常堆栈: {ex.StackTrace}");
            return new GoodsSearchResponse
            {
                ErrorResponse = new ErrorResponse
                {
                    ErrorCode = 500,
                    ErrorMsg = $"查询商品异常: {ex.Message}"
                }
            };
        }
    }

    /// <summary>
    /// 生成签名
    /// </summary>
    private string GenerateSign(Dictionary<string, string> parameters, string secret)
    {
        // 参数排序（按key升序）
        var sortedParams = parameters
            .Where(p => p.Key != "sign") // 排除sign参数
            .OrderBy(p => p.Key);

        // 拼接参数：client_secret + key1value1key2value2... + client_secret
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

        var signString = sb.ToString();
        Console.WriteLine($"[PDDUnion] 签名字符串: {signString}");

        // MD5加密并转大写
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(signString));
        var result = BitConverter.ToString(bytes).Replace("-", "").ToUpper();

        Console.WriteLine($"[PDDUnion] 生成签名: {result}");

        return result;
    }

    /// <summary>
    /// 生成推广链接
    /// </summary>
    public async Task<PromotionUrlResponse?> GeneratePromotionUrlAsync(PromotionUrlRequest request)
    {
        try
        {
            const string method = "pdd.ddk.goods.promotion.url.generate";

            // 确保使用配置中的PID
            if (string.IsNullOrEmpty(request.PId))
            {
                request.PId = _config.Pid;
            }

            // 构建请求参数（此接口不需要access_token）
            var parameters = new Dictionary<string, string>
            {
                { "type", method },
                { "client_id", _config.ClientId },
                { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() },
                { "data_type", "JSON" },
                { "p_id", request.PId }
            };

            // 添加业务参数
            if (request.GoodsSignList != null && request.GoodsSignList.Count > 0)
            {
                var goodsSignListJson = JsonSerializer.Serialize(request.GoodsSignList);
                parameters["goods_sign_list"] = goodsSignListJson;
            }

            if (!string.IsNullOrEmpty(request.SearchId))
                parameters["search_id"] = request.SearchId;

            if (request.GenerateShortUrl.HasValue)
                parameters["generate_short_url"] = request.GenerateShortUrl.Value.ToString().ToLower();

            if (request.GenerateWeApp.HasValue)
                parameters["generate_we_app"] = request.GenerateWeApp.Value.ToString().ToLower();

            if (request.MultiGroup.HasValue)
                parameters["multi_group"] = request.MultiGroup.Value.ToString().ToLower();

            if (!string.IsNullOrEmpty(request.CustomParameters))
                parameters["custom_parameters"] = request.CustomParameters;

            if (request.GenerateShareImage.HasValue)
                parameters["generate_share_image"] = request.GenerateShareImage.Value.ToString().ToLower();

            if (request.GenerateWeixinCode.HasValue)
                parameters["generate_weixin_code"] = request.GenerateWeixinCode.Value.ToString().ToLower();

            // 生成签名
            var sign = GenerateSign(parameters, _config.ClientSecret);
            parameters["sign"] = sign;

            // 构建URL
            var queryString = string.Join("&", parameters.Select(p =>
                $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
            var requestUrl = $"{_config.ApiBaseUrl}?{queryString}";

            Console.WriteLine($"[PDDUnion] 推广链接请求URL: {requestUrl}");

            // 发送请求
            var response = await _httpClient.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[PDDUnion] HTTP请求失败: {response.StatusCode} - {response.ReasonPhrase}");
                return new PromotionUrlResponse
                {
                    ErrorResponse = new ErrorResponse
                    {
                        ErrorCode = (int)response.StatusCode,
                        ErrorMsg = $"HTTP请求失败: {response.ReasonPhrase}"
                    }
                };
            }

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[PDDUnion] 推广链接API响应: {json.Substring(0, Math.Min(500, json.Length))}...");

            // 解析响应
            var apiResponse = JsonSerializer.Deserialize<PromotionUrlResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return apiResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PDDUnion] 生成推广链接异常: {ex.Message}");
            Console.WriteLine($"[PDDUnion] 异常堆栈: {ex.StackTrace}");
            return new PromotionUrlResponse
            {
                ErrorResponse = new ErrorResponse
                {
                    ErrorCode = 500,
                    ErrorMsg = $"生成推广链接异常: {ex.Message}"
                }
            };
        }
    }

    /// <summary>
    /// 快捷生成推广链接（单个商品）
    /// </summary>
    public async Task<string?> GeneratePromotionLinkAsync(string goodsSign, string? searchId = null)
    {
        var request = new PromotionUrlRequest
        {
            PId = _config.Pid,
            GoodsSignList = new List<string> { goodsSign },
            SearchId = searchId,
            GenerateShortUrl = true
        };

        var response = await GeneratePromotionUrlAsync(request);

        if (response?.ErrorResponse != null)
        {
            Console.WriteLine($"[PDDUnion] 生成推广链接失败: {response.ErrorResponse.ErrorMsg}");
            return null;
        }

        var promotionUrl = response?.GoodsPromotionUrlGenerateResponse?.GoodsPromotionUrlList?.FirstOrDefault();
        return promotionUrl?.ShortUrl ?? promotionUrl?.Url ?? promotionUrl?.MobileShortUrl ?? promotionUrl?.MobileUrl;
    }
}
