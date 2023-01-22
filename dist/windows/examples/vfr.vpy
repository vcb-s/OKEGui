import vapoursynth as vs
from vapoursynth import core
import pathlib
import mvsfunc as mvf

#OKE:INPUTFILE
A="00000.m2ts"

if 'a' not in globals(): a = A

src8 = core.lsmas.LWLibavSource(a)
src16 = core.fmtc.bitdepth(src8,bits=16)

# preprocess
res = core.grain.Add(src16, 1)

# generate cfr clips
res_a = core.std.AssumeFPS(res, fpsnum=24000,fpsden=1001)[:res.num_frames//2]
res_b = core.std.AssumeFPS(res, fpsnum=30000,fpsden=1001)[res.num_frames//2:]

# VFR splice
path = pathlib.Path(a)
res = mvf.VFRSplice([res_a, res_b], tcfile=str(path.with_suffix('.tcfile')), v2=False) # v2=True also ok
# x265 only support CFR input, so use an approximate FPS here.
res = core.std.AssumeFPS(res, fpsnum=27000, fpsden=1001)

#OKE:DEBUG
Debug = 0
if Debug:
	res=core.std.Interleave([src16, res])
	res=mvf.ToRGB(res,full=False,depth=8)
else: res = mvf.Depth(res, 10)

res.set_output()
src16.set_output(1)
