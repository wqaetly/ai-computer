pdd.ddk.goods.promotion.url.generate多多进宝推广链接生成
更新时间：2024-09-14 11:38:52

¥免费API不需用户授权
生成普通商品推广链接

公共参数
点击展开
请求参数说明
点击收起
参数接口	参数类型	是否必填	说明
cash_gift_id	LONG	非必填	多多礼金ID
cash_gift_name	STRING	非必填	自定义礼金标题，用于向用户展示渠道专属福利，不超过12个字
custom_parameters	STRING	非必填	自定义参数，为链接打上自定义标签；自定义参数最长限制64个字节；格式为： {"uid":"11111","sid":"22222"} ，其中 uid 用户唯一标识，可自行加密后传入，每个用户仅且对应一个标识，必填； sid 上下文信息标识，例如sessionId等，非必填。该json字符串中也可以加入其他自定义的key。若进行cid投放，生链的时候不填充custom_parameters，后续在推广前原始链接上拼接custom_parameters。（如果使用GET请求，请使用URLEncode处理参数）
generate_authority_url	BOOLEAN	非必填	是否生成带授权的单品链接。如果未授权，则会走授权流程
generate_mall_collect_coupon	BOOLEAN	非必填	是否生成店铺收藏券推广链接
generate_qq_app	BOOLEAN	非必填	是否生成qq小程序
generate_schema_url	BOOLEAN	非必填	是否返回 schema URL
generate_share_image	BOOLEAN	非必填	是否生成商品推广分享图，仅支持单个商品
generate_short_link	BOOLEAN	非必填	获取微信ShortLink链接，仅支持单个商品，单个渠道每天生成的shortLink数量有限，请合理生成shortLink链接
generate_short_url	BOOLEAN	非必填	是否生成短链接，true-是，false-否
generate_we_app	BOOLEAN	非必填	是否生成拼多多福利券微信小程序推广信息
generate_weixin_code	BOOLEAN	非必填	获取微信小程序码，仅支持单个商品
goods_gen_url_param_list	OBJECT[]	非必填	支持拼接特殊参数的商品生链参数列表。生链优先级：goods_gen_url_param_list > goods_sign_list，两者按优先级选其一。
goods_sign	STRING	非必填	商品goodsSign，支持通过goodsSign查询商品。goodsSign是加密后的goodsId, goodsId已下线，请使用goodsSign来替代。使用说明：https://jinbao.pinduoduo.com/qa-system?questionId=252
sku_id_code_list	STRING[]	非必填	需要在链接上拼接的skuIdCode列表，skuIdCode为skuId密文，由订单详情接口pdd.ddk.order.detail.get返回。要求拥有sku权限否则不生效，作用同sku_id_list，且与sku_id_list独立。此列表传入n个skuIdCode，则针对该goodsSign生成n个拼接sku_id=xxx(skuIdCode)的链接。若列表为空或者skuIdCode无效则返回普通链接
sku_id_list	LONG[]	非必填	需要在链接上拼接的skuId列表，要求拥有sku权限否则不生效。拼接sku_id的链接在点击跳转商详时，自动选中对应的sku。此列表传入n个skuId，则针对该goodsSign生成n个拼接sku_id链接。若列表为空或着skuId无效（null，非正）则返回普通链接。
goods_sign_list	STRING[]	非必填	商品goodsSign列表，例如：["c9r2omogKFFAc7WBwvbZU1ikIb16_J3CTa8HNN"]，支持批量生链。goodsSign是加密后的goodsId, goodsId已下线，请使用goodsSign来替代。使用说明：https://jinbao.pinduoduo.com/qa-system?questionId=252
material_id	STRING	非必填	素材ID，可以通过商品详情接口获取商品素材信息
multi_group	BOOLEAN	非必填	true--生成多人团推广链接 false--生成单人团推广链接（默认false）1、单人团推广链接：用户访问单人团推广链接，可直接购买商品无需拼团。2、多人团推广链接：用户访问双人团推广链接开团，若用户分享给他人参团，则开团者和参团者的佣金均结算给推手
p_id	STRING	必填	推广位ID
search_id	STRING	非必填	搜索id，建议填写，提高收益。来自pdd.ddk.goods.recommend.get、pdd.ddk.goods.search、pdd.ddk.top.goods.list.query等接口
special_params	MAP	非必填	特殊参数
$key	STRING	必填	特殊参数key
$value	STRING	必填	特殊参数value
url_type	INTEGER	非必填	生成商品链接类型 0-默认 1-百补相似品列表
zs_duo_id	LONG	非必填	招商多多客ID
generate_we_app_long_link	BOOLEAN	非必填	是否生成小程序schema长链
返回参数说明
点击收起
参数接口	参数类型	例子	说明
goods_promotion_url_generate_response	OBJECT		response
goods_promotion_url_list	OBJECT[]		多多进宝推广链接对象列表
mobile_short_url	STRING		对应出参mobile_url的短链接，与mobile_url功能一致。
mobile_url	STRING		普通长链，微信环境下进入领券页点领券拉起小程序，浏览器环境下直接拉起APP，未安装拼多多APP时落地页点领券拉起登录页
qq_app_info	OBJECT		qq小程序信息
app_id	STRING		拼多多小程序id
banner_url	STRING		Banner图
desc	STRING		描述
page_path	STRING		小程序path值
qq_app_icon_url	STRING		小程序icon
source_display_name	STRING		来源名
title	STRING		小程序标题
user_name	STRING		用户名
schema_url	STRING		使用此推广链接，用户安装拼多多APP的情况下会唤起APP（需客户端支持schema跳转协议）
share_image_url	STRING		商品推广分享图
short_url	STRING		对应出参url的短链接，与url功能一致
tz_schema_url	STRING		使用此推广链接，用户安装多多团长APP的情况下会唤起APP（需客户端支持schema跳转协议）
url	STRING		普通长链。微信环境下进入领券页点领券拉起小程序，浏览器环境下优先拉起微信小程序
we_app_info	OBJECT		拼多多福利券微信小程序信息
app_id	STRING		小程序id
banner_url	STRING		Banner图
desc	STRING		描述
page_path	STRING		小程序path值
source_display_name	STRING		来源名
title	STRING		小程序标题
user_name	STRING		用户名
we_app_icon_url	STRING		小程序图片
weixin_code	STRING		微信小程序码
weixin_short_link	STRING		小程序短链，点击可直接唤起微信小程序
weixin_long_link	STRING		微信小程序schema长链
请求示例
点击收起
JAVA
CURL
1
curl -X POST 'http://host:port/api/router' \
2
-H 'Content-Type:application/x-www-form-urlencoded;charset=utf-8' \
3
-d generate_share_image=true \
4
-d generate_authority_url=true \
5
-d sign=your+sign \
6
-d generate_we_app_long_link=false \
7
-d type=pdd.ddk.goods.promotion.url.generate \
8
-d client_id=your+client+id \
9
-d cash_gift_name=%E5%A4%9A%E5%A4%9A%E7%A4%BC%E9%87%91 \
10
-d generate_weixin_code=true \
11
-d generate_qq_app=false \
12
-d generate_we_app=true \
13
-d goods_gen_url_param_list=%5B%7Bgoods_sign%3Dstr%2C+sku_id_code_list%3D%5Bstr%5D%2C+sku_id_list%3D%5B0%5D%7D%5D \
14
-d generate_schema_url=false \
15
-d generate_short_url=true \
16
-d timestamp=1527065024 \
17
-d url_type=0 \
18
-d cash_gift_id=1000 \
19
-d custom_parameters=str \
20
-d generate_short_link=true \
21
-d search_id=str \
22
-d special_params=%7B%22%22%3A%22str%22%7D \
23
-d access_token=your+access+token \
24
-d goods_sign_list=%5Bstr%5D \
25
-d data_type=JSON \
26
-d material_id=str \
27
-d multi_group=true \
28
-d generate_mall_collect_coupon=true \
29
-d p_id=str \
30
-d zs_duo_id=21003
响应示例
点击收起
1
{
2
"goods_promotion_url_generate_response": {
3
"goods_promotion_url_list": [
4
{
5
"mobile_short_url": "str",
6
"mobile_url": "str",
7
"qq_app_info": {
8
"app_id": "str",
9
"banner_url": "str",
10
"desc": "str",
11
"page_path": "str",
12
"qq_app_icon_url": "str",
13
"source_display_name": "str",
14
"title": "str",
15
"user_name": "str"
16
},
17
"schema_url": "str",
18
"share_image_url": "str",
19
"short_url": "str",
20
"tz_schema_url": "str",
21
"url": "str",
22
"we_app_info": {
23
"app_id": "str",
24
"banner_url": "str",
25
"desc": "str",
26
"page_path": "str",
27
"source_display_name": "str",
28
"title": "str",
29
"user_name": "str",
30
"we_app_icon_url": "str"
31
},
32
"weixin_code": "str",
33
"weixin_long_link": "str",
34
"weixin_short_link": "str"
35
}
36
]
37
}
38
}
异常示例
点击收起
JSON
XML
1
​
2
{
3
"error_response": {
4
"error_msg": "公共参数错误:type",
5
"sub_msg": "",
6
"sub_code": null,
7
"error_code": 10001,
8
"request_id": "15440104776643887"
9
}
10
}
相关权限包
点击收起
拥有此接口的权限包	可获得/可申请此权限包的应用类型
多多客权限包	多多客联盟
多多客
返回错误码说明
点击收起
主错误码	主错误描述	子错误码	子错误描述	解决办法
10000	参数错误	10000	参数错误	参数值有误，按照文档要求填写请求参数
10001	公共参数错误	10001	公共参数错误	请检查请求的公共参数
10016	client下线或者clientId不正确	10016	client下线或者clientId不正确	请核查您的client_id是否正确
10017	type不正确	10017	type不正确	检查type是否正确
10018	target_client_id下线或者target_client_id不正确	10018	target_client_id下线或者target_client_id不正确	检查target_client_id 是否正确
10019	access_token已过期	10019	access_token已过期	刷新access_token或者重新授权再次获取access_token
20004	签名sign校验失败	20004	签名sign校验失败	请按照接入指南第三部分指导，生成签名
20005	ip无权访问接口，请加入ip白名单	20005	ip无权访问接口，请加入ip白名单	把ip白名单加入白名单
20031	用户没有授权访问此接口	20031	用户没有授权访问此接口	您创建的应用中不包含此接口，请查看API文档，了解相关权限包
20032	access_token或client_id错误	20032	access_token或client_id错误	检查access_token或client_id
20034	接口处于下线状态	20034	接口处于下线状态	检查接口状态
20035	接口不属于当前网关	20035	接口不属于当前网关	判断调用网关url 是否正确
21001	请求参数错误	21001	请求参数错误	业务参数输入错误
21002	请求参数不能为空	21002	请求参数不能为空	检查业务参数必填是否已填
30000	没有调用此target接口权限	30000	没有调用此target接口权限	检查是否获得调用此target接口权限
30001	client_id和partner_id不匹配	30001	client_id和partner_id不匹配	检查partner_id是否正确
50000	系统内部错误	50000	系统内部错误	系统内部错误，请加群联系相关负责人
50002	业务系统内部异常	50002	业务系统内部异常	请加群联系相关负责人
52001	网关业务服务错误	52001	网关业务服务错误	联系技术支持解决
52002	网关系统内部异常	52002	网关系统内部异常	联系技术支持解决
52004	请求body 太大	52004	请求body 太大	检查请求体是否过大
52101	当前接口被限流，请稍后重试	52101	当前接口被限流，请稍后重试	当前接口被限流，请稍后重试
52102	当前接口暂时不可用，请稍后重试	52102	当前接口暂时不可用，请稍后重试	当前接口被降级，请稍后重试
52103	服务暂时不可用，请稍后重试	52103	服务暂时不可用，请稍后重试	当前接口被降级，请稍后重试
70031	调用过于频繁，请调整调用频率	70031	调用过于频繁，请调整调用频率	调用过于频繁，请调整调用频率
70032	当前请求被禁止调用	70032	当前请求被禁止调用	当前请求被禁止调用
70033	当前接口因系统维护，暂时下线，请稍后再试！	70033	当前接口因系统维护，暂时下线，请稍后再试！	当前接口因系统维护，暂时下线，请稍后再试！
70034	当前用户或应用存在风险，禁止调用！	70034	当前用户或应用存在风险，禁止调用！	当前用户或应用存在风险，禁止调用！
70035	当前用户或应用存在风险，禁止调用。请联系ddjb@pinduoduo.com	70035	当前用户或应用存在风险，禁止调用。请联系ddjb@pinduoduo.com	当前用户或应用存在风险，禁止调用。请联系ddjb@pinduoduo.com
70036	应用处于测试状态，调用次数被限制	70036	应用处于测试状态，调用次数被限制	应用处于测试状态，调用次数达到上限被限制
50001	业务服务错误			
限流规则
点击收起
接口总限流频次：111500次/10秒