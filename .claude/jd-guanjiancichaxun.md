jd.union.open.goods.query
接口描述：查询商品及优惠券信息，返回的结果可调用转链接口生成单品或二合一推广链接。支持按SKUID、关键词、优惠券基本属性、是否拼购、是否爆款等条件查询，建议不要同时传入SKUID和其他字段，以获得较多的结果。支持按价格、佣金比例、佣金、引单量等维度排序。用优惠券链接调用转链接口时，需传入搜索接口link字段返回的原始优惠券链接，切勿对链接进行任何encode、decode操作，否则将导致转链二合一推广链接时校验失败【需要申请权限】【支持用户授权】

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
goodsReqDTO
GoodsReq
是
无
请求入参

cid1
Number
否
737
一级类目id

cid2
Number
否
738
二级类目id

cid3
Number
否
739
三级类目id

pageIndex
Number
否
1
页码

pageSize
Number
否
20
每页数量，单页数最大30，默认20

skuIds
Number[]
否
[5225346,7275691]
skuid集合(一次最多支持查询20个sku)，数组类型开发时记得加[];仅sceneId=2时支持入参

keyword
String
否
手机
关键词，字数同京东商品名称一致，目前未限制字数个数；支持cps长链接查询（https://union-click.jd.com）；支持cps短链接查询（https://u.jd.com）;sceneId=2时支持入参京东主站商品ID、京东商品链接（包含3.cn单品链接）。其他场景仅支持入参关键词、联盟商品ID、联盟商品链接

pricefrom
Number
否
16.88
商品券后价格下限

priceto
Number
否
19.95
商品券后价格上限

commissionShareStart
Number
否
10
佣金比例区间开始

commissionShareEnd
Number
否
50
佣金比例区间结束

owner
String
否
g
商品类型：自营[g]，POP[p]

sortName
String
否
price
排序字段(price：单价, commissionShare：佣金比例, commission：佣金， inOrderCount30Days：30天引单量， inOrderComm30Days：30天支出佣金)

sort
String
否
desc
asc,desc升降序,默认降序

isCoupon
Number
否
1
是否是优惠券商品，1：有优惠券

isPG
Number
否
1
是否是拼购商品，1：拼购商品

pingouPriceStart
Number
否
16.88
拼购价格区间开始

pingouPriceEnd
Number
否
19.95
拼购价格区间结束

isHot
Number
否
1
已废弃，请勿使用

brandCode
String
否
7998
品牌code

shopId
Number
否
45619
店铺Id

hasContent
Number
否
1
1：查询内容商品；其他值过滤掉此入参条件。

hasBestCoupon
Number
否
1
1：查询有最优惠券商品；其他值过滤掉此入参条件。（查询最优券需与isCoupon同时使用）

pid
String
否
618_618_618
联盟id_应用iD_推广位id

fields
String
否
videoInfo
支持出参数据筛选，逗号','分隔，目前可用：videoInfo(视频信息),hotWords(热词),similar(相似推荐商品),documentInfo(段子信息，智能文案),skuLabelInfo（商品标签）,promotionLabelInfo（商品促销标签）,stockState（商品库存）,companyType（小店标识）,purchasePriceInfo（到手价）,purchaseBPriceInfo（普惠到手价） ,freeShippingInfo（是否包邮），seckillSpecialPriceInfo（秒杀专享价）

forbidTypes
String
否
10,11
10微信京东购物小程序禁售，11微信京喜小程序禁售

jxFlags
Number[]
否
[1,2,3]
京喜商品类型，1京喜、2京喜工厂直供、3京喜优选，入参多个值表示或条件查询

shopLevelFrom
Number
否
3.5
支持传入0.0、2.5、3.0、3.5、4.0、4.5、4.9，默认为空表示不筛选评分

isbn
String
否
9787515515564
图书编号

spuId
Number
否
11144230
主商品spuId

couponUrl
String
否
http://coupon.m.jd.com/coupons/show.action?key=4fd004d7bd594ca4975db6bc8fecdd1b
优惠券链接

deliveryType
Number
否
1
京东配送 1：是，0：不是

eliteType
Number[]
否
[17]
资源位17：极速版商品，22：百亿补贴，23：便宜包邮，38:学生价商品

isSeckill
Number
否
1
是否秒杀商品。1：是

isPresale
Number
否
1
是否预售商品。1：是

isReserve
Number
否
1
是否预约商品。1:是

bonusId
Number
否
1
奖励活动ID

area
String
否
1-2802-54745
区域地址（查区域价格）

isOversea
Number
否
1
是否全球购商品 1：是

userIdType
Number
否
32
用户ID类型，传入此参数可获得个性化推荐结果。当前userIdType支持的枚举值包括：8、16、32、64、128、32768。userIdType和userId需同时传入，且一一对应。userIdType各枚举值对应的userId含义如下：8(安卓移动设备Imei); 16(苹果移动设备Openudid)；32(苹果移动设备idfa); 64(安卓移动设备imei的md5编码，32位，大写，匹配率略低);128(苹果移动设备idfa的md5编码，32位，大写，匹配率略低); 32768(安卓移动设备oaid); 131072(安卓移动设备oaid的md5编码，32位，大写)

userId
String
否
示例1： userIdType设置为8时，此时userId需要设置为安卓移动设备Imei，如861794042953717 示例2： userIdType设置为16时，此时userId需要设置为苹果移动设备Openudid，如f99dbd2ba8de45a65cd7f08b7737bc919d6c87f7 示例3： userIdType设置为32时，此时userId需要设置为苹果移动设备idfa，如DCC77BDA-C2CA-4729-87D6-B7F65C8014D6 示例4： userIdType设置为64时，此时userId需要设置为安卓移动设备imei的32位大写的MD5编码，如1097787632DB8876D325C356285648D0（原始imei：861794042953717） 示例5： userIdType设置为128时，此时userId需要设置为苹果移动设备idfa的32位大写的MD5编码，如38D7C90186B1328F9418816DCC762A27（原始idfa：DCC77BDA-C2CA-4729-87D6-B7F65C8014D6） 示例6： userIdType设置为32768时，此时userId需要设置为安卓移动设备oaid，如7dafe7ff-bffe-a28b-fdf5-7fefdf7f7e85 示例7： userIdType设置为131072时，此时userId需要设置为安卓移动设备oaid的32位大写的MD5编码，如4967357D630E32E312A3A3EE0C5A128B（原始oaid：7dafe7ff-bffe-a28b-fdf5-7fefdf7f7e85）
userIdType对应的用户设备ID，传入此参数可获得个性化推荐结果，userIdType和userId需同时传入

channelId
Number
否
100001
渠道关系ID

ip
String
否
111.202.149.15
客户端ip

provinceId
Number
否
1
省Id

cityId
Number
否
2802
市Id

countryId
Number
否
54745
县Id

townId
Number
否
2032
镇Id

itemIds
String[]
否
[Q9Z2ZdyMsa9g7jpsfgQNVA0R_3SD7M7ISbsR0zCKPoF]
联盟商品ID集合(一次最多支持查询20个itemId)，为字符串数组类型，开发时记得加[]；仅sceneId=1时支持入参

sceneId
Number
是
1
场景ID，支持入参1,2；2需要权限申请

searchPosition
String
否
ENCohYvfyBJkAxdsyV6t1w==
查询索引位，首次入参传入空字符串，再次入参传入响应参数searchPosition；仅支持查询eliteType=22,23；pageSize最大20；需向cps-qxsq@jd.com申请权限

返回结果
queryResult
GoodsQueryResult
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
GoodsResp[]
是
无
数据明细

goodsResp
GoodsResp
是
无
数据明细

categoryInfo
CategoryInfo
是
无
类目信息

cid1
Number
是
6144
一级类目ID

cid1Name
String
是
珠宝首饰
一级类目名称

cid2
Number
是
12041
二级类目ID

cid2Name
String
是
木手串/把件
二级类目名称

cid3
Number
是
12052
三级类目ID

cid3Name
String
是
其他
三级类目名称

comments
Number
是
250
评论数

commissionInfo
CommissionInfo
是
无
佣金信息

commission
Number
是
22.68
佣金

commissionShare
Number
是
50
佣金比例

couponCommission
Number
否
12.68
券后佣金，（促销价-优惠券面额）*佣金比例

plusCommissionShare
Number
否
50
plus佣金比例，plus用户购买推广者能获取到的佣金比例

isLock
Number
否
1
是否锁定佣金比例：1是，0否

startTime
Number
否
1601364491000
计划开始时间（时间戳，毫秒）

endTime
Number
否
1601364491062
计划结束时间（时间戳，毫秒）

couponInfo
CouponInfo
是
无
优惠券信息，返回内容为空说明该SKU无可用优惠券

couponList
Coupon[]
是
无
优惠券集合

coupon
Coupon
是
无
优惠券明细

bindType
Number
是
3
券种类 (优惠券种类：0 - 全品类，1 - 限品类（自营商品），2 - 限店铺，3 - 店铺限商品券)

discount
Number
是
30
券面额

link
String
是
http://coupon.jd.com/ilink/couponActiveFront/front_index.action?XXXXXXX
券链接

platformType
Number
是
0
券使用平台 (平台类型：0 - 全平台券，1 - 限平台券)

quota
Number
是
39
券消费限额

getStartTime
Number
是
1532921782000
领取开始时间(时间戳，毫秒)

getEndTime
Number
是
1532921782000
券领取结束时间(时间戳，毫秒)

useStartTime
Number
是
1532921782000
券有效使用开始时间(时间戳，毫秒)

useEndTime
Number
是
1532921782000
券有效使用结束时间(时间戳，毫秒)

isBest
Number
是
1
最优优惠券，1：是；0：否，购买一件商品可使用的面额最大优惠券

hotValue
Number
是
5
券热度，值越大热度越高，区间:[0,10]

isInputCoupon
Number
否
1
入参couponUrl优惠券链接搜索对应的券，1 是 ，0 否

couponStyle
Number
否
0
优惠券分类 0：满减券，3：满折券，28: 每满减券

couponStatus
Number
否
-1
领取状态。0： 正常可领， -1 ：不可领取， 1： 已领取

timeCouponInfoList
TimeCouponInfo[]
否
时段券信息集合

timeCouponInfo
TimeCouponInfo
否
时段券信息

timeCouponBegin
String
否
00:00:00
时段券领取开始时间

timeCouponEnd
String
否
08:59:59
时段券领取结束时间

goodCommentsShare
Number
是
99
商品好评率

imageInfo
ImageInfo
是
无
图片信息

imageList
UrlInfo[]
是
无
图片合集

urlInfo
UrlInfo
是
无
图片合集

url
String
是
http://img14.360buyimg.com/ads/jfs/t22495/56/628456568/380476/9befc935/5b39fb01N7d1af390.jpg
图片链接地址，第一个图片链接为主图链接,修改图片尺寸拼接方法：/s***x***_jfs/，例如：http://img14.360buyimg.com/ads/s300x300_jfs/t22495/56/628456568/380476/9befc935/5b39fb01N7d1af390.jpg

whiteImage
String
否
https://img14.360buyimg.com/pop/jfs/t1/74611/40/9199/226994/5d6f1c60E211d7a9e/e69c31469897a95a.png
白底图

inOrderCount30Days
Number
是
6018
30天引单数量

isJdSale
Number
是
1
已废弃，请用owner

materialUrl
String
是
item.jd.com/26898778009.html
商品落地页，当入参场景2且有对应场景权限时，返回京东商品链接，materialUrl拼接为：item.jd.com/{skuId}.html；其他返回联盟商品链接，materialUrl拼接为：jingfen.jd.com/detail/{itemId}.html

priceInfo
PriceInfo
是
无
价格信息

price
Number
是
39.9
商品价格

lowestPrice
Number
否
14.9
促销价

lowestPriceType
Number
否
2
促销价类型，1：商品价格；2：拼购价格； 3：秒杀价格； 4：预售价格

lowestCouponPrice
Number
否
10.9
券后价（有无券都返回此字段，价格排序以此字段排序）

historyPriceDay
Number
否
100
历史最低价天数（例：当前券后价最近180天最低）

shopInfo
ShopInfo
是
无
店铺信息

shopName
String
是
XXXX旗舰店
店铺名称（或供应商名称）

shopId
Number
是
45619
商家Id

shopLevel
Number
否
3.5
店铺等级

shopLabel
String
否
1
1：京东好店 https://img12.360buyimg.com/schoolbt/jfs/t1/80828/19/2993/908/5d14277aEbb134d76/889d5265315e11ed.png

userEvaluateScore
String
否
9.46
用户评价评分（仅pop店铺有值）

commentFactorScoreRankGrade
String
否
高
用户评价评级（仅pop店铺有值）

logisticsLvyueScore
String
否
9.69
物流履约评分（仅pop店铺有值）

logisticsFactorScoreRankGrade
String
否
高
物流履约评级（仅pop店铺有值）

afterServiceScore
String
否
8.98
售后服务评分（仅pop店铺有值）

afsFactorScoreRankGrade
String
否
中
售后服务评级（仅pop店铺有值）

scoreRankRate
String
否
94.36
店铺风向标（仅pop店铺有值）

skuId
Number
是
26898778009
商品ID

skuName
String
是
便携式女士香水持久淡香小样 初见系列香水 遇见时光
商品名称

isHot
Number
是
1
已废弃，请勿使用

spuid
Number
是
3491692
spuid，其值为同款商品的主skuid

brandCode
String
是
7998
品牌code

brandName
String
是
悍途（Humtto）
品牌名

owner
String
是
g
g=自营，p=pop

pinGouInfo
PinGouInfo
是
无
拼购信息

pingouPrice
Number
是
39.9
拼购价格

pingouTmCount
Number
是
2
拼购成团所需人数

pingouUrl
String
是
https://wq.jd.com/pingou_api/GetAutoTuan?sku_id=35097232463 from=cps
拼购落地页url

pingouStartTime
Number
是
1546444800000
拼购开始时间(时间戳，毫秒)

pingouEndTime
Number
是
1548604800000
拼购结束时间(时间戳，毫秒)

videoInfo
VideoInfo
是
无
视频信息

videoList
Video[]
否
无
视频集合

video
Video
否
无
视频明细

width
Number
是
400
宽

high
Number
是
300
高

imageUrl
String
是
https://img.300hu.com/4c1f7a6atransbjngwcloud1oss/44128edd173016898433773569/imageSampleSnapshot/1555986468_406717890.100_2756.jpg
视频图片地址

videoType
Number
是
1
1:主图，2：商详

playUrl
String
是
https://vod.https://vod.300hu.com/4c1f7a6atransbjngwcloud1oss/44128edd173016898433773569/v.f20.mp4?dockingId=2bc88c56-a44d-45c4-99b4-d9b68557e4e9storageSource=3.com/4c1f7a6atransbjngwcloud1oss/44128edd173016898433773569/v.f20.mp4?dockingId=2bc88c56-a44d-45c4-99b4-d9b68557e4e9storageSource=3
播放地址

playType
String
是
high
low：标清，high：高清

duration
Number
否
10
时长(单位:s)

commentInfo
CommentInfo
否
无
评价信息

commentList
Comment[]
是
无
评价集合

comment
Comment
是
无
评价列表

content
String
是
不错，是正品
评价内容

imageList
UrlInfo[]
是
无
图片集合【废弃】

urlInfo
UrlInfo
是
无
图片集合【废弃】

url
String
是
http://img14.360buyimg.com/ads/jfs/t22495/56/628456568/380476/9befc935/5b39fb01N7d1af390.jpg
图片链接地址【废弃】

jxFlags
Number[]
否
[1,2,3]
京喜商品类型，1京喜、2京喜工厂直供、3京喜优选（包含3时可在京东APP购买）

documentInfo
DocumentInfo
否
段子信息
商品段子信息，emoji表情等

document
String
是
温和亲肤的配方 洁净面部污垢
描述文案

discount
String
否
29.9碧素堂氨基酸洗面奶
优惠力度文案

bookInfo
BookInfo
否
无
图书信息

isbn
String
否
9787515515564
图书编号

publisherName
String
否
某出版社
出版商名称

authorName
String
否
刘某某
作者名称

bookDesc
String
否
内容简介
内容摘要

bookName
String
否
中文图书名称
图书中文名称

foreignBookName
String
否
英文图书名称
图书英文名称

specInfo
SpecInfo
否
无
扩展信息

size
String
否
1
尺寸

color
String
否
白色
颜色

spec
String
否
无
自定义属性

specName
String
否
无
自定义属性名称

isFreeShipping
Number
否
1
是否包邮(1:是,0:否,2:自营商品遵从主站包邮规则)

stockState
Number
否
1
库存状态：1有货、0无货（供tob选品场景参考，toc场景不适用）

eliteType
Number[]
否
[17]
资源位17：极速版商品

forbidTypes
Number[]
否
[0,10,11]
0普通商品，10微信京东购物小程序禁售，11微信京喜小程序禁售

deliveryType
Number
否
1
京东配送 1：是，0：不是

skuLabelInfo
SkuLabelInfo
否
无
商品标签

is7ToReturn
Number
否
1
0：不支持； 1或null：支持7天无理由退货； 2：支持90天无理由退货； 4：支持15天无理由退货； 6：支持30天无理由退货；

fxg
Number
否
1
1：放心购商品

fxgServiceList
CharacteristicServiceInfo[]
否
放心购商品子标签集合

characteristicServiceInfo
CharacteristicServiceInfo
否
放心购商品子标签，此字段值可能为空

serviceName
String
否
破损包退换
服务名称

promotionLabelInfoList
PromotionLabelInfo[]
否
商品促销标签集

promotionLabelInfo
PromotionLabelInfo
否
商品促销标签

promotionLabel
String
否
满2件，总价打8折
商品促销文案

lableName
String
否
满折
促销标签名称【废弃】

startTime
Number
否
1608998400000
促销开始时间

endTime
Number
否
1609862399000
促销结束时间

promotionLableId
Number
否
5000125161
促销ID【废弃】

labelName
String
否
满折
促销标签名称

promotionLabelId
Number
否
5000125161
促销ID

provinceNameList
String[]
否
[天津]
促销生效区域（国补）--省中文名

subType
Number
否
9105
促销类型（国补），9105：以旧换新国补，9107：购新立减国补

rebate
Number
否
0.5
促销优惠比例（国补），0到1之间的小数

topDiscount
Number
否
2000
最大优惠金额（国补）

secondPriceInfoList
SecondPriceInfo[]
否
双价格

secondPriceInfo
SecondPriceInfo
否
双价格信息

secondPriceType
Number
否
2
双价格类型：2:plus会员价格，9:学生价，18:新人价

secondPrice
Number
否
8.8
价格

seckillInfo
SeckillInfo
否
秒杀信息

seckillOriPrice
Number
否
36.9
秒杀价原价

seckillPrice
Number
否
26.8
秒杀价

seckillStartTime
Number
否
1574474399000
秒杀开始时间(时间戳，毫秒)

seckillEndTime
Number
否
1574388000000
秒杀结束时间(时间戳，毫秒)

preSaleInfo
PreSaleInfo
否
预售信息

currentPrice
Number
否
100
预售价格

earnest
Number
否
15
订金金额（定金不能超过预售总价的20%）

preSalePayType
Number
否
1
预售支付类型：1.仅全款 2.定金、全款均可 5.一阶梯仅定金

discountType
Number
否
1
1: 定金膨胀 2: 定金立减

depositWorth
Number
否
10
定金膨胀金额（定金可抵XXX）【废弃】

preAmountDeposit
Number
否
10
立减金额

preSaleStartTime
Number
否
1546444800000
定金开始时间

preSaleEndTime
Number
否
1546444800000
定金结束时间

balanceStartTime
Number
否
1546444800000
尾款开始时间

balanceEndTime
Number
否
1546444800000
尾款结束时间

shipTime
Number
否
1546444800000
预计发货时间

preSaleStatus
Number
否
1
预售状态（0 未开始；1 预售中；2 预售结束；3 尾款进行中；4 尾款结束）

amountDeposit
Number
否
10
定金膨胀金额（定金可抵XXX）

reserveInfo
ReserveInfo
否
预约信息

price
Number
否
15
预约价格

type
Number
否
1
预约类型： 1：预约购买资格（仅预约的用户才可以进行购买）； 5：预约抽签（仅中签用户可购买）

status
Number
否
1
1：等待预约 2：预约中 3：等待抢购/抽签中 4：抢购中 5：抢购结束

startTime
Number
否
1601364491000
预定开始时间

endTime
Number
否
1601364491000
预定结束时间

panicBuyingStartTime
Number
否
1601364491000
抢购开始时间

panicBuyingEndTime
Number
否
1601364491000
抢购结束时间

isOversea
Number
否
1
是否全球购商品 1：是

companyType
Number
否
2
2：POP自然人小店

purchasePriceInfo
PurchasePriceInfo
否
到手价明细

code
Number
否
200
返回码

message
String
否
成功
返回消息

purchasePrice
Number
否
30.9
到手价

thresholdPrice
Number
否
39.9
门槛价金额，计算到手价的基准价

basisPriceType
Number
否
1
依据的价格类型，1、京东价 ,2 Plus价，7 粉丝价，8 新人价，9学生价，10 陪伴计划价（双价格新增）

promotionLabelInfoList
PromotionLabelInfo[]
否
商品促销标签集

promotionLabelInfo
PromotionLabelInfo
否
商品促销标签

promotionLabel
String
否
满2件，总价打8折
商品促销文案

startTime
Number
否
1608998400000
促销开始时间

endTime
Number
否
1609862399000
促销结束时间

promotionLabelId
Number
否
5000125161
促销ID

labelName
String
否
满折
促销标签名称

provinceNameList
String[]
否
[天津]
该字段为预留字段，尚未启用，请勿使用

subType
Number
否
9105
该字段为预留字段，尚未启用，请勿使用

rebate
Number
否
0.5
该字段为预留字段，尚未启用，请勿使用

topDiscount
Number
否
2000
该字段为预留字段，尚未启用，请勿使用

couponList
Coupon[]
否
优惠券集合

coupon
Coupon
否
优惠券明细

bindType
Number
否
3
券种类 (优惠券种类：0 - 全品类，1 - 限品类（自营商品），2 - 限店铺，3 - 店铺限商品券)

discount
Number
否
30
券面额

link
String
否
http://coupon.jd.com/ilink/couponActiveFront/front_index.action?XXXXXXX
券链接

platformType
Number
否
0
券使用平台 (平台类型：0 - 全平台券，1 - 限平台券)

quota
Number
否
39
券消费限额

couponStyle
Number
否
0
优惠券分类 0：满减券，3：满折券，28: 每满减券

couponStatus
Number
否
-1
领取状态。0： 正常可领， -1 ：不可领取， 1： 已领取

timeCouponInfoList
TimeCouponInfo[]
否
时段券信息集合

timeCouponInfo
TimeCouponInfo
否
时段券信息

timeCouponBegin
String
否
00:00:00
时段券领取开始时间

timeCouponEnd
String
否
08:59:59
时段券领取结束时间

itemId
String
否
Q9Z2ZdyMsa9g7jpsfgQNVA0R_3SD7M7ISbsR0zCKPoF
联盟商品ID

activityCardInfo
ActivityCardInfo
否
超市购物卡明细

amount
Number
否
100
超市卡金额

activityType
Number
否
3
活动类型，购物返卡：3

expireDay
Number
否
5
超市卡有效天数

smartDocumentInfoList
SmartDocumentInfo[]
否
GPT算法智能生成的推广文案集合,内含多个风格

smartDocumentInfo
SmartDocumentInfo
否
智能文案明细

documentType
Number
否
2
智能文案类型，1：最优推荐，2：知乎风格， 3：小红书风格， 4：社群风格

documentName
String
否
知乎风格
文案名称

document
String
否
内衣洗衣液，深层祛渍，植物配方。限时24元！
智能文案内容

kaAdowner
Number
否
1
是否星选商家商品。1：是

oriItemId
String
否
Q9Z2ZdyMsa9g7jpsfgQNVA0R_3SD7M7ISbsR0zCKPoF
原始入参ItemId

callerItemId
String
否
UOWr2TlQWFp0JAokyWv5S1fP_309lO7rgdWouKuKnsF
工具商联盟商品ID

skuTagList
SkuTagInfo[]
否
联盟标签

skuTagInfo
SkuTagInfo
否
联盟标签明细

type
Number
否
3
标签类型

index
Number
否
1
优先级顺序（越小越优先）

name
String
否
7天无理由退货
标签名称

inOrderCount30DaysSku
Number
否
1000
30天引单数量(sku维度)

specialSkuUrlInfo
SpecialSkuUrlInfo
否
频道页信息

skuUrlType
Number
否
1
频道页链接类型，1：秒杀专享价

skuUrl
String
否
https://pro.m.jd.com/mall/active/Md9FMi1pJX...
频道页地址

searchPageInfo
SearchPageInfo
否
该字段信息已禁用，请勿获取和使用

searchPageUrl
String
否
无
该字段信息已禁用，请勿获取和使用

totalCount
Number
是
6186186
有效商品总数量，上限1w

hotWords
String
否
牛奶,家电
日常top10的热搜词，按小时更新

similarSkuList
Number[]
否
[11144230,11993134]
相似推荐商品skuId集合

similarItemIdList
String[]
否
['Q9Z2ZdyMsa9g7jpsfgQNVA0R_3SD7M7ISbsR0zCKPoF']
相似推荐联盟商品ID集合

searchPosition
String
否
ENCohYvfyBJkAxdsyV6t1w==
查询索引位，再次查询须入参该值

请求示例
phpjavapython
import jd.api
import json
from jd.api.rest.UnionOpenGoodsQueryRequest import UnionOpenGoodsQueryRequest
import datetime
jd.setDefaultAppInfo(appkey, secret)
a = UnionOpenGoodsQueryRequest(url,port)
a.goodsReqDTO=""
a.version = "1.0"
try:
f= a.getResponse(sessionkey)
print(json.dumps(f, ensure_ascii=False))
except Exception,e:
print(e)
响应示例
jsonxml
{
"jd_union_open_goods_query_responce": {
"queryResult": {
"similarSkuList": [
11144230,
11993134
],
"hotWords": "牛奶,家电",
"code": "200",
"data": {
"goodsResp": {
"bookInfo": {
"foreignBookName": "英文图书名称",
"publisherName": "某出版社",
"authorName": "刘某某",
"isbn": "9787515515564",
"bookName": "中文图书名称",
"bookDesc": "内容简介"
},
"materialUrl": "item.jd.com/26898778009.html",
"documentInfo": {
"document": "温和亲肤的配方 洁净面部污垢",
"discount": "29.9碧素堂氨基酸洗面奶"
},
"imageInfo": {
"whiteImage": "https://img14.360buyimg.com/pop/jfs/t1/74611/40/9199/226994/5d6f1c60E211d7a9e/e69c31469897a95a.png",
"imageList": {
"urlInfo": {
"url": "http://img14.360buyimg.com/ads/jfs/t22495/56/628456568/380476/9befc935/5b39fb01N7d1af390.jpg"
}
}
},
"pinGouInfo": {
"pingouEndTime": "1548604800000",
"pingouPrice": "39.9",
"pingouTmCount": "2",
"pingouUrl": "https://wq.jd.com/pingou_api/GetAutoTuan?sku_id=35097232463 from=cps",
"pingouStartTime": "1546444800000"
},
"forbidTypes": [
0,
10,
11
],
"skuLabelInfo": {
"is7ToReturn": "1",
"fxg": "1",
"fxgServiceList": {
"characteristicServiceInfo": {
"serviceName": "破损包退换"
}
}
},
"activityCardInfo": {
"amount": "100",
"expireDay": "5",
"activityType": "3"
},
"callerItemId": "UOWr2TlQWFp0JAokyWv5S1fP_309lO7rgdWouKuKnsF",
"skuName": "便携式女士香水持久淡香小样 初见系列香水 遇见时光",
"oriItemId": "Q9Z2ZdyMsa9g7jpsfgQNVA0R_3SD7M7ISbsR0zCKPoF",
"priceInfo": {
"lowestPrice": "14.9",
"lowestCouponPrice": "10.9",
"price": "39.9",
"historyPriceDay": "100",
"lowestPriceType": "2"
},
"stockState": "1",
"isOversea": "1",
"smartDocumentInfoList": {
"smartDocumentInfo": {
"documentType": "2",
"document": "内衣洗衣液，深层祛渍，植物配方。限时24元！",
"documentName": "知乎风格"
}
},
"searchPageInfo": {
"searchPageUrl": "无"
},
"specInfo": {
"size": "1",
"color": "白色",
"specName": "无",
"isFreeShipping": "1",
"spec": "无"
},
"skuTagList": {
"skuTagInfo": {
"name": "7天无理由退货",
"index": "1",
"type": "3"
}
},
"specialSkuUrlInfo": {
"skuUrl": "https://pro.m.jd.com/mall/active/Md9FMi1pJX...",
"skuUrlType": "1"
},
"spuid": "3491692",
"commissionInfo": {
"isLock": "1",
"commissionShare": "50",
"plusCommissionShare": "50",
"commission": "22.68",
"startTime": "1601364491000",
"couponCommission": "12.68",
"endTime": "1601364491062"
},
"isJdSale": "1",
"skuId": "26898778009",
"brandCode": "7998",
"owner": "g",
"kaAdowner": "1",
"shopInfo": {
"logisticsLvyueScore": "9.69",
"shopLevel": "3.5",
"userEvaluateScore": "9.46",
"scoreRankRate": "94.36",
"afterServiceScore": "8.98",
"shopName": "XXXX旗舰店",
"shopLabel": "1",
"afsFactorScoreRankGrade": "中",
"shopId": "45619",
"logisticsFactorScoreRankGrade": "高",
"commentFactorScoreRankGrade": "高"
},
"brandName": "悍途（Humtto）",
"comments": "250",
"seckillInfo": {
"seckillOriPrice": "36.9",
"seckillPrice": "26.8",
"seckillStartTime": "1574474399000",
"seckillEndTime": "1574388000000"
},
"couponInfo": {
"couponList": {
"coupon": {
"timeCouponInfoList": {
"timeCouponInfo": {
"timeCouponEnd": "08:59:59",
"timeCouponBegin": "00:00:00"
}
},
"isInputCoupon": "1",
"useStartTime": "1532921782000",
"bindType": "3",
"link": "http://coupon.jd.com/ilink/couponActiveFront/front_index.action?XXXXXXX",
"platformType": "0",
"discount": "30",
"hotValue": "5",
"isBest": "1",
"couponStyle": "0",
"useEndTime": "1532921782000",
"getEndTime": "1532921782000",
"quota": "39",
"couponStatus": "-1",
"getStartTime": "1532921782000"
}
}
},
"preSaleInfo": {
"depositWorth": "10",
"balanceEndTime": "1546444800000",
"shipTime": "1546444800000",
"preSalePayType": "1",
"currentPrice": "100",
"preSaleStartTime": "1546444800000",
"balanceStartTime": "1546444800000",
"preSaleEndTime": "1546444800000",
"preSaleStatus": "1",
"amountDeposit": "10",
"discountType": "1",
"earnest": "15",
"preAmountDeposit": "10"
},
"companyType": "2",
"videoInfo": {
"videoList": {
"video": {
"duration": "10",
"high": "300",
"playType": "high",
"videoType": "1",
"imageUrl": "https://img.300hu.com/4c1f7a6atransbjngwcloud1oss/44128edd173016898433773569/imageSampleSnapshot/1555986468_406717890.100_2756.jpg",
"width": "400",
"playUrl": "https://vod.https://vod.300hu.com/4c1f7a6atransbjngwcloud1oss/44128edd173016898433773569/v.f20.mp4?dockingId=2bc88c56-a44d-45c4-99b4-d9b68557e4e9storageSource=3.com/4c1f7a6atransbjngwcloud1oss/44128edd173016898433773569/v.f20.mp4?dockingId=2bc88c56-a44d-45c4-99b4-d9b68557e4e9storageSource=3"
}
}
},
"secondPriceInfoList": {
"secondPriceInfo": {
"secondPriceType": "2",
"secondPrice": "8.8"
}
},
"deliveryType": "1",
"goodCommentsShare": "99",
"categoryInfo": {
"cid1Name": "珠宝首饰",
"cid2Name": "木手串/把件",
"cid2": "12041",
"cid3Name": "其他",
"cid3": "12052",
"cid1": "6144"
},
"inOrderCount30DaysSku": "1000",
"inOrderCount30Days": "6018",
"reserveInfo": {
"price": "15",
"panicBuyingEndTime": "1601364491000",
"startTime": "1601364491000",
"endTime": "1601364491000",
"type": "1",
"status": "1",
"panicBuyingStartTime": "1601364491000"
},
"commentInfo": {
"commentList": {
"comment": {
"imageList": {
"urlInfo": {
"url": "http://img14.360buyimg.com/ads/jfs/t22495/56/628456568/380476/9befc935/5b39fb01N7d1af390.jpg"
}
},
"content": "不错，是正品"
}
}
},
"itemId": "Q9Z2ZdyMsa9g7jpsfgQNVA0R_3SD7M7ISbsR0zCKPoF",
"purchasePriceInfo": {
"thresholdPrice": "39.9",
"code": "200",
"basisPriceType": "1",
"couponList": {
"coupon": {
"timeCouponInfoList": {
"timeCouponInfo": {
"timeCouponEnd": "08:59:59",
"timeCouponBegin": "00:00:00"
}
},
"quota": "39",
"bindType": "3",
"link": "http://coupon.jd.com/ilink/couponActiveFront/front_index.action?XXXXXXX",
"platformType": "0",
"couponStatus": "-1",
"discount": "30",
"couponStyle": "0"
}
},
"purchasePrice": "30.9",
"message": "成功",
"promotionLabelInfoList": {
"promotionLabelInfo": {
"provinceNameList": "[天津]",
"promotionLabel": "满2件，总价打8折",
"rebate": "0.5",
"startTime": "1608998400000",
"subType": "9105",
"topDiscount": "2000",
"endTime": "1609862399000",
"labelName": "满折",
"promotionLabelId": "5000125161"
}
}
},
"eliteType": [
17
],
"promotionLabelInfoList": {
"promotionLabelInfo": {
"provinceNameList": "[天津]",
"promotionLabel": "满2件，总价打8折",
"lableName": "满折",
"rebate": "0.5",
"promotionLableId": "5000125161",
"startTime": "1608998400000",
"subType": "9105",
"topDiscount": "2000",
"endTime": "1609862399000",
"labelName": "满折",
"promotionLabelId": "5000125161"
}
},
"isHot": "1",
"jxFlags": [
1,
2,
3
]
}
},
"similarItemIdList": "['Q9Z2ZdyMsa9g7jpsfgQNVA0R_3SD7M7ISbsR0zCKPoF']",
"searchPosition": "ENCohYvfyBJkAxdsyV6t1w==",
"message": "success",
"totalCount": "6186186"
}
}
}