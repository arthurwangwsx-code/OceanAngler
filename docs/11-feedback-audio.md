# 音效、环境底音与探索音乐

后续源码保留原有咬钩、收鱼和逃脱提示，并新增原创水下底音与探索主题。设置中的“环境与提示音”和“探索音乐”独立控制，音乐默认关闭，旧设置没有music字段时也保持关闭。设置即时保存；在钓鱼页主动继续后播放，暂停、失焦、模拟后台事件或存档恢复界面会停止所有声源。继续后循环从头淡入，音乐不会在恢复后台时自行启动。

## 素材与重建

| 文件 | 内容 | 源格式 |
|---|---|---|
| [OceanBed.wav](../game/Assets/OceanAngler/Resources/Audio/OceanBed.wav) | 12秒原创合成水下底音，含循环边界平滑 | 22050Hz、立体声、16位PCM |
| [ExploreTheme.wav](../game/Assets/OceanAngler/Resources/Audio/ExploreTheme.wav) | 32秒原创五声音阶探索主题 | 22050Hz、立体声、16位PCM |

生成入口为`python3 tools/create_audio.py`，仅用标准库与固定随机种子，不读取录音、私人媒体或第三方曲目。`--verify`检查PCM结构、非静音电平、峰值和循环衔接；素材及摘要按MIT登记于art/asset-manifest.json。听感仍是待审阅候选，合成水声不标作真实海洋录音。

Unity通过AudioAssetBuilder实际导入，使用Vorbis、CompressedInMemory和保留采样率；Prepare和Mac/iOS构建入口检查资源存在、声道、频率、时长及导入类型。Unity生成的.meta原样保留。两循环声源为2D，优先级低于提示音，淡入约半秒；提示音相位连续，避免换音时直接跳到不同相位。声音关闭会在同一玩法阶段停止提示与环境声；音乐可单独关闭。

这些选择基于Unity的[AudioSource说明](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/audiosource)及[音频导入说明](https://docs.unity.com/en-us/engine/6000.3/manual/audio/reference/class-audio-clip)。压缩驻留的解码CPU、实际音量、耳机效果及手机内存仍须实测，不从文件尺寸或桌面编译推断真机表现。

## 检查范围

[audio-feedback.json](../reports/audio-feedback.json)保留PCM检查、28项Editor与7项无画面PlayMode及Mac编译结果。实际场景控件检查音乐设置保存重载、默认关闭、音效/音乐独立音量、同阶段静音、模拟后台停止与恢复保持暂停；也读取三组运行时提示音的样本检查非静音和连续性。无画面测试只验证数据、声源属性和逻辑，不证明扬声器真正发声或听感合格。

首次底音边界检查失败及最终字体补齐前的检查记录均保留。最终字库包含新说明文字。iOS咬钩触觉沿用原有可关闭的Handheld.Vibrate调用，本轮未改变系统触觉实现，实际震动效果未验。

扬声器/耳机试听、音效与音乐平衡、实际设备触觉、后台/锁屏恢复及20分钟音频性能仍为NOT_RUN。当前TestFlight 0.1.0 (2)不含这项补丁；不能用既有构建验证新增音乐或环境声。

发行更新：上述后续源码已进入0.1.0 (3)/05c8cc7，Apple处理和本人组已核实；[build 3回执](../reports/testflight-0.1.0-3.json)与[设备复核](12-beta3-review.md)另记。科学人工、实际设备画面/听感及完整产品仍未通过。
