# 拼多多API集成指南

## 概述

本文档说明如何使用拼多多API集成进行商品推荐。项目现已支持三个电商平台：拼多多、京东、淘宝（淘宝暂未实现）。

## 架构说明

### 1. 电商供应商枚举

**文件**: `Models/ECommerceProvider.cs`

定义了三个电商平台：
- `PinDuoDuo` - 拼多多（默认）
- `JingDong` - 京东
- `TaoBao` - 淘宝（待实现）

### 2. 拼多多数据模型

**文件夹**: `Models/PDD/`

- `PDDConfig.cs` - 拼多多API配置（包含client_id, client_secret, access_token, pid等）
- `GoodsSearchRequest.cs` - 商品搜索请求模型
- `GoodsSearchResponse.cs` - 商品搜索响应模型

### 3. 拼多多服务类

#### PDDUnionService
**文件**: `Services/PDDUnionService.cs`

负责与拼多多API通信的底层服务：
- `SearchGoodsAsync()` - 搜索商品
- `GeneratePromotionLink()` - 生成推广链接
- `GenerateSign()` - 生成MD5签名

#### PDDGoodsRecommendService
**文件**: `Services/PDDGoodsRecommendService.cs`

提供高级商品推荐功能：
- `RecommendProductsAsync()` - 智能推荐商品
- 自动筛选和排序
- 综合评分算法（考虑佣金、销量、优惠券、店铺类型等）

### 4. 统一电商服务接口

#### IECommerceService
**文件**: `Services/IECommerceService.cs`

定义统一的电商服务接口和`UnifiedProduct`统一商品模型。

#### UnifiedECommerceService
**文件**: `Services/UnifiedECommerceService.cs`

统一的电商服务实现，根据设置自动选择调用不同平台的API：
```csharp
var service = new UnifiedECommerceService();
var products = await service.RecommendProductsAsync(
    keyword: "iPhone",
    minPrice: 3000,
    maxPrice: 8000,
    maxCount: 3
);
```

## 使用方法

### 1. 在设置中选择电商平台

运行应用后，进入"设置" → "AI设置" → "电商平台"，选择需要使用的平台（拼多多/京东/淘宝）。

### 2. 在代码中使用统一服务

```csharp
// 使用统一的电商服务（自动根据设置选择平台）
var ecommerceService = new UnifiedECommerceService();

// 推荐商品
var products = await ecommerceService.RecommendProductsAsync(
    keyword: "笔记本电脑",
    minPrice: 3000,
    maxPrice: 6000,
    maxCount: 5
);

// 处理结果
foreach (var product in products)
{
    Console.WriteLine($"商品: {product.ProductName}");
    Console.WriteLine($"价格: {product.GetPriceDisplay()}");
    Console.WriteLine($"推广链接: {product.PromotionUrl}");
    Console.WriteLine($"平台: {product.Platform}");
}
```

### 3. 直接使用拼多多服务

如果需要直接使用拼多多服务：

```csharp
var httpClient = new HttpClient();
var pddService = new PDDUnionService(httpClient);
var recommendService = new PDDGoodsRecommendService(pddService);

var products = await recommendService.RecommendProductsAsync(
    keyword: "键盘",
    minPrice: 100,
    maxPrice: 500,
    maxCount: 3
);
```

## API配置

拼多多API配置在 `Models/PDD/PDDConfig.cs` 中：

```csharp
public class PDDConfig
{
    public string ClientId { get; set; } = "8de7a099abc743348a31184daed8fc96";
    public string ClientSecret { get; set; } = "3e0f19916a96583d3ae3602a39c7283bece69644";
    public string AccessToken { get; set; } = "44b763d20422472f802fdf3ec84b6f2134127550";
    public string Pid { get; set; } = "43592911_311190797";
    public string ApiBaseUrl { get; set; } = "http://gw-api.pinduoduo.com/api/router";
}
```

**注意**: 这些是从拼多多开发者平台获取的凭证，需要妥善保管。

## 商品筛选和评分算法

PDDGoodsRecommendService使用综合评分算法筛选商品：

- **佣金比例** (30%权重) - 优先推荐高佣金商品
- **销量** (25%权重) - 销量高的商品更受欢迎
- **优惠券** (20%权重) - 有优惠券且优惠力度大的商品
- **店铺类型** (15%权重) - 旗舰店 > 专卖店 > 专营店 > 企业店 > 个人店
- **价格合理性** (10%权重) - 券后价适中的商品

## 推广链接生成

当前版本使用简化的推广链接生成方式：

```csharp
public string GeneratePromotionLink(string goodsSign, string searchId)
{
    return $"https://mobile.yangkeduo.com/goods.html?goods_id={goodsSign}";
}
```

**TODO**: 后续可以调用拼多多的推广链接生成API获取真正的推广链接。

## 设置持久化

电商平台选择会自动保存到配置文件：
- 位置: `%AppData%/AiComputer/settings.json`
- 字段: `ECommerceProvider`

## 错误处理

所有服务都包含完善的错误处理和日志输出：

```csharp
try
{
    var products = await service.RecommendProductsAsync(keyword);
    if (products.Count == 0)
    {
        Console.WriteLine("未找到商品");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"查询失败: {ex.Message}");
}
```

## 未来扩展

1. **淘宝联盟集成** - 实现 `RecommendFromTaoBaoAsync` 方法
2. **推广链接API** - 调用拼多多官方推广链接生成API
3. **更多筛选条件** - 支持品牌、类目、活动标签等筛选
4. **商品详情** - 获取商品详细信息、评价等
5. **订单跟踪** - 跟踪推广订单和佣金

## 相关文件

- `.claude/PDD_API.md` - 拼多多API官方文档
- `Models/ECommerceProvider.cs` - 电商供应商枚举
- `Models/PDD/*` - 拼多多数据模型
- `Services/PDDUnionService.cs` - 拼多多API服务
- `Services/PDDGoodsRecommendService.cs` - 拼多多商品推荐服务
- `Services/IECommerceService.cs` - 统一电商接口
- `Services/UnifiedECommerceService.cs` - 统一电商服务实现
- `Services/AppSettingsService.cs` - 应用设置服务
- `ViewModels/SettingsViewModel.cs` - 设置视图模型
- `Views/SettingsView.axaml` - 设置界面
