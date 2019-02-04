# Loden
OpenSimulator region module that creates and maintains a level of detail version
of the region (makes it LOD'en).

The name is a play on the action of making a "level of detail" ("LOD") version
of the [OpenSimulator] region -- to LOD'en the region.
There is a color named "loden green" and a [loden cape]
which lends itself to a logo and color theme.

## Installation
At the moment, one must build this module with the [OpenSimulator] sources.
Eventually there will be a DLL to drop into a running OpenSimulator installation.

```bash
cd DirectoryRootOfOpenSimulatorSources
cd addon-modules
git clone https://github.com/Herbal3d/Loden.git
cd ..
./runprebuild.sh    # or ./runprebuild.bat
compile
```

## What It Does
Once a region has loaded, Loden scans all of the objects in the region,
groups all the objects into areas, and creates unique hashes for all
the objects and their groupings.
From that, Loden can see if objects and groupings of objects have been
created already.

From that analysis, the [OpenSimulator] primitives and meshes are
converted into a [Basil viewer] compatable format
If groups have not been created, they are built and added to
the quad-trees.

There is also the concept of 'layers' what are overlapping 3D spaces
that are union'ed together. Loden can create multiple layers for the
region. Initially I'm thinking 'static', 'dynamic' (scripted or physical),
and 'avatar' layers. There could be multiple of each.

The converted format is expected to be close to (if not compatible to)
the [CesiumJS] [3D tile] format. This models a hiearchy of bounding boxes
that give decreasing level of detail as the bounding boxes get larger.

Over time, the actual rules of grouping and model simplification will change.

[OpenSimulator]: https://opensimulator.org
[loden cape]: https://en.wikipedia.org/wiki/Loden_cape
[CesiumJS]: https://cesiumjs.org/
[3D tile]: https://github.com/AnalyticalGraphicsInc/3d-tiles
[Basil viewer]: http://blog.misterblue.com/basil/

