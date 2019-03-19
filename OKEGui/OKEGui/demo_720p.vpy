import vapoursynth as vs
import sys
import havsfunc as haf
import mvsfunc as mvf

core = vs.get_core(threads=8)

core.max_cache_size = 4000

#OKE:INPUTFILE
a="00000.m2ts"
src8 = core.lsmas.LWLibavSource(a)
src16 = core.fmtc.bitdepth(src8,bits=16)

down = core.fmtc.resample(src16, 1280, 720)
res = down

#OKE:DEBUG
Debug = 1
if Debug:
	res=core.std.Interleave([down, res])
	res=mvf.ToRGB(res,full=False,depth=8) 
else: res = core.fmtc.bitdepth(res,bits=10)

res.set_output()
