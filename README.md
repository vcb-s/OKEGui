![alt text](http://www.ajivin.com/images/spsimpleportfolio/site-clearing/portfolio6_600x400.jpg)

## 安装

1. OKEGui 需要.NET 4.5。Windows 8/Windows Server 2012及以上自带；Windows 7和Windows Server 2008需要自行安装: https://www.microsoft.com/zh-cn/download/details.aspx?id=30653

2. OKEGui 自带的 qaac 工具依赖 Apple Quicktime. 这点请确保你的机器按照压制组需要正确安装了64bit iTunes 组件或者 AppleApplicationSupport: https://github.com/vcb-s/OKEGui/releases/download/4.0/AppleApplicationSupport64.msi

3. 下载最新 Release 的 zip 压缩包，解压到一个纯英文目录下。双击其中 OKEGui.exe，如果能正确运行显示出窗口，即安装成功。

## 代码中相关概念解释:

Task: 从单个源（例如m2ts）到成品（例如mkv）的整个过程。task会在主程序界面的列表里显示。

Job: 每个Task会被分解成不同的Job，并依次执行。例如抽流，压制，封装等。Job是可以独立运行的最低单位。

JobProcessror: 负责执行每个Job的命令行Warpper。比如X265Encoder调用x265压制HEVC，FFMpegVolumeChecker调用ffmpeg检查音轨音量

Model: 储存媒体文件相关的信息。Info只带例如语言、封装选项等信息，Track则是File+Info的组合，MediaFile则是多条Track的合集

Worker: 每一个Task只会在一个Worker里进行，因此有几个Worker就允许几个Task同时进行。多开相关的选项。每个Task具体的实现流程由Worker负责执行。
