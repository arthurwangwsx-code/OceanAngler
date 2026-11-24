# 英雄鱼动作节律候选

源与Editor局部通过，原生模拟器验证待完成；完整拟真美术仍PARTIAL。已发布TestFlight8保持冻结`1bdfa785237701ab2c7675031142d526f66c0e28`，本轮没有更新TestFlight或操作实体iPhone。机器结果见[JSON](bluefin-periodic-motion.json)，重建及实际复核入口见[制作说明](../docs/34-hero-motion-continuity.md)。

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

32份旧QA主文件已在更新前按当前容器及归属核实并留存原字节，新包验证后再核对。仅清理本项目已结束的发行Xcode中间缓存，原build7/8签名归档与IPA摘要前后相同；其他项目、个人设备和已有存档未清理，du数值不代表独占APFS回收量。

塑料感、细线、身体与转向协调、完整自然游动、首套用户方向、科学/儿童/A1–A3及其余成品模型/海域均未完成；真实触控、最低OS与双真机20分钟仍未验收。源码、Editor、实际Player、人工观感和分发分开记录，2.4、apply与完整目标保持未完成。
