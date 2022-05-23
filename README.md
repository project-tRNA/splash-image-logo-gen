# Splash Image Logo Generator

A fork of Qualcomm Splash Screen generator ([logo_gen.py](https://source.codeaurora.org/quic/la/device/qcom/common/tree/display/logo/logo_gen.py?h=LA.BR.1.3.3-06310-8952.0)). In short, a splash.img maker.

English | [简体中文](README-zh.md)

## Usage

* Put your picture (e.g. `logo.png`) along with `LogoGen.exe`
* Run `LogoGen.exe logo.png` in cmd.
* Then rename `logo.png.splash` to `splash.img` if you want.

## For Redmi 7

You may analyzed the `splash.img` of *Redmi 7* by [binwalk](https://github.com/ReFirmLabs/binwalk).
```
DECIMAL       HEXADECIMAL     DESCRIPTION
--------------------------------------------------------------------------------
4096          0x1000          Qualcomm splash screen, width: 720, height: 1520, type: 1, blocks: 104
61952         0xF200          Qualcomm splash screen, width: 720, height: 1520, type: 1, blocks: 608
377856        0x5C400         Qualcomm splash screen, width: 720, height: 1520, type: 1, blocks: 133
450560        0x6E000         Qualcomm splash screen, width: 720, height: 1520, type: 1, blocks: 106
509440        0x7C600         Qualcomm splash screen, width: 120, height: 234, type: 1, blocks: 8
```
As you see, The description is `Qualcomm splash screen`. 
It seems not the expected format we want [in this method](https://github.com/moonheart/sagit-logo-gen/wiki).

Then I found the logo_gen.py tools link in [binwalk source code](https://github.com/ReFirmLabs/binwalk/blob/563a19d5cb7748da8da2db3ed5ee5c4dd76e8ffe/src/binwalk/magic/firmware#L810-L816).
And now this is the project what you see: *A C# version logo_gen.py*.

If you want to make a Redmi 7 splash screen. You need to prepare pictures in 720*1520 size, and pack them through this tool. Then use the `copy /b` to connect a 4096-size empty file and the packed-pictures in order. blblblbl...

I am just a Chinese senior high school student and I have to  prepare the college entrance examination. So I have little time for the project. This is theoretically feasible. It is not tested yet and I will try it when I am free.