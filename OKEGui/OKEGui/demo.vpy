import vapoursynth as vs
import sys
import math
import havsfunc as haf
import mvsfunc as mvf

core = vs.get_core(threads=8)

core.max_cache_size = 8000

#OKE:INPUTFILE
a="00000.m2ts"
src8 = core.lsmas.LWLibavSource(a)
src16 = core.fmtc.bitdepth(src8,bits=16)

res = src16

#OKE:DEBUG
Debug = 1
if Debug:
	res=core.std.Interleave([src16, res])
	res=mvf.ToRGB(res,full=False,depth=8) 
else: res = mvf.Depth(res, 10)

res.set_output()

