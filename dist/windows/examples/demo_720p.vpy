import vapoursynth as vs
import sys
from vapoursynth import core
import havsfunc as haf
import mvsfunc as mvf

core.num_threads = 12

#OKE:MEMORY
core.max_cache_size = 9000

#OKE:INPUTFILE
a="00001.m2ts"
src8 = core.lsmas.LWLibavSource(a)
src16 = core.fmtc.bitdepth(src8,bits=16)

gray = core.std.ShufflePlanes(src16, 0, colorfamily=vs.GRAY)
gray = core.fmtc.transfer(gray,transs="709",transd="linear")
gray = core.fmtc.resample(gray,1280,720)
gray = core.fmtc.transfer(gray,transs="linear",transd="709")
UV = core.fmtc.resample(src16,1280,720)
down = core.std.ShufflePlanes([gray,UV],[0,1,2], vs.YUV)

nr16 = core.knlm.KNLMeansCL(down,device_type="GPU",h=0.6,s=3,d=1,a=2,channels="Y")
noise16 = core.std.MakeDiff(down,nr16,0)
dbed = core.f3kdb.Deband(nr16, 8,48,48,48,0,0,output_depth=16)
dbed = core.f3kdb.Deband(dbed,16,32,32,32,0,0,output_depth=16)
dbed = mvf.LimitFilter(dbed,nr16,thr=0.5,thrc=0.4,elast=1.5)
dbed = core.std.MergeDiff(dbed,noise16,0)


bright = mvf.Depth(dbed,8,dither=1)
dark = mvf.Depth(dbed,8,dither=0,ampo=1.5)
res = core.std.MaskedMerge(dark, bright, core.std.Binarize(bright, 128, planes=0), first_plane=True)

res.set_output(0)
src8.set_output(1)
