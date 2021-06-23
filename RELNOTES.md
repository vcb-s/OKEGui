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
  
