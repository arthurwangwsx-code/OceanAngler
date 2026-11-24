# 英雄鱼动作循环与节律

REQ-05/2.4后续，实施IN_PROGRESS。当前源动画在奇数尾摆周期中，胸鳍使用半频正弦，只完成半个往复；转向偏置为半正弦，夹住的开口正弦也有速度折点。自动端点切线又会把周期边缘压平。现有七动作“存在”及尾骨“会动”不能证明连续、自然。

本轮保留全部三档网格、UV、权重、骨架静态结构、贴图与预算，只修订关键帧：胸鳍完成整数往复、转向采用闭合平滑包络，咬钩/上钩开口采用平滑闭合曲线；每个关键帧用解析速度建立显式Bezier切线。仍是原创美术节律，不是从实鱼测量得到的生物运动模型，不声称科学或自然观感已通过。

新增`python3 tools/art.py build --asset bluefin --motions-only --label unique --no-preview`，读取已保存Blend并更新动作和FBX，不重新减面或生成纹理。全量制作也复用同一动作配方。保存前后全部三档几何/UV/权重与九PNG字节、骨架结构独立比对；实际源曲线与Unity导入曲线分开检查，尤其循环接缝速度、尾摆相位、嘴部与鳍根。导入压缩若破坏连续性须继续修正，不以源文件通过代替导入通过。

需要查看当前/候选固定镜头动作序列，保留首次绘制与失败，使用生产Medium/Metal/Half HDR/4×MSAA。暂停、减弱动态、七动作切换和放流结束另验；不更改积分、概率、存档、UI或已发布的build8。源/Editor/原生/人工与最终美术分别验收。用户首套方向未确认前不批量生产其他鱼种，2.4与apply不勾选完成。

实际源检查已通过：全部三LOD几何/UV/权重、12骨骼静态矩阵和九PNG保持，三档24664/9864/3698；七动作端点值闭合、解析切线检查通过，原189条件/72处薄壁及35姿态检查通过。

原始FBX导入后仍有采样/压缩造成的接缝，不能直接声称源通过即播放通过。Unity重建入口现将七个导入动作派生为自有原生`.anim`资产，六个循环动作采用周期端点Hermite切线；局部极值保持零速度，跨过非极值时保留运动，放流仍不循环。Controller引用这些实际播放资产，不修改全局导入器。七文件约1.4MiB是源序列化占用，不能代替运行时内存测量。[Unity SetEditorCurves接口](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/animationutility/seteditorcurves)用于一次写入各浮点曲线。

10项定向Editor和3项定向PlayMode已通过；前一筛选命令中的SceneIntegrationTests并不存在，实际只运行两个FishAnimationTests，随后单独运行真正的IntegratedWorldTests一项，不把未匹配的名字计作测试。未重复完整成长或全量用例。

固定镜头D生成640帧序列、八张首次图及八张重复图，八组重复RGB差0。此前A/B俯视镜头裁尾被人工否决，C的新检查又发现诊断将缩放计算两次，非零失败保留；按[BakeMesh缩放契约](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/skinnedmeshrenderer/bakemesh)修正后，每帧同时检查变形网格投影与实际像素边界，至少留约12.8%边距，旧俯视控制负边距约-23%。这些是离线Clip采样，尚不能替代实际Animator、Player运行及自然观感审阅。

复核入口：从已发布build8冻结提交`1bdfa785237701ab2c7675031142d526f66c0e28`取出自有FBX到本机忽略目录，再运行`python3 tools/game.py motion-review --label unique --review-target ios --motion-reference .local/previous-bluefin.fbx`。对照序列按20fps编码四秒视频，编码帧率不是游戏性能。完整结果和原图见[动作记录](../reports/bluefin-periodic-motion.md)。本轮新的模拟器包仍待冻结和验证，已发TestFlight8保持独立证据。
