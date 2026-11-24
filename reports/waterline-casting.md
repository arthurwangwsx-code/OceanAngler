# 水面抛竿与沉浸镜头候选

实际钓具已移到各海域水面上方；准备时拟饵挂在竿尖下，抛出后先落到水面，再下沉到原有鱼群空间。准备／抛竿展示水面，落水后以短暂海色淡出／淡入切换水下，放流离开结束后才返回水面。设置新增“固定水下镜头”，减弱动态也固定水下；普通游玩仍是两个边缘按钮和阶段必要仪表。完整美术与产品仍PARTIAL，已发布TestFlight8未更新，未操作实体iPhone。约束见[设计与验收说明](../docs/35-waterline-casting.md)。

最终修正源码冻结`1317bc4b42958c2d94cb2c76854cc1765a851b3b`。278份已跟踪的模型、骨架、PBR、源文件与Prefab相对于cf0c9c9逐文件Git内容摘要相同；改变的是运行时姿态／轨迹、镜头、落水着色响应、设置与验收入口，不将资产保持等同于成品美术通过。

38项定向Editor与6项定向PlayMode通过，涵盖三海域／左右手机制在水面上方、实际英雄鱼网格嘴外鱼线、先落水再下沉的端点连续性、旧设置兼容、非有限参数、真实UI设置开关／恢复、菜单／失焦／放流及原最小HUD。初次夹具未初始化机制、放流未结束就要求返回水面的两个失败均保留；修正的是夹具，不缩短实际放流动画。

E轮GPU是Metal／Medium／Half HDR／4×MSAA，三海域、左右手、准备／抛出／落水／水下共24姿态、48张首绘与重绘。卷线器关键点距画面边缘至少约19.9%；23对RGB差0，首个海湾准备姿态仍有232像素差、最大14/255，严格状态为PARTIAL，非零回执和首次原图保留。前轮D是86像素／最大3/255，不能替代当前E的结果。接触表、选定当前原图及此前贴底构图已查看，未人工逐张看完48原图。

首个a5e0505模拟器A包19帧正常流程通过、放流196分，但实际首屏水面呈平色，视觉FAIL。原因是尚未落水的无穷时间进入着色器，即使强度为零仍可产生非数。真实红测复现GPU输入Infinity后，将无效／未开始／已过期状态统一为有限的关闭参数；新增Player实际材质块读回检查。E轮准备姿态也输入真实未开始哨兵，水面已恢复。首个GPU夹具还因缺少相机URP数据失败，其已知质量设置副作用按原始字节精确恢复；异常清理入口已修正。

最终B包摘要`10ffafdaf18b24889843568f30bbc844d1901fb61194ecc673beb8045ccfd7a6`，源码摘要`dfd49d5f4d924ba6de433f43462f6ac4a4afb67600eedc963edeccf68d081bc8`，仅本地ARM64模拟器。iPhone／iPad同包各19帧、正常教学放流195／220分通过，实际水面材质参数有限、镜头开关／减弱动态往返与设置恢复、左右手／暂停／失焦／未记录退出通过，HUD最高12.228%／4.526%，中央区域无普通HUD遮挡。双接触表、两张平板原尺寸图和横屏屏录0／1／2／3秒帧已查看；首屏波纹／倒影恢复，水面至水下场景确实切换。未把选帧审阅当作完整动态自然观感通过。机器结果见[JSON](waterline-casting.json)。

第一次B端流程因主机磁盘写满，在左手首页截图时中断，192分／一条真实鱼的存档校验仍有效。原生流程和录像均已终止；iSpec JSON状态完整，但完成记录及回执未写入，只保留实际启动`539d2470-da3f-4a9d-941c-e4c4a34b48da`，不补造完成时间或回执。仅删除本项目已结束的Xcode中间缓存与Unity Library/Bee可重建缓存，冻结App摘要前后相同，源文件、签名归档／IPA、导出、证据、存档和其他项目均保留。使用新隔离标签及同一已安装二进制复验通过，未重装或重放旧记录。

A更新后36份旧QA主文件字节相同，B更新后37份相同；重试前纳入中断流程的有效记录，共38份原字节在最终双端后再次通过。全部已完成测量的实际回执哈希、绑定、历史对象及串行区间另行核对；该磁盘事件留下的一条启动无完成记录和旧历史缺口均保持INCOMPLETE。录像初次额外旋转被否决，最终依据原始容器实际方向矩阵规范为横屏；原录像和否决产物均保留。

- [三海域24姿态](../art/previews/waterline-casting/three-habitat-contact.png)
- [水面准备](../art/previews/waterline-casting/bay-surface-ready.png)、[抛出](../art/previews/waterline-casting/bay-surface-cast.png)、[落水](../art/previews/waterline-casting/bay-impact.png)、[水下](../art/previews/waterline-casting/bay-underwater.png)
- [近海水面](../art/previews/waterline-casting/channel-surface.png)、[陆坡水面](../art/previews/waterline-casting/slope-surface.png)
- [早期贴底构图](../art/previews/waterline-casting/rejected-bottom-framing.png)、[A包失败首屏](../art/previews/waterline-casting/rejected-native-idle-water.png)
- [实际模拟器抛竿屏录](../art/previews/waterline-casting/native-phone-casting.mp4)、[手机19帧](../art/previews/waterline-casting/native-phone-contact.png)、[平板19帧](../art/previews/waterline-casting/native-tablet-contact.png)
- [修正后手机首屏](../art/previews/waterline-casting/native-phone-ready.png)、[平板水面](../art/previews/waterline-casting/native-tablet-ready.png)、[平板控线](../art/previews/waterline-casting/native-tablet-fighting.png)

静态三海域图统一使用同一英雄鱼作几何诊断，不是该物种分布或全内容证明。深海下沉时间经过游戏压缩；水面反射仍为成本受限近似，P0未制作手部，第一人称持握和整体水面／地形／其他模型仍为候选品质。截图和自动脚本会影响屏录墙钟节奏，不把视频编码帧率当作运行性能。当前没有新20轮全内容、900秒休息或真实双设备20分钟结果；用户／科学／儿童、完整自然观感及拟真A1–A3仍未验收。4.7、apply与完整目标保持未完成。

已知文案待改：水下等待时，禁用动作按钮仍沿用“观察海面”；阶段提示已经说明等待咬钩，后续应统一措辞并验证，不能将当前候选视为最终儿童交互验收。
