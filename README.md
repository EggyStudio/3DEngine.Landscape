[Devlog](https://github.com/EggyStudio/Landscape/blob/main/DEVLOG.md) [Cheatsheet](https://github.com/EggyStudio/Landscape/blob/main/CHEATSHEET.md)

# Landscape

Landscape is a library any engine on .NET takes as a git submodule, for a heightfield whose recipe
is an ordered list of stamps and procedural operations applied again on each change, erosion among
them, with vegetation placed through Vegetation by slope, height and a density map, and a splat map
over a few materials. It references no engine, and an engine whose recipes stamp pictures implements
the one interface it declares, `ILandscapePictures`, where it binds the module.

## What it holds

| Type | What it is |
|---|---|
| `Heightfield` | Heights in meters on a grid of points, columns along X and rows along Z with Y up about the field's middle, sampled anywhere for a height, a normal and a slope, and met by a ray in `Heightfield.Raycast`, which gives a `RayHit` where the ray first meets the drawn ground |
| `Recipe` | A field's size and its ordered steps, each a `LandscapeStep`, a stamp or an operation of the module's, built from flat ground each time |
| `Stamp` | A shape, `StampShape`, pressed in at a place with a radius and a height and met by a blend, `StampBlend` |
| `Noise`, `Terrace`, `Smoothing`, `ThermalErosion`, `HydraulicErosion` | The procedural operations, fractal gradient noise from a seed, terraces with a flat tread, a binomial blur, ground slumping to its talus, and droplets from a seed cutting channels and filling the feet of slopes |
| `LandscapeBuilder` | A recipe built and built again after each change from the first step that changed, each rebuild giving a `LandscapeChange` with the `LandscapeArea` of points it changed and the tiles that reaches |
| `SplatMap` | A weight a point for each of up to eight materials by a `SplatRule` each, a range of height and of steepness with blends, summing to one, and the same as RGBA bytes four materials a layer, summing to 255 |
| `PictureStamp` | A picture's heights pressed in over a rectangle, a share of its height a texel, drawn toward by a blend and faded in over an edge, the picture a `HeightPicture` the engine reads through `ILandscapePictures` |
| `Placement`, `PlacementRule`, `DensityMap` | The places a planting gives, as a place and a seed each, kept by a rule's ranges of height and steepness and a density map, painted or taken from a splat material, and lifted onto the ground as `Planted` with the ground's normal |
| `LandscapeGeometry` | The field cut into square tiles of triangles, each a `LandscapeMesh` of positions, normals, texture coordinates over the whole field, indices and its bounds, at a step of every point or coarser with a skirt below its edges for a tile drawn far off |

A recipe gives the same heights to the bit on Linux, Windows and macOS, since its steps use only
the arithmetic IEEE 754 rounds alike on every system, and a test holds a recipe's heights to a hash
on each of them. The module gives out arrays, from which an engine builds its own meshes and bodies.

`build/reading.cs` draws a recipe's field on the CPU, oblique, from above and in contours, as the
pictures of a reading, with no window and no GPU. `--no-cache` builds the library again, since a
file-based program's cached build does not follow a change to a project it references:

```bash
dotnet run --no-cache build/reading.cs -- <folder> <stamps|erosion-before|erosion-after|droplets-before|droplets-after|splat|placement|picture|distance|distance-bare>
```

`build/bench.cs` builds a recipe of every step at 513, 1,025, 2,049 and 4,097 points a side and
prints each stage's time and bytes a point, a timing to run alone on the machine:

```bash
dotnet run -c Release --no-cache build/bench.cs -- <numbers.json>
```

## Taking it into an engine

An engine adds the repository as a git submodule under `modules/` and references its project, here
from a project one folder below the engine's root.

```bash
git submodule add https://github.com/EggyStudio/Landscape.git modules/Landscape
```

```xml
<ProjectReference Include="..\modules\Landscape\Landscape\Landscape.csproj" />
```

The project builds the assembly `3DEngine.Landscape`, whose types are in the namespace `Landscape`.

A clone of the engine fetches the module with `git clone --recurse-submodules`, or with
`git submodule update --init` in a clone made without it, and a workflow checks it out with
`submodules: true` on `actions/checkout`. A change the engine uses is committed and pushed here
before the engine's commit that moves its pointer, so the engine's history never names a commit
this repository does not have.

## Binding it in your engine

The module needs nothing of an engine but the pictures a `PictureStamp` names. It declares one
interface, `ILandscapePictures`, through which the engine gives a picture's heights by its name as
a `HeightPicture`, read from its own files, and it calls no renderer, window, scheduler or file
system. What it gives out is arrays of .NET's own types and `System.Numerics`' vectors, which an
engine turns into its own meshes, textures and bodies, and any engine on .NET 10 binds it so. A
recipe with no picture stamp needs no interface at all, as the smallest binding below shows.

### Conventions

| What | The module's convention |
|---|---|
| Units | Meters for every length and height |
| Axes | Y up and right-handed, the field's columns along X and its rows along Z, about the field's middle |
| Winding | A tile's triangles run counter-clockwise seen from above, the front faces with Y up and right-handed axes |
| Heights | A row at a time, each row along X and the rows along Z, the order a physics heightfield takes |
| Texture coordinates | U along X and V along Z, from nothing at the field's first point to one at its last |
| Splat layers | RGBA bytes, a texel a point, four materials' weights a layer, linear values and not sRGB color |
| Distance | A binding picks each tile's step itself, by its distance or by the error on screen it accepts, `LandscapeGeometry.StepError` giving the meters a tile at a step stands off the field's points, and hangs every tile's skirt at the depth `LandscapeGeometry.SkirtDepth` gives for the coarsest step it draws, so tiles at different steps meet with no crack |
| Pictures | A row at a time from the stamp's least Z, each along X, the order an image file is read from its top, its gray a share of the stamp's height |
| Recipes | JSON on a `Stream`, by `Recipe.Write` and `Recipe.Read`, format 1, each step by its kind and each number as the shortest text that reads back to the same float |
| Light and color | None, the module holds no light, no color and no exposure |
| Threads | A build runs on the thread that calls it, stops on its cancellation token, and shares the rows of its stamps, noise, terraces and smoothing, and a splat map's and the tiles', among the threads a `BuildOptions` allows, the erosions on one, so it gives the same bits at any count; it reports each step it applies as a `BuildProgress`, and one builder serves one thread at a time |

An engine with Z up and right-handed axes takes a corner `(x, y, z)` as `(x, -z, y)`, a turn about
X that keeps the winding. An engine with left-handed axes and Y up takes it as `(x, y, -z)`, and an
engine with left-handed axes and Z up as `(x, z, y)`; either is a mirror, so it takes each
triangle's corners in the other order as well. Normals turn as positions do, and an engine whose
unit is not the meter scales positions and heights by its units a meter.

A splat layer has a texel a point, its middle half a texel in from the edge, so a tile's texture
coordinates sample it as `uv * (n - 1) / n + 0.5f / n`, where `n` is the field's columns for U and
its rows for V.

### The smallest binding

The engine's side is four calls of its own, one making a tile's mesh, one uploading a splat layer
as a texture and one a rectangle of it, and one giving the heights to its physics, here on an
object named `engine`. The module builds the recipe, cuts the field into tiles and, after an edit,
says which tiles and which texels the edit reached.

```csharp
var builder = new LandscapeBuilder(recipe);
var field = builder.Field;
foreach (var tile in LandscapeGeometry.Tiles(field))
    engine.MeshTile(tile.Column, tile.Row, tile.Positions, tile.Normals, tile.Uvs, tile.Indices);
var splat = SplatMap.Of(field, rules);
for (var layer = 0; layer < splat.Layers.Count; layer++)
    engine.SplatLayer(layer, splat.Columns, splat.Rows, splat.Layers[layer]);
engine.CollisionHeights(field.Columns, field.Rows, field.Spacing, field.Heights);

// After an edit, the tiles the change reached are made again and the rest kept, the
// splat layers' texels it reached are weighed again and given, and so are the heights.
var change = builder.Rebuild(edited);
field = builder.Field;
if (change.Changed is { } area)
{
    foreach (var (column, row) in area.Tiles(field))
    {
        var tile = LandscapeGeometry.Tile(field, column, row);
        engine.MeshTile(column, row, tile.Positions, tile.Normals, tile.Uvs, tile.Indices);
    }

    if (splat.Update(field, area) is { } weighed)
    {
        for (var layer = 0; layer < splat.Layers.Count; layer++)
            engine.SplatRegion(layer, weighed, splat.Columns, splat.Layers[layer]);
    }

    engine.CollisionHeights(field.Columns, field.Rows, field.Spacing, field.Heights);
}
```

## Building and testing

The tests take the checks every module keeps from the Tools module, a git submodule under
`modules/Tools`, so a clone fetches it with `git clone --recurse-submodules`, or with
`git submodule update --init` in a clone made without it. With the .NET 10 SDK:

```bash
dotnet build Landscape.slnx
dotnet test Landscape.Tests
```

## License

The Mozilla Public License 2.0, in [LICENSE](LICENSE).
