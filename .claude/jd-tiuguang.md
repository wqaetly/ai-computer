jd.union.open.promotion.common.get
接口描述：网站/APP/流量媒体来获取的推广链接，功能同宙斯接口的自定义链接转换、 APP领取代码接口通过商品链接、活动链接获取普通推广链接，支持传入subunionid参数，可用于区分媒体自身的用户ID，该参数可在订单查询接口返回，需向cps-qxsq@jd.com申请权限。

版本1.0
系统参数
method
String
是
jd.union.open.order.query
API接口名称

app_key
String
是
联盟分配给应用的appkey，可在应用查看中获取appkey

access_token
String
否
根据API属性标签，如果需要授权，则此参数必传;如果不需要授权，则此参数不需要传

timestamp
String
是
2018-01-01 12:00:00
时间戳，格式为yyyy-MM-dd HH:mm:ss，时区为GMT+8。API服务端允许客户端请求最大时间误差为10分钟

format
String
是
json
响应格式，暂时只支持json

v
String
是
1.0
API协议版本，请根据API具体版本号传入此参数，一般为1.0

sign_method
String
是
md5
签名的摘要算法，暂时只支持md5

sign
String
是
API输入参数签名结果

业务参数
promotionCodeReq
PromotionCodeReq
是
无
请求入参

materialId
String
是
https://item.jd.com/23484023378.html
推广物料url，例如活动链接、商品链接、联盟商品ID（itemId）、联盟商品链接（jingfen.jd.com/detail/{itemId}.html）等；单品转链时，sceneId=2时支持入参京东主站商品ID、京东商品链接（包含3.cn单品链接）；其他场景仅支持入参联盟商品ID、联盟商品链接。非单品转链不影响。

siteId
String
是
435676
网站ID/APP ID/流量媒体ID，入口：京东联盟-推广管理-网站管理/APP管理/流量媒体管理-查看网站ID/APP ID/流量媒体ID（1、接口禁止使用导购媒体id入参；2、投放链接的网址或应用必须与传入的网站ID/AppID备案一致，否则订单会判“无效-来源与备案网址不符”）

positionId
Number
否
6
推广位id

subUnionId
String
否
618_18_c35***e6a
子渠道标识，仅支持传入字母、数字、下划线或中划线，最多80个字符（不可包含空格），该参数会在订单行查询接口中展示（需向cps-qxsq@jd.com申请权限）

ext1
String
否
100_618_618
系统扩展参数（需向cps-qxsq@jd.com申请权限），最多支持40字符，不支持中文、特殊符号，参数会在订单行查询接口中展示

protocol
Number
否
0
【已废弃】请勿再使用

pid
String
否
618_618_6018
联盟子推客身份标识（不能传入接口调用者自己的pid）

couponUrl
String
否
http://coupon.jd.com/ilink/get/get_coupon.action?XXXXXXX
优惠券领取链接，在使用优惠券、商品二合一功能时入参，且materialId须为商品详情页链接

giftCouponKey
String
否
xxx_coupon_key
礼金批次号

channelId
Number
否
12345
渠道关系ID

rid
String
否
435676
团长的子渠道id，由团长自定义分配。取rid的优先级：入参rid优先materialId中拼接的rid，materialId中拼接的rid优先cps链接后面拼接的rid。

command
Number
否
1
是否生成短口令：1生成，默认不生成

sceneId
Number
是
1
场景ID，支持入参1,2；2需要权限申请

proType
Number
否
5
5：种草版二合一

返回结果
getResult
GetResult
否
返回结果

code
Number
是
200
返回码

message
String
是
success
返回消息

data
PromotionCodeResp
是
无
数据明细

clickURL
String
是
http://union-click.jd.com/jdc?XXXXXXXXXX
生成的目标推广链接，长期有效

jCommand
String
否
6.0复制整段话 http://JhT7V5wlKygHDK京口令内容#J6UFE5iMn***
京口令（匹配到红包活动有效配置才会返回京口令）

