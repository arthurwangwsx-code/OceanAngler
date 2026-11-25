# 庸鲽长鳍与躯干绑定候选

实现 IN_PROGRESS；源/导入逻辑 PASS，严格GPU首帧一致性 PARTIAL，美术/科学/用户/儿童/真实设备未验收，apply继续开放。本轮不更改游玩HUD、钓鱼规则、积分或存档结构。

旧长鳍缺独立波动；图审另发现旧下颌权重函数把鱼身后半段绑到head，原LOD0尾段2464顶点在swim/burst/hooked中位移为0，鳍根有3–43对中心离开静止躯干。增加两侧各17控制、总42骨/每顶点四权重，保留根部内侧四分之一，并限定把Skin、X>−0.55的顶点改绑原脊柱。三LOD分别4704/1069/482个Skin顶点修复；口腔、眼睛、其他鳍、原八骨、七套身体/下颌曲线、三LOD几何/UV与八张贴图保持。

新版独立源检查1014条件通过，旧口腔/核心160条件通过；所有抽检七动作中的130对根中心保持在变形鱼身内，swim/burst/hooked后段实际移动约7.63/13.89/16.86厘米。6项Editor及9项PlayMode通过，包含真实导入后半身移动、长鳍消融、下颌牙龈与图鉴暂停/继续。重复编辑检查最大权重差3.73e−9，未改写文件。

GPU E保留473张Metal HDR、4×MSAA原图：七动作各61姿态，每段60对相邻帧均变化；四个固定身体/关闭鳍控制的图像对分别5275/7635/5181/7684像素变化。每张完整变形网格均在画框内，七段两秒预览为30次作者采样/秒，不能当运行FPS。已查看动作/控制联系图与burst23整图，旧后身接缝已合拢；眼部、薄边与整体自然观感仍为候选。

严格首/重30对中29对零差异，首个idle仍有18像素、最大单通道1级差异，独立检查保持FAIL、GPU回执保持PARTIAL。CPU顶点/法线摘要完全相同；按[Unity手动重复蒙皮渲染文档](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/skinnedmeshrenderer/forcematrixrecalculationperrender)启用fixture的矩阵重算也未消除。成因未唯一定位，未丢弃首图、未放宽阈值，不以后续稳定帧替代首帧。

保留初版远距权重删除失败、headless剔除失败、GPU A旧姿态批次、B裁切/首图差异、C虽局部通过却漏掉后身误绑、D/E首图差异、后身受控红测，以及参考捕获中低效重复集合计算被终止的回执。新版基线来自原始旧源，前端和其他鳍权重按新明确范围比对；不掩盖后身绑定的有意变化。

模拟器同包c5bdaa5/游戏摘要5edc3462/App摘要f935d1dd，ARM64与1/2设备族编译并核对安装成功。手机26条/4570分、平板21条/3868分真实旧捕获副本，通过已发现图鉴的游动、静止、继续、1.3倍缩放；每端34控制、17实际Quaternion样本，暂停漂移0，继续位移21.11/21.09度。两端各20装备与12模型提示检查通过，模型操作前后存档字节相同；整段装备选择只改变四个装备字段，积分、捕获、设置、海域、观察完全保持。独立验证20张原生PNG；两段实际录屏约3.85秒，各抽看6个四分之一秒采样并查看暂停/继续原图，只有游戏画面，无音频/调色/曝光编辑，30fps只为导出采样。180份旧进度相关文件（93主文件）逐字节保持。

未生成新捕获、未重跑完整成长或七套原生动作；真机、OS触控与成品美术独立开放。平板放大后下侧长鳍靠近模型控件、眼部和材质仍需后续打磨。不操作个人iPhone，不上传TestFlight；Apple旧build8不等于本轮本地0.1.0(8)。源/预览均为自有资产，研究论文及设备原始日志只留私下目录。设计与参考适用范围见[方案](../docs/39-halibut-fin-motion.md)，原任务4.11不勾选。

审阅：[手机实际录屏](../art/previews/halibut-fin-motion-native/phone-journal-swim.mp4)、[平板实际录屏](../art/previews/halibut-fin-motion-native/tablet-journal-swim.mp4)、[七动作联系图](../art/previews/halibut-fin-motion/cycles-contact.jpg)、[固定鱼身的鳍控制对照](../art/previews/halibut-fin-motion/ablation-contact.jpg)。源码提交c5bdaa5对应[CI已通过](https://github.com/arthurwangwsx-code/OceanAngler/actions/runs/45559760884)，代码仍在integration候选分支，master保持原基线。

测量完整性：本轮52条已结束记录与开始项、当前apply attempt、回执SHA及串行区间核对，旧对象未改；检查自身另有完成回执，新增总计53条且无悬挂开始项。保留11条失败/终止，其中参考捕获SIGTERM不当成功；历史独立审计仍INCOMPLETE，完整产品与apply继续开放。
