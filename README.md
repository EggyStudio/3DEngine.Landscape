# Landscape

Landscape is a library both engines, 3DEngine and BevyCSharp, take as a git submodule, for a
heightfield whose recipe is an ordered list of stamps and procedural operations applied again on
each change, erosion among them, with vegetation placed through Vegetation by slope, height and a
density map, and a splat map over a few materials. It references no engine, and each engine
implements the interfaces it declares where it binds the module.

## What it holds

| Type | What it is |
|---|---|
| `Heightfield` | Heights in meters on a grid of points, columns along X and rows along Z with Y up about the field's middle, sampled anywhere for a height, a normal and a slope |
| `Recipe` | A field's size and its ordered steps, built from flat ground each time |
| `Stamp` | A shape, `StampShape`, pressed in at a place with a radius and a height and met by a blend, `StampBlend` |
| `LandscapeGeometry` | The field cut into square tiles of triangles, each a `LandscapeMesh` of positions, normals, texture coordinates over the whole field and indices |

A recipe gives the same heights to the bit on Linux, Windows and macOS, since its steps use only
the arithmetic IEEE 754 rounds alike on every system, and a test holds a recipe's heights to a hash
on each of them. The module gives out arrays, from which an engine builds its own meshes and bodies.

`build/reading.cs` draws a recipe's field on the CPU, oblique and from above, as the pictures of a
reading, with no window and no GPU:

```bash
dotnet run build/reading.cs -- <folder> stamps
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

A clone of the engine fetches the module with `git clone --recurse-submodules`, or with
`git submodule update --init` in a clone made without it, and a workflow checks it out with
`submodules: true` on `actions/checkout`. A change the engine uses is committed and pushed here
before the engine's commit that moves its pointer, so the engine's history never names a commit
this repository does not have.

## Building and testing

With the .NET 10 SDK:

```bash
dotnet build Landscape.slnx
dotnet test Landscape.Tests
```

## License

The Mozilla Public License 2.0, in [LICENSE](LICENSE).
