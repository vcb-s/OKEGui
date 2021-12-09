# v7.3.2 "The Protagonist Returns", again^4!
Basically upstream v7.3, with the following additions:
- Huge page support.
- Configurable set of common path components to strip for working/output directory.
- "Lossy" audio transcode support.
- The window title is still "The Protagonist Returns".

# v7.2.7 "The Protagonist Returns", again^4!

- Show vspipe path in window title.
- Add support for "Optional" field in AudioTracks and SubtitleTracks. All optionals tracks of the same type must satisfy that either all exist or none exists.
- The window title is still "The Protagonist Returns".

# v7.2.6 "The Protagonist Returns", again^3!

- Strip configurable set of common path components (e.g. BDMV STREAM) in working & output directory path.
- Update auto-update to check the AmusementClub fork, instead of the origin upstream.
- The window title is still "The Protagonist Returns".

# v7.2.5 "The Protagonist Returns", again^2!

- If "跳过Numa检测" is set, do not set numa node when starting vspipe and encoder.
  - Also change the affinity mask from (1<<28)-1 to (1<<64)-1 to avoid wasting CPUs.
- Updated eac3to-wrapper to v1.2 (built with Go 1.17).
- The window title is still "The Protagonist Returns".

# v7.2.4 "The Protagonist Returns", again!

- Simplify temporary working directory path, useless "BDMV" and "STREAM" components are removed.
- Separate output files from the working directory. Place output files into "./output/" under the project directory.
- The window title is still "The Protagonist Returns".

# v7.2.3 "Attack on Memory"

- Change task name text alignment to left by default
- Add **HUGE** page support for vs-classic and modded x265 that use mimalloc
- I forget to change the AssemblyDescription, so the window title is still "The Protagonist Returns", it's a feature!

# v7.2.2 "The Protagonist Returns"

- Recognizes ass and srt subs in mkv inputs (though no support for muxing them into the final output though, so you have to specify `"MuxOption": "Skip"` for all those ASS/SRT tracks.

# v7.2.1 "Me and You and the Student Council"

Compared to upstream v7.2 release, this release introduces the following changes:

- `OKE:PROJECTDIR` tag: the vpy script can access files under the project directory (where the json file locates) to import custom modules or plugins. It can only used at most once in a vpy.
Example:
```python
#OKE:PROJECTDIR
sdir = '.'
sys.path.insert(1, sdir) # some packages rely on having '' as sys.path[0]
import akvsfunc as akf # imports akvsfunc from the project directory
core.std.LoadPlugin(os.path.abspath(os.path.join(sdir, 'akarin.dll')))
```

- Temporary files will no longer be created in the source m2ts file and instead will be placed under the project directory.
  
