# 英雄鱼动作节律候选

源、Editor与双端原生模拟器定向通过；完整拟真美术仍PARTIAL。最终本地包冻结`5a5936c3e8c3bafc22ac5256977de002ca7c37ba`，源摘要`e71068da98d387bbfe12ccac8a963a4db7f41d3b99a0ce6aa00eca78578fead4`、包摘要`48ef2430a4d4786b8e6401c98fe03c6846af665efd6293ddf1031e3a91fcba9c`。已发布TestFlight8保持冻结`1bdfa785237701ab2c7675031142d526f66c0e28`，本轮没有更新TestFlight或操作实体iPhone。机器结果见[JSON](bluefin-periodic-motion.json)，重建及实际复核入口见[制作说明](../docs/34-hero-motion-continuity.md)。

本轮修订胸鳍整数往复、转向平滑包络与开口闭合曲线，使用明确的源关键帧切线。全部三档源几何/UV/权重、12骨骼静态矩阵与九张PNG保持；三档24664/9864/3698，源189条件、72处薄壁与七动作35姿态通过。动作独立更新不重新减面，完整源资产重新生成未重跑。

实际原始FBX导入仍保留了采样/压缩接缝，不能用源通过替代游戏播放通过。Controller现在引用从自有导入动作派生的七个原生`.anim`文件，六个循环动作闭合值与端点切线保持一致、非极值处保留尾摆速度，放流仍不循环。七文件合计1,416,592字节是序列化源文件占用，不是运行时内存。10项定向Editor及两个动画/一个整合场景PlayMode通过；最终诊断字段后的复验另记。未以先前发行全量、成长或休息结果充当本源码结论。

| 选定通道 | 旧播放控制 | 新播放候选 |
| --- | ---: | ---: |
| 上钩胸鳍 | 25.463 | 1.255 |
| 转向尾骨 | 10.913 | 0.239 |
| 上钩下颌 | 91.714 | 10.671 |

表格是归一播放速度下、循环两侧各5毫秒窗口的角速度向量差（度/秒），包含加速度和采样误差，不是严格瞬时速度不连续量，也不代表每个通道都下降。实际循环端点值与切线另外检查，不以有限窗口数值必须为零作为美术或科学门槛。

Medium/Metal/Half HDR/4×MSAA的D轮包含640帧动作序列、八张单独保留的首次绘制和八张重复图；八组重复RGB差0。每帧用实际变形网格投影和像素边界双检，最小边距约12.8%，没有靠裁剪画面隐藏鱼鳍。两张接触表和两张当前原尺寸图、一个否决原图已查看，未逐张看完656原图，也未将视频生成为完整动态自然观感审阅。

对照视频均为离线Clip采样，20fps编码、四秒两个周期，不是游戏帧率或Player屏录。左列为调整前，右列为候选；上行为游动，下行为上钩。

- [侧面动作对照](../art/previews/bluefin-periodic-motion/comparison-side.mp4)、[俯视动作对照](../art/previews/bluefin-periodic-motion/comparison-top.mp4)
- [侧面选帧](../art/previews/bluefin-periodic-motion/contact-side.png)、[俯视选帧](../art/previews/bluefin-periodic-motion/contact-top.png)
- [当前上钩原图](../art/previews/bluefin-periodic-motion/candidate-hooked-side.png)、[当前俯视原图](../art/previews/bluefin-periodic-motion/candidate-swim-top.png)

失败保留：A/B捕获与重复检查通过，但俯视镜头实际裁尾，人工否决；[原裁尾图](../art/previews/bluefin-periodic-motion/rejected-top-framing.png)保留。C的新增检查错误地重复计算缩放，非零回执及图保留；依据BakeMesh契约改为补偿缩放后D通过，旧俯视控制的投影边距约-23%。首次编码因本机ffmpeg缺少drawtext失败，改为自有帧合成标签，没有安装或更换全局工具。

首个6205a89原生A包双端各15帧正常流程通过，落盘224/245分；iPhone实际屏录却发现收鱼进度到两位数时换行截断，因此该包的视觉仅PARTIAL。新增回归先真实失败于10%：文字需要78点，原容器只有74点。修正保持字号17、228×48点总区域及8点间距，把张力/收鱼宽度调整为128/92，危险刻度仍为80%。左右手各0–100%的文字、菜单、休息边界、动画与共享世界六项定向PlayMode通过；边界夹具不落盘合成进度，原生流程只检查自然经过的真实数字。

最终5a5936c的同一ARM64包在iPhone/iPad模拟器各15帧通过，正常教学放流232/180分，最大HUD占用12.228%/4.526%，各阶段中央区域无UI遮挡、两个当前按钮，左右手菜单/暂停/失焦和未记录退出保护通过。所有实际观察的仪表文字均完整，实际部署七动作名称和六循环/放流非循环标记核实；源码CI通过。双端接触表和四张原尺寸图已查看，其中实际屏录1秒/3秒帧分别显示7%/22%，未将四秒视频生成或两个选帧当作完整动态自然观感审阅。

- [实际iPhone模拟器控线屏录](../art/previews/bluefin-periodic-motion/native-phone-fighting.mp4)、[两位数进度原图](../art/previews/bluefin-periodic-motion/native-phone-two-digit-progress.png)
- [首页与图形入口](../art/previews/bluefin-periodic-motion/native-phone-home.png)、[平板控线原图](../art/previews/bluefin-periodic-motion/native-tablet-fighting.png)
- [手机15帧](../art/previews/bluefin-periodic-motion/native-phone-contact.png)、[平板15帧](../art/previews/bluefin-periodic-motion/native-tablet-contact.png)

首次更新32份旧QA主文件字节相同，HUD更新前扩展至34份并于更新后逐字节再次通过；按当前容器及归属核实，无恢复或重放。仅清理本项目已结束的Xcode中间缓存，原build7/8签名归档与IPA摘要前后相同，A模拟器包在后一次缓存清理前后摘要相同；其他项目、个人设备和已有存档未清理，du数值不代表独占APFS回收量。新增测量的历史对象、起止记录、实际回执哈希/绑定和串行区间另行核对，四个真实非零结果保留；历史整体审计仍INCOMPLETE。

塑料感、细线、身体与转向协调、完整自然游动、首套用户方向、科学/儿童/A1–A3及其余成品模型/海域均未完成；真实触控、最低OS与双真机20分钟仍未验收。源码、Editor、实际Player、人工观感和分发分开记录，2.4、apply与完整目标保持未完成。
