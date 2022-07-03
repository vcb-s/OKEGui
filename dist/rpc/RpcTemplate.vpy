import vapoursynth as vs
import mvsfunc as mvf
import types
import sys
import functools
import multiprocessing
from vapoursynth import core
from importlib.machinery import SourceFileLoader

core.num_threads = min(16, multiprocessing.cpu_count())
core.max_cache_size = core.num_threads * 500

# Load the source clip by loading another vs script. 

sourceScript = r"OKE:SOURCE_SCRIPT"
videoFile = r"OKE:VIDEO_FILE"
loader = SourceFileLoader('source', sourceScript)
mod = types.ModuleType(loader.name)
OKE:VSPIPE_ARGS
loader.exec_module(mod)
src = vs.get_output(index = 1)
src = core.fmtc.bitdepth(src, bits=16)

# Load the ripped file.
rip = core.lsmas.LWLibavSource(videoFile).fmtc.bitdepth(bits=16)

# Get the Y plane in same dimention.
src = mvf.GetPlane(src)
rip = mvf.GetPlane(rip)
if src.width != rip.width or src.height != rip.height:
    src = core.resize.Bicubic(src, rip.width, rip.height, format=vs.GRAY16)

# runtime function to print psnr value to console.
def callback(n, f, clip):
    print("RPCOUT:", n, f.props.PlanePSNR, flush=True, file=sys.stderr)
    return clip

# Compare. Run it using 'vspipe "00009.m2ts_rpc.vpy" NUL'. 
cmp = mvf.PlaneCompare(rip, src, mae=False, rmse=False, cov=False, corr=False)
cmp = core.std.FrameEval(cmp,functools.partial(callback, clip=cmp), prop_src=[cmp])
cmp.set_output()
