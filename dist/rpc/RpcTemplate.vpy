import vapoursynth as vs
import types
import sys
import functools
from vapoursynth import core
from importlib.machinery import SourceFileLoader


# Load the source clip by loading another vs script. 

sourceScript = r"OKE:SOURCE_SCRIPT"
videoFile = r"OKE:VIDEO_FILE"
loader = SourceFileLoader('source', sourceScript)
mod = types.ModuleType(loader.name)
OKE:VSPIPE_ARGS
loader.exec_module(mod)

src = vs.get_output(index = 1)
srcy = src.std.ShufflePlanes(0, vs.GRAY)
if not (srcy.format.bits_per_sample == 8): srcy = srcy.resize.Point(format=vs.GRAY8, dither_type='none')

# Load the ripped file.
rip = core.lsmas.LWLibavSource(videoFile)
ripy = rip.std.ShufflePlanes(0, vs.GRAY)
if not (ripy.format.bits_per_sample == 8): ripy = ripy.resize.Point(format=vs.GRAY8, dither_type='none')

# Get the Y plane in same dimention.
if srcy.width != ripy.width or srcy.height != ripy.height:
    srcy = srcy.resize.Bicubic(ripy.width, ripy.height)

# runtime function to print psnr value to console.
def callback(n, f, clip):
    print("RPCOUT:", n, f.props.PlanePSNR, flush=True, file=sys.stderr)
    return clip

# Compare. Run it using 'vspipe "00009.m2ts_rpc.vpy" NUL'. 
cmp = core.complane.PSNR(ripy, srcy)
cmp = core.std.FrameEval(cmp,functools.partial(callback, clip=cmp), prop_src=[cmp])
cmp.set_output()
