# Splash Image Logo Generator

复刻自高通平台的第一屏生成器 ([logo_gen.py](https://source.codeaurora.org/quic/la/device/qcom/common/tree/display/logo/logo_gen.py?h=LA.BR.1.3.3-06310-8952.0)). 简称，splash.img 制作器.

[English](README.md) | 简体中文

## 用法

* 将你的图片 (例如 `logo.png`) 跟 `LogoGen.exe` 放在一起
* 在 cmd 执行 `LogoGen.exe logo.png`
* 然后会生成一个 `logo.png.splash`，需要的话改名成 `splash.img` 即可刷入

## 制作 红米 7 开机第一屏

你可能已经用 [binwalk](https://github.com/ReFirmLabs/binwalk) 分析过 `splash.img` 了.
```
DECIMAL       HEXADECIMAL     DESCRIPTION
--------------------------------------------------------------------------------
4096          0x1000          Qualcomm splash screen, width: 720, height: 1520, type: 1, blocks: 104
61952         0xF200          Qualcomm splash screen, width: 720, height: 1520, type: 1, blocks: 608
377856        0x5C400         Qualcomm splash screen, width: 720, height: 1520, type: 1, blocks: 133
450560        0x6E000         Qualcomm splash screen, width: 720, height: 1520, type: 1, blocks: 106
509440        0x7C600         Qualcomm splash screen, width: 120, height: 234, type: 1, blocks: 8
```
如你所见，文件描述是 `Qualcomm splash screen`. 
这跟 [moonheart大佬的教程](https://github.com/moonheart/sagit-logo-gen/wiki) 里的看起来不一样啊.

然后我在 [binwalk 的源代码里](https://github.com/ReFirmLabs/binwalk/blob/563a19d5cb7748da8da2db3ed5ee5c4dd76e8ffe/src/binwalk/magic/firmware#L810-L816) 找到了 logo_gen.py 的链接.  
好巧不巧，我玩不明白python，就是 logo_gen.py 那个刁钻的环境需求给我整破防了.  
我当场拿手机远控电脑爆肝出来这么个东西: *用 C# 重置的 logo_gen.py*.

要是你想做第一屏，你需要准备 720*1520 大小的图片,  
然后分别把它们用这个工具生成.splash，再搞来一个 4096 长度的空文件…  
总之就是按照 binwalk 分析结果把东西都备好，然后用 `copy /b` 把它们拼一起就能当 splash.img 镜像刷进去了.

我，~~女子高中生~~，备战高考，没什么时间搞这个项目了。理论上是可行的，但我还没空去测试，我有空再去测试

注：MIUI卡刷官包的firmware文件夹里有 splash.img，可以拿来分析或者还原第一屏。