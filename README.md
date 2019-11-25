![alt text](http://www.ajivin.com/images/spsimpleportfolio/site-clearing/portfolio6_600x400.jpg)

## 安装

1. OKEGui 需要.NET 4.5。Windows 8/Windows Server 2012及以上自带；Windows 7和Windows Server 2008需要自行安装: https://www.microsoft.com/zh-cn/download/details.aspx?id=30653

2. OKEGui 自带的 qaac 工具依赖 Apple Quicktime. 这点请确保你的机器按照压制组需要正确安装了64bit iTunes 组件或者 AppleApplicationSupport: https://github.com/vcb-s/OKEGui/releases/download/4.0/AppleApplicationSupport64.msi

3. 下载最新 Release 的 zip 压缩包，解压到一个纯英文目录下。双击其中 OKEGui.exe，如果能正确运行显示出窗口，即安装成功。

## 需要文件准备

OKEGui 每一批任务需要由技术总监准备3个文件。这三个文件需要放在同一个文件夹内，通常建议放在蓝光的BDMV目录下。

### 编码器文件
编码器文件一般为 x265.exe/x265_10b.exe，名称随意。目前 OKEGui 支持 x265/x264 编码。

### vpy脚本文件
vpy 脚本文件指定了预处理方式。OKEGui 要求 vpy 文件中含有两个固定的 tag：

```python
#OKE:INPUTFILE
a = "00000.m2ts"
......
```

```python
#OKE:DEBUG
Debug = True
if Debug:
......
```

第一个 tag 的作用是指定输入文件。接下来一行必须是 变量 = 文件名 的格式。OKEGui 会在批量处理任务的时候，自动替换文件名以生成不同的脚本。

第二个tag（可以不添加）的作用是关闭Debug flag。OKEGui 会保证用于压制的脚本，Debug flag 永远是 None（无论你判断 `if Debug == True` 还是 `Debug == 1/2/3` ，都会正确执行 `else: res = mvf.Depth(res, depth=10)` 语句）。

如果需要配合RP Checker功能，则需要输出一份跟成品相似，但是处理尽可能简单的clip：

1. 必须有相同的帧率和帧数。如果成品做了ivtc/deint/Trim/Splice之类的操作，则为rpc准备的clip也需要做。
2. 必须有相同的画面内容。如果做了对画面改动较大的操作，例如Crop/TextSub等操作，则为rpc准备的clip也需要做。
3. 分辨率和Bitdepth不重要，RPC会自动转为与成品相同的规格。

绝大多数情况下，src8/src16就足够了。以src8为例，脚本里需要含有：

`src8.set_output(1)`

经过set_output(1)输出的clip会被用来执行RpCheck。

### json文件
安装包里有两个样例 json 文件以供参考。json文件里的项目有

- Version，固定写2即可。
- ProjectName，自行填写。
- EncoderType，必须写 "x264" 或者 "x265"。
- Encoder，填写编码器的完整名称。比如你使用的编码器是 x265_10b_Yukki_Mod.exe，那么填写 "x265_10b_Yukki_Mod.exe"。编码器**必须**跟 json 文件放在同一个目录下。
- EncoderParam，编码器参数。不需要写 --y4m 和 --output。
- ContainerFormat，可选 "mp4" 和 "mkv"。
- AudioTracks 是一个 json array。其中，json 适用的源有几条音轨，这个 array 必须有几个项目。OKEGui 会自动跳过空音轨或者重复音轨（适用于那些每个原盘双音轨，但是只有部分集是评论，其他集是重复的原盘），但是你依旧需要给出每条音轨的参数：
  - OutputCodec，可选 "flac", "aac", "ac3"和"dts"。
  - Bitrate，选择 aac 时候，可以指定码率。默认是 192Kbps。
  - MuxOption，封装格式，可选 "Default", "Mka", "External", "ExtractOnly" 和 "Skip"。"Default" 是默认值，表示正常封装；"Mka" 表示额外封装在 mka 中；"External" 表示外挂，会给文件加上 CRC32；"ExtractOnly" 只做抽取；"Skip" 直接不抽取。
  - Language，语言。默认 "jpn"，可选 "eng", "chn", ...
- InputScript，输入脚本的全名。脚本文件**必须**跟 json 文件放在同一目录下。
- Fps，脚本输出的帧率。可选 23.976, 29.970, 59.940。
- FpsNum 和 FpsDen，当帧率不是上述三种之一时候，按照 vspipe 输出的帧率填写。比如24和1，代表 24.000fps
OKEGui 添加字幕。
- SubtitleTracks 是一个 json array，如果留空表示没有字幕。其中，json 适用的源有几条字幕，这个 array 必须有几个项目：
  - MuxOption，封装格式，可选 "Default", "Mka", "External", "ExtractOnly" 和 "Skip"。效果同 AudioTracks。
  - Language，语言。默认"jpn"，可选"eng", "chn", ...
- Config，这是默认的每个文件额外配置。这些参数一般单独在每一集的 json 里配置，这里只提供默认值。可以配置的参数有：
  - VspipeArgs 是一个 json array，每一项是一个 string，用 "arg=value" 的形式，指定传给 vspipe.exe 的额外参数。

需要单独配置每一集的 Config，通过单独的 json 文件（见附带 00001.m2ts.json）来指定。

## 载入并运行任务

1. OKEGui 可以压制不同后缀名的源。除了原盘的 m2ts，还可以以 mkv 等作为源文件。如果原盘不是一集一个 m2ts的，可以先通过 remux，封装出一集一个 mkv 文件的样式来压制。如果压制的文件需要章节，请提前抽取章节文件。命名为与源文件同名的 txt，比如 00000.m2ts->00000.txt, EP01.mkv->EP01.txt
2. 负责压制的组员，必须先检查确保自己的机器上，vpy 能正确输出，且画面正常。
3. 点击新建任务按钮：
	- 选择 OKEGui 项目文件，载入 json 文件。如果载入成功，界面会显示任务项目的一些摘要。如果失败，按照报错信息修改 json 文件，并确保 vpy 和编码器文件在同一目录，重新尝试载入。成功后点击下一步。
	- 选择输入文件。输入文件就是待压制的视频源。OKEGui 可以多选文件，也可以在选择一个文件之后，再次点击“打开文件”选择其他的文件。选择错了可以点击并删除。通常技术总监交代任务时候，会指定这套方案适用于哪些 m2ts 文件。点击下一步。
4. 当前页面上应该已经添加了刚刚指定的所有压制任务，每个源文件一个。如果还需要添加其他方案的压制任务，重复2。
5. 如果需要多开，点击右下角“新建工作单元”和“删除工作单元”调整同时运行的任务数量。
6. 点击“运行”开始压制任务。

## 常见错误检查

1. 如果在载入 json 文件之后，出现非常混乱的报错，通常是因为 json 文件有语法错误。
2. 如果指定输入文件后，提示“添加多个输入文件请确保 VapourSynth 脚本使用 OKE 提供的模板。”，说明 vpy 文件里没有 `#OKE:INPUTFILE`。
3. 如果编码后弹窗出错，状态改为“音轨数不一致”，说明 json 里指定的音轨数量，和原盘中实际抽取出的数量不一致。json 里音轨数量必须和原盘里的完全一致；不想封装的可以用 `SkipMuxing : true` 来跳过封装。
4. 如果编码后弹窗出错，状态改为“x265出错”，说明 x265 编码报错。如果是任务开头出现问题，多半是编码器参数写错了。
5. 如果编码后弹窗出错，状态改为“vpy出错”，说明 vpy 脚本解读错误。
6. 其他未知错误，如果造成程序崩溃或者任务中断，请开任务管理器，先看看是否还有 vspipe.exe/x265.exe 在占用运算资源（记得汇报这一点），再手动掐掉。部分错误发生的之后，程序将跳过出错任务，继续跑完接下来的任务。
7. 汇报未知错误的时候，请尽可能还原所用的 json/vpy/exe，以及源的 m2ts 类型。（帧率，音轨数量，是否有字幕，是否准备了章节）

## 代码中相关概念解释:

Task: 从单个源（例如m2ts）到成品（例如mkv）的整个过程。task会在主程序界面的列表里显示。

Job: 每个Task会被分解成不同的Job，并依次执行。例如抽流，压制，封装等。Job是可以独立运行的最低单位。

JobProcessror: 负责执行每个Job的命令行Warpper。比如X265Encoder调用x265压制HEVC，FFMpegVolumeChecker调用ffmpeg检查音轨音量

Model: 储存媒体文件相关的信息。Info只带例如语言、封装选项等信息，Track则是File+Info的组合，MediaFile则是多条Track的合集

Worker: 每一个Task只会在一个Worker里进行，因此有几个Worker就允许几个Task同时进行。多开相关的选项。每个Task具体的实现流程由Worker负责执行。
