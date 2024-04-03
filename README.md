# [OKEGui](https://github.com/vcb-s/OKEGui/) &middot; [![GitHub license](https://img.shields.io/badge/license-GPLv2-blue.svg)](https://github.com/vcb-s/OKEGui/blob/master/LICENSE) [![Build status](https://ci.appveyor.com/api/projects/status/p4p7upa6hmsgu599?svg=true&passingText=%E7%BC%96%E8%AF%91%20-%20%E7%A8%B3%20&pendingText=%E5%B0%8F%E5%9C%9F%E8%B1%86%E7%82%B8%E4%BA%86%20&failingText=%E6%88%91%E6%84%9F%E8%A7%89%E5%8D%9C%E8%A1%8C%20)](https://ci.appveyor.com/project/vcb-s/okegui)


![alt text](http://www.ajivin.com/images/spsimpleportfolio/site-clearing/portfolio6_600x400.jpg)

## 安装与使用

1. OKEGui 需要 .NET 4.5。Windows 8/Windows Server 2012 及以上自带；Windows 7 和 Windows Server 2008 需要自行安装: https://www.microsoft.com/zh-cn/download/details.aspx?id=30653 。

2. OKEGui 自带的 qaac 工具依赖 Apple Quicktime，请确保正确安装了 64bit iTunes 组件或者 [AppleApplicationSupport](https://github.com/vcb-s/OKEGui/releases/download/4.0/AppleApplicationSupport64.msi)。  
    从 8.7.1 版本开始，OKEGui Release 将自带 qaac 相关依赖，无需额外安装。

3. 下载最新 Release 的 zip 压缩包，解压到一个纯英文目录下。双击其中 OKEGui.exe，如果能正确运行显示出窗口，即安装成功。

4. OKEGui 依赖于视频处理框架 VapourSynth，本仓库的 Release 不包含 VapourSynth，推荐使用我们打包好的 [OKEGui portable 整合包](https://github.com/AmusementClub/tools/releases)。

5. OKEGui 的使用方法可参考 [Wiki](https://github.com/vcb-s/OKEGui/wiki) 和 VCB-Studio 公开教程 [第十一章](https://guides.vcb-s.com/basic-guide-11)。

## 代码中相关概念解释:

Task: 从单个源（例如 m2ts）到成品（例如 mkv）的整个过程。Task 会在主程序界面的列表里显示。

Job: 每个 Task 会被分解成不同的 Job，并依次执行。例如抽流，压制，封装等。Job 是可以独立运行的最低单位。

JobProcessror: 负责执行每个 Job 的命令行 Warpper。比如 X265Encoder 调用 x265 压制 HEVC，FFMpegVolumeChecker 调用 ffmpeg 检查音轨音量。

Model: 储存媒体文件相关的信息。Info 只带例如语言、封装选项等信息，Track 则是 File+Info 的组合，MediaFile 则是多条 Track 的合集。

Worker: 每一个 Task 只会在一个 Worker 里进行，因此有几个 Worker 就允许几个 Task 同时进行。多开相关的选项。每个 Task 具体的实现流程由 Worker 负责执行。
