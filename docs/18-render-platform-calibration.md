# 渲染检查的平台与环境校准

近海候选出现过两类差异：同一进程静态绘制的极少量单色阶变化，以及不同进程之间的整体颜色变化。两者分别取证，不以中间一次零差异覆盖其他失败。

旧GPU入口沿用Editor上一次使用的目标平台，Mac编译后与iOS导出后的检查可能处于不同配置。本次入口新增明确的 `--review-target mac|ios`，默认mac；进入水面夹具还会核对实际活动目标。Unity官方说明，未指定目标时会载入上次配置，见[命令行构建说明](https://docs.unity.com/en-us/engine/6000.3/manual/building-and-publishing/build-command-line)。

串行复查示例（每次使用新label，保留原始回执；下面只示范一项）：

```sh
ispec changes measure --change product-technical-foundation --stage apply --category e2e -- python3 tools/game.py channel-review --label freshioslabel --review-target ios
```

`ios`仍是Mac上的Editor Metal渲染，只选择iOS的导入/着色配置，不能称为iPhone截图或性能测试。实际记录中两种目标都是Gamma/Ultra、同一URP/4×MSAA、相同太阳与27项环境探针系数；近海六张贴图在Mac为BC7，在iOS为ASTC 4×4。切换目标能重现整体差异，但不能单凭此断言某一种压缩或某个shader分支是全部根因。当前项目仍需实际设备与最终色彩管理审阅。

水面夹具在场景载入、海域呈现、三次实际绘制后分别写 `environment.json`，记录目标、颜色空间、质量、雾、天空shader、太阳、相机、环境探针、默认反射解码和实际纹理格式。它是诊断输入快照，不是GPU帧调试器；不声称捕获所有shader常量或内部中间缓冲。

同平台另一次差异局限于船体及其倒影，而近景岛壁保持一致。显式设置船体各材质槽颜色/光滑度/金属度的两次对照，水面首图与倒影跨进程分别一致；水面同进程仍有约12–13像素、最大1/255变化。该控制仅通过 `OCEAN_CHANNEL_REVIEW_BOAT_BLOCKS=1` 启用并在回执标明，默认关闭；没有把它当作生产修复，也没有给所有场景增加PropertyBlock成本。

零差异阈值与游戏4×MSAA保持，PARTIAL仍返回非零。完整记录见[证据](../reports/render-platform-calibration.md)。本轮仅修改Editor诊断与入口，没有更新手机或TestFlight，也没有修改存档、玩法或成品资产。完整产品、美术和双设备门槛保持未完成。
