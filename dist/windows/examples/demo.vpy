import vapoursynth as vs
import sys
import math
from vapoursynth import core
import havsfunc as haf
import mvsfunc as mvf

core.num_threads = 8

#OKE:MEMORY
core.max_cache_size = 8000

#OKE:INPUTFILE
a="00000.m2ts"
src8 = core.lsmas.LWLibavSource(a)
src16 = core.fmtc.bitdepth(src8,bits=16)

op = core.rgvs.RemoveGrain(src16, 20)

res = core.std.Trim(src16, 0, int(op_start) - 1) + core.std.Trim(op, int(op_start), int(op_end)) + core.std.Trim(src16, int(op_end) + 1, src16.num_frames - 1)

#OKE:DEBUG
Debug = 1
if Debug:
	res=core.std.Interleave([src16, res])
	res=mvf.ToRGB(res,full=False,depth=8) 
else: res = mvf.Depth(res, 10)

res.set_output()
src16.set_output(1)
