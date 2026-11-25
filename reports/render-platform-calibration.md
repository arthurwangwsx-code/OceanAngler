# 渲染平台校准证据

本轮仅改善诊断入口，游戏运行逻辑和资产保持地形候选e9fe2a3；完整产品与图形稳定性仍PARTIAL。51项Editor通过。没有运行新原生玩家窗口，没有生成新移动包；本地安装与TestFlight均保留build 5，不包含后续地形候选。

旧入口会继承上一次活动目标。显式Mac→iOS→Mac检查在相同Gamma/Ultra、太阳、雾与环境探针下重现大范围差异：Mac/iOS水面图最大差88、均差约6.098字节，近海贴图实际分别为BC7和ASTC 4×4。它证明平台配置是必须控制的变量，不证明仅压缩格式解释全部差异；不能把不同目标图当作同一环境的稳定性比较。入口已固定默认mac并允许明确选择ios，水面夹具核对实际目标，逐阶段写环境快照。用法及范围见[说明](../docs/18-render-platform-calibration.md)。

同平台另外的3697像素变化全部落在船体/倒影附近的矩形[381,491,528,543]，最大89字节；比较中的岛壁近景为零差异。船体各材质槽PropertyBlock控制的两个进程首图与倒影均零差异，但同进程水面三图仍失败。该控制仅是默认关闭的Editor实验，没有修改运行时，也没有证明SRP Batcher一定是根因。

| 运行 | 活动目标 | 严格静态回执 | 首次/后次变化像素 |
|---|---|---|---|
| channel14environment | StandaloneOSX | PARTIAL_STATIC_RGB_DIFFERENCE | 12 / 0 |
| channel15environment | StandaloneOSX | PARTIAL_STATIC_RGB_DIFFERENCE | 12 / 0 |
| channel16ios | iOS | PARTIAL_STATIC_RGB_DIFFERENCE | 27 / 0 |
| channel17mac | StandaloneOSX | PARTIAL_STATIC_RGB_DIFFERENCE | 0 / 13 |
| channel18macformats | StandaloneOSX | PARTIAL_STATIC_RGB_DIFFERENCE | 12 / 0 |
| channel19iosformats | iOS | PASS | 0 / 0 |
| channel20boatblocks | StandaloneOSX | PARTIAL_STATIC_RGB_DIFFERENCE | 13 / 0 |
| channel21boatblocks | StandaloneOSX | PARTIAL_STATIC_RGB_DIFFERENCE | 12 / 0 |
| channel22iosfinal | iOS | PASS | 0 / 0 |
| channel23macfinal | StandaloneOSX | PARTIAL_STATIC_RGB_DIFFERENCE | 0 / 13 |

所有运行均保留4×MSAA和零差异门槛，PARTIAL返回1。最后iOS配置三图相同并与前一次iOS首图相同，不能覆盖早期同配置失败；最后Mac配置仍失败。最终普通夹具和两个材质控制不可合并成全绿结果；跨设备、跨全部进程的逐位稳定性没有通过。

已分别审阅并登记两张最终[Editor原图及输入快照](render-platform-calibration/)，不是物理iOS帧。环境快照不包含全部引擎/GPU内部状态。完整数值、原始回执范围及测量ID见[结构化报告](render-platform-calibration.json)。本地保留全部10次运行、失败退出码和原图；独立receipt审计仍INCOMPLETE。

后续需继续定位船体默认材质的首图与水面MSAA差异，验证实际设备画面和成本；不能用本轮诊断替代A1–A3、科学/用户/儿童、20次实际UI、iPhone/iPad 20分钟或完整模型/陆坡交付。全产品与apply保持未完成。
