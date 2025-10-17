pdd.ddk.goods.search多多进宝商品查询
更新时间：2025-06-26 14:35:43

¥免费API不需用户授权
多多进宝商品查询

公共参数
点击展开
请求参数说明
点击收起
参数接口	参数类型	是否必填	说明
activity_tags	INTEGER[]	非必填	活动商品标记数组，例：[4,7]，4-秒杀，7-百亿补贴，10851-千万补贴，11879-千万神券，10913-招商礼金商品，31-品牌黑标，10564-精选爆品-官方直推爆款，10584-精选爆品-团长推荐，24-品牌高佣，其他的值请忽略
block_cat_packages	INTEGER[]	非必填	屏蔽商品类目包：1-拼多多小程序屏蔽的类目&关键词;2-虚拟类目;3-医疗器械;4-处方药;5-非处方药;6-冬奥元素相关商品
block_cats	INTEGER[]	非必填	自定义屏蔽一级/二级/三级类目ID，自定义数量不超过20个;使用pdd.goods.cats.get接口获取cat_id
cat_id	LONG	非必填	商品类目ID，使用pdd.goods.cats.get接口获取
custom_parameters	STRING	非必填	自定义参数，为链接打上自定义标签；自定义参数最长限制64个字节；格式为： {"uid":"11111","sid":"22222"} ，其中 uid 用户唯一标识，可自行加密后传入，每个用户仅且对应一个标识，必填； sid 上下文信息标识，例如sessionId等，非必填。该json字符串中也可以加入其他自定义的key。（如果使用GET请求，请使用URLEncode处理参数）
goods_img_type	INTEGER	非必填	商品主图类型：1-场景图，2-白底图，默认为0
goods_sign_list	STRING[]	非必填	商品goodsSign列表，例如：["c9r2omogKFFAc7WBwvbZU1ikIb16_J3CTa8HNN"]，支持通过goodsSign查询商品。goodsSign是加密后的goodsId, goodsId已下线，请使用goodsSign来替代。使用说明：https://jinbao.pinduoduo.com/qa-system?questionId=252
is_brand_goods	BOOLEAN	非必填	是否为品牌商品
keyword	STRING	非必填	商品关键词，与opt_id字段选填一个或全部填写。可支持goods_id、拼多多链接（即拼多多app商详的链接）、进宝长链/短链（即为pdd.ddk.goods.promotion.url.generate接口生成的长短链）
list_id	STRING	非必填	翻页时建议填写前页返回的list_id值
merchant_type	INTEGER	非必填	店铺类型，1-个人，2-企业，3-旗舰店，4-专卖店，5-专营店，6-普通店（未传为全部）
merchant_type_list	INTEGER[]	非必填	店铺类型数组，例如：[1,2]
opt_id	LONG	非必填	商品标签类目ID，使用pdd.goods.opt.get获取
page	INTEGER	非必填	默认值1，商品分页数
page_size	INTEGER	非必填	默认100，每页商品数量
pid	STRING	非必填	推广位id
range_list	OBJECT[]	非必填	筛选范围列表 样例：[{"range_id":0,"range_from":1,"range_to":1500},{"range_id":1,"range_from":1,"range_to":1500}]
range_from	LONG	非必填	区间的开始值
range_id	INTEGER	非必填	0，最小成团价 1，券后价 2，佣金比例 3，优惠券价格 4，广告创建时间 5，销量 6，佣金金额 7，店铺描述分 8，店铺物流分 9，店铺服务分 10， 店铺描述分击败同行业百分比 11， 店铺物流分击败同行业百分比 12，店铺服务分击败同行业百分比 13，商品分 17 ，优惠券/最小团购价 18，过去两小时pv 19，过去两小时销量
range_to	LONG	非必填	区间的结束值
risk_params	MAP	非必填	风控参数
$key	STRING	非必填	风控参数key
$value	STRING	非必填	风控参数value
sort_type	INTEGER	非必填	排序方式:0-综合排序;1-按佣金比率升序;2-按佣金比例降序;3-按价格升序;4-按价格降序;5-按销量升序;6-按销量降序;7-优惠券金额排序升序;8-优惠券金额排序降序;9-券后价升序排序;10-券后价降序排序;11-按照加入多多进宝时间升序;12-按照加入多多进宝时间降序;13-按佣金金额升序排序;14-按佣金金额降序排序;15-店铺描述评分升序;16-店铺描述评分降序;17-店铺物流评分升序;18-店铺物流评分降序;19-店铺服务评分升序;20-店铺服务评分降序;27-描述评分击败同类店铺百分比升序，28-描述评分击败同类店铺百分比降序，29-物流评分击败同类店铺百分比升序，30-物流评分击败同类店铺百分比降序，31-服务评分击败同类店铺百分比升序，32-服务评分击败同类店铺百分比降序
use_customized	BOOLEAN	非必填	是否使用个性化推荐，true表示使用，false表示不使用，默认true。
with_coupon	BOOLEAN	非必填	是否只返回优惠券的商品，false返回所有商品，true只返回有优惠券的商品
返回参数说明
点击收起
参数接口	参数类型	例子	说明
goods_search_response	OBJECT		response
goods_list	OBJECT[]		商品列表
activity_promotion_rate	LONG		活动佣金比例，千分比（特定活动期间的佣金比例）
activity_tags	INTEGER[]		商品活动标记数组，例：[4,7]，4-秒杀 7-百亿补贴等
activity_type	INTEGER		活动类型，0-无活动;1-秒杀;3-限量折扣;12-限时折扣;13-大促活动;14-名品折扣;15-品牌清仓;16-食品超市;17-一元幸运团;18-爱逛街;19-时尚穿搭;20-男人帮;21-9块9;22-竞价活动;23-榜单活动;24-幸运半价购;25-定金预售;26-幸运人气购;27-特色主题活动;28-断码清仓;29-一元话费;30-电器城;31-每日好店;32-品牌卡;101-大促搜索池;102-大促品类分会场;
brand_name	STRING		商品品牌词信息，如“苹果”、“阿迪达斯”、“李宁”等
cash_gift_amount	LONG		全局礼金金额，单位分
cat_ids	LONG[]		商品类目id
clt_cpn_batch_sn	STRING		店铺收藏券id
clt_cpn_discount	LONG		店铺收藏券面额,单位为分
clt_cpn_end_time	LONG		店铺收藏券截止时间
clt_cpn_min_amt	LONG		店铺收藏券使用门槛价格,单位为分
clt_cpn_quantity	LONG		店铺收藏券总量
clt_cpn_remain_quantity	LONG		店铺收藏券剩余量
clt_cpn_start_time	LONG		店铺收藏券起始时间
coupon_discount	LONG		优惠券面额，单位为分
coupon_end_time	LONG		优惠券失效时间，UNIX时间戳
coupon_min_order_amount	LONG		优惠券门槛价格，单位为分
coupon_remain_quantity	LONG		优惠券剩余数量
coupon_start_time	LONG		优惠券生效时间，UNIX时间戳
coupon_total_quantity	LONG		优惠券总数量
create_at	LONG		创建时间（unix时间戳）
desc_txt	STRING		描述分
extra_coupon_amount	LONG		额外优惠券，单位为分
goods_desc	STRING		商品描述
goods_image_url	STRING		商品主图
goods_labels	INTEGER[]		商品特殊标签列表。例: [1]，1-APP专享
goods_name	STRING		商品名称
goods_sign	STRING		商品goodsSign，支持通过goodsSign查询商品。goodsSign是加密后的goodsId, goodsId已下线，请使用goodsSign来替代。使用说明：https://jinbao.pinduoduo.com/qa-system?questionId=252
goods_thumbnail_url	STRING		商品缩略图
has_coupon	BOOLEAN		商品是否有优惠券 true-有，false-没有
has_mall_coupon	BOOLEAN		是否有店铺券
has_material	BOOLEAN		商品是否有素材(图文、视频)
is_multi_group	BOOLEAN		是否多人团
lgst_txt	STRING		物流分
mall_coupon_discount_pct	INTEGER		店铺券折扣
mall_coupon_end_time	LONG		店铺券结束使用时间
mall_coupon_id	LONG		店铺券id
mall_coupon_max_discount_amount	INTEGER		最大使用金额
mall_coupon_min_order_amount	INTEGER		最小使用金额
mall_coupon_remain_quantity	LONG		店铺券余量
mall_coupon_start_time	LONG		店铺券开始使用时间
mall_coupon_total_quantity	LONG		店铺券总量
mall_cps	INTEGER		该商品所在店铺是否参与全店推广，0：否，1：是
mall_id	LONG		店铺id
mall_name	STRING		店铺名字
mall_sn	STRING		店铺sn，店铺id不存在时作为店铺唯一标识
merchant_type	INTEGER		店铺类型，1-个人，2-企业，3-旗舰店，4-专卖店，5-专营店，6-普通店
min_group_price	LONG		最小拼团价（单位为分）
min_normal_price	LONG		最小单买价格（单位为分）
only_scene_auth	BOOLEAN		快手专享
opt_id	LONG		商品标签ID，使用pdd.goods.opts.get接口获取
opt_ids	LONG[]		商品标签id
opt_name	STRING		商品标签名
plan_type	INTEGER		推广计划类型: 1-全店推广 2-单品推广 3-定向推广 4-招商推广 5-分销推广
platform_discount_list	OBJECT[]		进宝平台券信息
coupon_amount	LONG		券面额，单位分
coupon_threshold	LONG		券门槛，单位分
discount_type	INTEGER		优惠类型：17-千万神券 21-达人礼金 22-超红大额券
predict_promotion_rate	LONG		比价行为预判定佣金，需要用户备案
promotion_rate	LONG		佣金比例，千分比
sales_tip	STRING		已售卖件数
search_id	STRING		搜索id，建议生成推广链接时候填写，提高收益
serv_txt	STRING		服务分
service_tags	LONG[]		服务标签: 1-全场包邮,2-七天退换,3-退货包运费,4-送货入户并安装,5-送货入户,6-电子发票,7-诚信发货,8-缺重包赔,9-坏果包赔,10-果重保证,11-闪电退款,12-24小时发货,13-48小时发货,14-免税费,15-假一罚十,16-贴心服务,17-顺丰包邮,18-只换不修,19-全国联保,20-分期付款,21-纸质发票,22-上门安装,23-爱心助农,24-极速退款,25-品质保障,26-缺重包退,27-当日发货,28-可定制化,29-预约配送,30-商品进口,31-电器城,1000001-正品发票,1000002-送货入户并安装,2000001-价格保护
share_rate	INTEGER		招商分成服务费比例，千分比
subsidy_amount	INTEGER		优势渠道专属商品补贴金额，单位为分。针对优质渠道的补贴活动，指定优势渠道可通过推广该商品获取相应补贴。补贴活动入口：[进宝网站-官方活动]
subsidy_duo_amount_ten_million	INTEGER		官方活动给渠道的收入补贴金额，不允许直接给下级代理展示，单位为分
subsidy_goods_type	INTEGER		补贴活动类型：0-无补贴，1-千万补贴，4-千万神券，6-佣金翻倍
unified_tags	STRING[]		优惠标签列表，包括："X元券","比全网低X元","服务费","精选素材","近30天低价","同款低价","同款好评","同款热销","旗舰店","一降到底","招商优选","商家优选","好价再降X元","全站销量XX","实时热销榜第X名","实时好评榜第X名","额外补X元"等
zs_duo_id	LONG		招商团长id
list_id	STRING		翻页时必填前页返回的list_id值
search_id	STRING		搜索id，建议生成推广链接时候填写，提高收益
total_count	INTEGER		返回商品总数
请求示例
点击收起
JAVA
CURL
1
curl -X POST 'http://host:port/api/router' \
2
-H 'Content-Type:application/x-www-form-urlencoded;charset=utf-8' \
3
-d list_id=str \
4
-d sign=your+sign \
5
-d use_customized=true \
6
-d pid=str \
7
-d type=pdd.ddk.goods.search \
8
-d client_id=your+client+id \
9
-d is_brand_goods=true \
10
-d merchant_type_list=%5B0%5D \
11
-d cat_id=49 \
12
-d risk_params=%7B%22%22%3A%22str%22%7D \
13
-d keyword=str \
14
-d goods_img_type=1 \
15
-d block_cat_packages=%5B0%5D \
16
-d merchant_type=1 \
17
-d page_size=100 \
18
-d timestamp=1527065024 \
19
-d with_coupon=true \
20
-d block_cats=%5B0%5D \
21
-d custom_parameters=str \
22
-d range_list=%5B%7Brange_from%3D0%2C+range_id%3D0%2C+range_to%3D0%7D%5D \
23
-d opt_id=49 \
24
-d access_token=your+access+token \
25
-d sort_type=0 \
26
-d activity_tags=%5B0%5D \
27
-d goods_sign_list=%5Bstr%5D \
28
-d data_type=JSON \
29
-d page=1
响应示例
点击收起
1
{
2
"goods_search_response": {
3
"goods_list": [
4
{
5
"activity_promotion_rate": 0,
6
"activity_tags": [
7
0
8
],
9
"activity_type": 0,
10
"brand_name": "str",
11
"cash_gift_amount": 0,
12
"cat_ids": [
13
0
14
],
15
"clt_cpn_batch_sn": "str",
16
"clt_cpn_discount": 0,
17
"clt_cpn_end_time": 0,
18
"clt_cpn_min_amt": 0,
19
"clt_cpn_quantity": 0,
20
"clt_cpn_remain_quantity": 0,
21
"clt_cpn_start_time": 0,
22
"coupon_discount": 0,
23
"coupon_end_time": 0,
24
"coupon_min_order_amount": 0,
25
"coupon_remain_quantity": 0,
26
"coupon_start_time": 0,
27
"coupon_total_quantity": 0,
28
"create_at": 0,
29
"desc_txt": "str",
30
"extra_coupon_amount": 0,
31
"goods_desc": "str",
32
"goods_image_url": "str",
33
"goods_labels": [
34
0
35
],
36
"goods_name": "str",
37
"goods_sign": "str",
38
"goods_thumbnail_url": "str",
39
"has_coupon": false,
40
"has_mall_coupon": false,
41
"has_material": false,
42
"is_multi_group": false,
43
"lgst_txt": "str",
44
"mall_coupon_discount_pct": 0,
45
"mall_coupon_end_time": 0,
46
"mall_coupon_id": 0,
47
"mall_coupon_max_discount_amount": 0,
48
"mall_coupon_min_order_amount": 0,
49
"mall_coupon_remain_quantity": 0,
50
"mall_coupon_start_time": 0,
51
"mall_coupon_total_quantity": 0,
52
"mall_cps": 0,
53
"mall_id": 0,
54
"mall_name": "str",
55
"mall_sn": "str",
56
"merchant_type": 0,
57
"min_group_price": 0,
58
"min_normal_price": 0,
59
"only_scene_auth": false,
60
"opt_id": 0,
61
"opt_ids": [
62
0
63
],
64
"opt_name": "str",
65
"plan_type": 0,
66
"platform_discount_list": [
67
{
68
"coupon_amount": 0,
69
"coupon_threshold": 0,
70
"discount_type": 0
71
}
72
],
73
"predict_promotion_rate": 0,
74
"promotion_rate": 0,
75
"sales_tip": "str",
76
"search_id": "str",
77
"serv_txt": "str",
78
"service_tags": [
79
0
80
],
81
"share_rate": 0,
82
"subsidy_amount": 0,
83
"subsidy_duo_amount_ten_million": 0,
84
"subsidy_goods_type": 0,
85
"unified_tags": [
86
"str"
87
],
88
"zs_duo_id": 0
89
}
90
],
91
"list_id": "str",
92
"search_id": "str",
93
"total_count": 0
94
}
95
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