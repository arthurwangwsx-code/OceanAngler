# 英雄鱼体色与双端原生复核

本轮为局部通过，完整产品与拟真美术仍PARTIAL。源提交`201534b0df86767bfcf9434dde2dc2e2fc74800e`，源码摘要`18d26b17df5b633c5ed5ef5f1a60c374f6a789aecd4144fec7e553dd1df5f25f`；同一新Simulator包摘要`d9a89297f5079302a535240af90b6db915229ae1f34897b9175339a41d0c1a30`。完整机器结果见[JSON](bluefin-neutral-albedo.json)，可复用入口与参考见[说明](../docs/32-hero-albedo-and-lighting.md)。

只将原创身体贴图调整为更中性的灰银/灰铜，保留蓝黑斑点和蓝色鳍。所选原图偏绿有所减轻；这是美术RGB选择，没有采样或再分发参考照片，不是科学或色度学资格。

全部三档几何、UV、权重与材质角色指纹在六位小数规范精度下保持；FBX和另外八张PNG字节一致，仅BaseColor变化。三档24664/9864/3698，近景115根实体鳍条、远距各16根轮廓鳍条保持。源文件链接图像与导出PNG在同一Blender解码器下最大像素差0；源189条件、72薄壁及七动作35姿态通过。五项定向Editor、两项定向PlayMode通过，不借用旧全量、成长或休息检查作为新源码结果。

Metal、Medium、Half HDR、4×MSAA下最终18张静态对照通过，三视角参考/重复与临时探针/重复严格RGB差0。首次绘制单独保留、不作冷启动资格。默认天空反射实际存在，临时局部探针对所选鱼体没有明确观感收益，未加入正式资产或运行时。与前一8a1c68c相比，游戏Runtime、Resources和ProjectSettings未改。

两次早期GPU失败保留：首次场景材质绑定异常，第二次仍有42像素初始差异。全量重生成曾意外改变两档远距减面选择，身份校验失败；保留被拒Blend/FBX后恢复原模型，使用新增maps-only入口更新贴图，没有放宽身份精度。探针捕获触发Editor颜色温度序列化初始化，工具仅恢复已识别的单项变化，其他差异会保留并报错；恢复前后设置SHA一致。

iPhone与iPad各15帧流程、左右手布局和一次真实正常教学放流通过，分别222/191分；Metal/HDR4/4×MSAA一致。可见HUD最高12.228%/4.526%，中心留空，操作保持暂停与当前阶段图标。覆盖首页、准备、五个钓鱼阶段、暂停、设置、统一退出及左手关键页面。已有30份QA主存档文件逐字节保持，未卸载或恢复覆盖；自动EventSystem操作不代表系统触摸或儿童理解。

两张接触表和四张原生原图、两张源近景原图已查看，没有逐张查看全部18张GPU或30张原生原图。源对照为同镜头静态姿态；原生截图时刻不同，不作逐像素前后比较。

| 相同视角源对照 | 原图 |
| --- | --- |
| 调整前 | [近景](../art/previews/bluefin-neutral-albedo/near-albedo-before.png) |
| 当前候选 | [近景](../art/previews/bluefin-neutral-albedo/near-albedo-candidate.png) |
| iPhone | [控线](../art/previews/bluefin-neutral-albedo/phone-fighting.png)、[收鱼](../art/previews/bluefin-neutral-albedo/phone-landed.png) |
| iPad | [控线](../art/previews/bluefin-neutral-albedo/tablet-fighting.png)、[收鱼](../art/previews/bluefin-neutral-albedo/tablet-landed.png) |

另只清理旧fin-screen构建的Intermediates.noindex（du约1263064KB），原可安装App树摘要前后不变，源码、导出、日志及QA数据保留。此数不是独占APFS释放量。

塑料感、近景细线、刚性与自然游动、首套用户/科学/儿童/A1–A3、其余成品模型、真实触控与双真机20分钟仍未通过。首套方向待用户审阅，不批量制作其他鱼种。没有新跑完整成长、全套测试或900秒休息；没有操作个人手机或更新TestFlight，已发build7仍冻结653d3a7，实际TestFlight安装尚未验证。原任务及apply保持未完成。

本增量严格读回34次启动/34次完成，实际回执摘要与绑定一致、测量区间无重叠，三次真实失败保留；1121/1116条历史对象原样保持。总1155/1150的原五条启动缺口仍INCOMPLETE，不把本增量通过称作整体审计通过。
