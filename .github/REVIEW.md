# Review

Reviewed up to `35effea`, item 2, pushed at 12:31 on 2026-10-31 with `81d2ba2`, item 1, after the
owner's first commit, its run green at 12:35 on the three systems in about three minutes, the first
of this repository. Both commits keep the form, the three-mark subject, one sentence, the owner's
identity as author and committer, no trailer (N 7.2 and N 7.6 kept at the first commit); the library
references no engine (N 2.1). The documents are the engines' with the engine's name replaced and the
engine-only lines dropped, COMMITS.md the game's with N 7.1's pointer rule in place of the legacy
branch line, .gitignore with the dump folder, .claude and .ref; AGENTS.md's devlog row carries no
link until DEVLOG.md exists and its table of areas gains the rows item 2 adds. The skeleton sets
ImportDirectoryBuildTargets false, since a submodule under modules/ would inherit an engine's
Directory.Build.targets, makes a missing summary an error, loads the assembly by name in its one
test, and build/version.sh prints 0.1.0; the strict build 0 warnings and 0 errors, the test passing.
After it `dec676a`, the norm and the review's head alone, pushed at 14:51 on 2026-11-03, with no run
since the build passes over the review's files; it keeps the form. Before it `22b3da6`, the owner's
first commit alone.

## Now

The order the owner set on 2026-10-31 for the six modules, this one holding a heightfield whose
recipe is an ordered list of stamps and procedural operations re-applied on change, erosion as one
operation, vegetation placed by slope, height and a density map through Vegetation, and a splat map
over a few materials. The documents first, then the rest as the other projects have it, then the
engines taking the module. Items 1 and 2 are the game's session's, being free while the game waits
on the engine's package; item 3 is the session's that owns the code today; item 4 is each engine's
session's. Among the six this module comes fifth, on Modeling and Vegetation.

1. **The documents, one commit.** AGENTS.md on the game's shape (one paragraph saying what the
    module is and that no engine is referenced, N 2.1; the documents table with STYLE, COMMITS,
    REVIEW, NORM and DEVLOG; building and testing; Where things are with a row a folder; the
    conventions, the commit form among them), README.md (what the module is, how an engine takes it
    as a submodule, `dotnet build` and `dotnet test`), `.github/STYLE.md`, `COMMITS.md` and
    `CODE_OF_CONDUCT.md` as the engines' with the engine's name replaced, `.gitignore` of the
    engines' shape with `/.github/dump/` in it, `.gitattributes`. The game's AGENTS.md and the
    engines' documents are the models; nothing is written that the module does not have yet. This
    commit is the first test of the commit form here, N 7.2 and N 7.6, read by the reviewing session
    before it is pushed. Done at `81d2ba2`.

2. **The skeleton, one commit after item 1.** `Landscape.slnx`, `Landscape/Landscape.csproj` as a
    class library on net10.0 with `Nullable` and `ImplicitUsings` in `Directory.Build.props`,
    `Landscape.Tests/` on xunit with one test that the library loads, `build/version.txt` at `0.1`
    and `build/version.sh` as the engines', and `.github/workflows/build.yml` on a push, `test (os)`
    on ubuntu-24.04, windows-latest and macos-15 with `dotnet build Landscape.slnx -warnaserror` and
    `dotnet test Landscape.Tests --no-build`, a time limit on every job with its reason,
    `permissions` read and the engines' concurrency group. Green on the three systems before item 3.
    Done at `35effea`.

3. **The code's move.** The source is new; its items are written when Modeling and Vegetation stand,
the first a heightfield with stamps and a mesh from it. One commit a part, the tests coming with the
code, the strict build and the suite green, a Shared line in the engines' REVIEW.md when a part
lands. Items written by the reviewing session when item 2 is green and the owning session is free.

4. **The engines take the module.** Each engine adds this repository as a git submodule under
`modules/` and references its project, its workflows checking submodules out, its own folder for
this code becoming the binding of the module's interfaces (N 2.1) and the rest deleted; a change
used by an engine is committed here first and pushed before the engine's commit that moves the
pointer. One commit an engine, after item 3's first part.

## Verdicts

None yet.

## Decisions

1. **The repository's birth.** The owner made this repository on 2026-10-31 at 12:05 among six,
Modeling, Vegetation, Levels, RadianceCascades, Tools beside it, after the ideas recorded as
3DEngine's Decision 78 and BevyCSharp's Decision 71, to hold a heightfield whose recipe is an
ordered list of stamps and procedural operations re-applied on change, erosion as one operation,
vegetation placed by slope, height and a density map through Vegetation, and a splat map over a few
materials as one library both engines use rather than one implementation an engine. The owner set
the order the same day, the documents first, then the rest as the other projects have it, then the
engines taking the module, and said the modules may be tried at once beside the engine's global
illumination work (3DEngine Decision 79).

2. **A git submodule, not a package.** The owner chose on 2026-10-31 that the engines take the
module as a git submodule built as a project of their own, since a package's upload, wait, test and
pack is slower to iterate than a commit. A change is committed here first; the reviewing session
pushes this repository before the engine's commit that moves its pointer; the workflows check
submodules out. The game keeps using the engine through the engine's package.

3. **The module knows no engine.** N 2.1. The module references no engine, package or bridge; what
it needs of a renderer, a world, a scheduler or a file system it takes through interfaces it
declares, which each engine implements where it binds the module. A type of an engine's in this
repository is a fault.

4. **The repository's terms are the owner's.** The repository was made by the owner with its terms
   file in place, which stands as the owner set it and is spoken of nowhere else.

5. **Who writes here.** The game's session writes items 1 and 2 for the six modules, being free
while the game waits on the engine's package; the session that owns the code today moves it (item
3), BevyCSharp's for Vegetation and Levels, 3DEngine's for Modeling and the cascades, the reviewing
session's scripts going into Tools; each engine's session binds the module (item 4). Every session
reads REVIEW.md's head, its items and the Replies before a batch, as N 7.7 says, and the reviewing
session pushes.

6. **The module is worked by a session of its own.** Decided at 16:43 on 2026-11-18 for the seven
    modules at once. From its first commit the module's session alone writes the module's source,
    tests and documents, and the engines' sessions write here no longer; an engine's coder that
    needs something of the module asks the module's session in a message and takes the commit by
    moving its submodule's pointer. Every open item above that names an engine's or the game's
    session as its worker is the module's session's, apart from the binding inside an engine, which
    stays that engine's. The session's first Reply reads this file against the tree and says what is
    done, what is open and the order it proposes; the review answers, and that order stands. The
    rules are NORM.md's as before, among them no engine referenced (N 2.1), tests that need no
    window and no GPU, the commit's form, a failing test before a mend, a picture on a white ground
    where a thing is seen, and any GPU program through the lock of N 7.9 with a waiting file. The
    session reports each checked commit to the reviewing session, which reads it, pushes it and
    reads its run; it does not push.

## Replies

The first Reply, read against the tree at `34c3b63`. Items 1 and 2 are done as the head says and
the tree holds what they name, the strict build with no warning and its one test passing (dotnet
build Landscape.slnx -warnaserror, dotnet test Landscape.Tests). Four comment lines of the skeleton
run past 100 columns, two in build.yml and two in Landscape.csproj, which the first commit below
wraps. DEVLOG.md exists, so AGENTS.md's devlog row may take its link on the owner's word (N 7.4).
Item 3 is open and this session's by Decision 6, its parts proposed below, and item 4, each engine's
binding, may start after the first.

**What the module holds.** A `Heightfield`, heights in meters on a grid of columns along X and rows
along Z a spacing apart, Y up about the field's middle as Vegetation's grass patches lie, sampled
between its points bilinearly with a normal and a slope anywhere. A `Recipe`, the field's size and
an ordered list of steps, built from a flat field each time, so a level keeps the recipe rather than
the heights. The steps are stamps, a shape at a place with a radius and a height blended by adding,
by the larger, by the smaller or by replacing, and procedural operations, noise, terraces,
smoothing, thermal erosion and hydraulic erosion. From the field come a mesh a tile, a splat map
over up to eight materials four to a layer, and the places vegetation keeps by slope, height and a
density map. The heights are made with addition, multiplication, division, floor and square root,
which IEEE rounds alike on every system, and never with `MathF.Sin` or `MathF.Pow`, whose rounding
is each C runtime's, and a seeded step draws from a PCG of the module's own in a fixed order, so a
recipe gives the same bits on the three systems, which a pinned hash of its heights checks on each.

**The interface the engines bind through.** The module gives out arrays, as Levels does, a
`LandscapeMesh` a tile with positions, normals, texture coordinates over the whole field and indices
counter-clockwise from above, the splat layers as RGBA bytes, and the heights row by row as a
physics heightfield takes them, of which an engine builds its meshes, textures, bodies and entities.
It takes in one interface, `ILandscapePictures`, the heights of a picture a stamp names, read by the
engine from its own files as `ILevelMaterials` answers for a picture's size. A build is a call that
takes a cancellation token and runs on whatever thread the engine calls it from, and a recipe is
written and read as JSON on a `Stream`, so the module declares no scheduler and no file system. A
rebuild after a change gives the rectangle of points that changed, which names the tiles an engine
meshes again.

**The first three commits.**

1. **The heightfield and its stamps, with a mesh a tile.** `Heightfield`, `Recipe`, `Stamp` with the
   cone, the dome, the plateau and the crater and the four blends, `LandscapeMesh` and
   `LandscapeGeometry.Tiles`, with the skeleton's four long comment lines wrapped. Its tests hold
   that an empty recipe gives a flat field of the size asked, a dome stands at its height at its
   middle and at nothing from its radius out, a stamp over the edge is cut there, two stamps that
   overlap sum by adding and give the larger by the larger, a height between points is bilinear, a
   ramp of 45 degrees reads 45, and three stamps built twice give the same bits and a pinned hash;
   that 129 by 129 points in tiles of 64 quads make four tiles of 65 by 65 corners, two tiles'
   shared edge holds the same positions and normals in both, every triangle runs counter-clockwise
   from above and the texture coordinates run from 0 to 1 over the field; and the refusals. A relief
   of the three stamps on a white ground goes with it, drawn on the CPU by a file-based program,
   `build/reading.cs`, saved as WebP by Pillow, AGENTS.md's build row naming it on the owner's word.
2. **The procedural operations and the rebuild on change.** Fractal gradient noise by seed, scale,
   octaves and amplitude, terraces, smoothing and thermal erosion by a talus angle, and a recipe
   that keeps each step's result, so a changed step is applied again with the steps after it and
   none before. Its tests hold noise to a pinned hash a seed and within its amplitude, thermal
   erosion to the sum of the heights kept and the steepest slope brought under the talus angle
   within a tolerance measured in the commit, a rebuild after a change at one step to the full
   build's bits with that step and those after it alone run, and a moved stamp's changed rectangle
   to its old and new places, nothing outside it changed. A pair before and after the erosion goes
   with it.
3. **The splat map.** A rule a material, a height range and a slope range each with a width it
   blends over, the weights at every point summing to one and packed four materials a layer as RGBA
   bytes that sum to 255 by largest remainder. Its tests hold both sums at every point, a material's
   weight at nothing outside its ranges, a steep face going to the rule for rock, and the refusals
   of no rule, more than eight and a range upside down. The relief colored by four materials goes
   with it.

After them come hydraulic erosion by droplets, the placement, and the recipe as JSON with stamps of
pictures through `ILandscapePictures`.

**The placement and Vegetation, on the owner's word.** Vegetation gives a forest as places with
seeds and grass as clumps about a patch's middle, on level ground. Landscape would take the places
as they come, `(Vector3 Place, uint Seed)`, keep each by slope, height and the density map with its
seed drawn against the density, and lift it to the ground with the ground's normal, the engine
handing one module's output to the other, with no reference to Vegetation's project. A reference
would need Vegetation checked out beside this repository in its workflow and in both engines at
commits that agree, a dependency the owner decides, so the placement waits for that word and the
first three commits do not.

Division and square root give the same bits on the three systems because IEEE 754 requires each to
round to the float nearest the exact result, which the SSE instructions of the Linux and Windows
runners and the ARM64 instructions of the macOS runner do, and .NET's JIT fuses a multiplication and
an addition into one rounding only where `Math.FusedMultiplyAdd` is called, which the module never
does, so every step rounds alike everywhere. The slope's `MathF.Acos` reads a field and makes none
of its heights.

Item 3's first part in the commit carrying this line. `Heightfield`, `Recipe`, `LandscapeStep`,
`Stamp` with `StampShape` and `StampBlend`, `LandscapeMesh` and `LandscapeGeometry`, with 24 tests
beside the one that loads the library, the pinned hash of three stamps among them, read on Linux and
held on Windows and macOS by the workflow. N 4.11's test, `CommentWidthTests`, reads every comment
line of the sources the repository keeps, and failed on the skeleton's four lines before they were
wrapped. `build/reading.cs` draws a recipe's tiles on the CPU, laid out by Tools' `sheet.py`, and
the reading is `2026-11-18-item-3-stamps.webp`. AGENTS.md's devlog row takes its link and its build
row names the program. The strict build is clean and the tests pass, 25 where there was 1 (dotnet
test Landscape.Tests).

Item 3's second part in the commit carrying this line. `Noise`, fractal Perlin noise of unit
gradients a function of the place in meters, so two spacings agree at their shared points;
`Terrace`, a flat tread and a smooth riser; `Smoothing`, binomial passes; `ThermalErosion`, ground
slumping to a talus given as a rise over a run; and `LandscapeBuilder`, which keeps the field each
step left, rebuilds from the first step that differs, and gives a `LandscapeChange` with the
`LandscapeArea` of points whose bits changed and the tiles whose corners or normals that reaches.
The library's internals are visible to its tests, which apply a step to a field they made. The
sheet's contour panel showed the erosion against four neighbors slumping a cone into a square
pyramid, its diagonal faces at 1.4142 times the talus, and a test of the slope along both axes at
once failed so before the mend to eight neighbors, the diagonals over the square root of two, which
holds it to 1.0824, one over the cosine of 22.5 degrees. The tolerances are measured in the test's
comment and the commit's sentence (N 3.5), and two new hashes are pinned, noise of one seed and the
five operations together. The reading is `2026-11-18-item-3-erosion.webp`, before, against four and
against eight. `dotnet run` of a file-based program reused its cached build after the library
changed and drew the old erosion again, so the README and the program name `--no-cache`. The
strict build is clean and the tests pass, 47 where there were 25.

**Rule:** on the owner's direction for a stranger's engine, one public shape fits our two engines
alone. `LandscapeMesh` gives positions with Y up in right-handed axes and triangles
counter-clockwise from above, which a Z-up or left-handed engine takes by swapping two axes and the
winding. I propose to keep the shape and state the convention in the README's binding section with
the swap a Z-up engine makes, rather than give the mesh an axis option, since every engine converts
once where it builds its mesh.
