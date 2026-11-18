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

7. **The module is agnostic enough that a stranger binds it in an engine of their own from its
    README alone (2026-11-18 17:28).** It asks a README section, Binding it in your engine, with
    every interface the module declares, the conventions it stands on and the smallest binding that
    works, a small host in the tests that implements those interfaces with no engine and that the
    section's steps are run against, a test that fails where the library references an engine, a
    window or a graphics API, the form in the Tools module's ReferenceTests.cs, and no engine's name
    in a public member or its summary. The two engines appear in the README as examples. A public
    shape that fits the two engines alone is named in a Reply before it changes.

8. **The package id and the assembly are 3DEngine.Landscape and the namespace stays Landscape
    (2026-11-18 17:28).** The prefix gives a unique name. A package is published when the time is
    right and on the owner's word, for a reader who takes the module alone. The engines keep taking
    the module as a git submodule, compiled with them and carried inside each engine's one package.
    Folders and project file names stay.

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

The names in the commit carrying this line. The library's project builds the assembly and names
the package `3DEngine.Landscape`, `IsPackable` staying false, and the tests' assembly is
`3DEngine.Landscape.Tests`, the folders, the project files and the namespace `Landscape` as they
were. The test that loads the library loads it by the new name and checks a type of its own is in
it, the internals are visible to the tests under theirs, and the README names the assembly and the
namespace. The tests pass, 47.

Item 3's third part in the commit carrying this line. `SplatRule` gives a material's range of
height and of steepness, each with a blend beyond it by Hermite's smooth step, and `SplatMap.Of`
weighs up to eight materials at every point, the weights brought to one and the first material
covering a point no rule reaches, and packs them four a layer as RGBA bytes summing to 255 by the
largest remainders. Steepness is `Heightfield.Steepness`, a rise over a run, so the map is the
field's own arithmetic on every system, and the remarks give the half-texel move an engine makes to
sample a layer with a tile's texture coordinates. Seven tests come with it, the sums at every
point, a material at nothing beyond its ranges, a steep flank wholly rock and level ground wholly
grass, the first material where no rule reaches, five materials in two layers, the refusals, and a
pinned hash of the layer's bytes. The reading is `2026-11-18-item-3-splat.webp`, drawn by
`build/reading.cs` blending four colors by the weights. The strict build is clean and the tests
pass, 54 where there were 47.

The owner's first point in the commit carrying this line. The README's section, Binding it in your
engine, says the module declares no interface, since it calls no renderer, window, scheduler or
file system and gives out arrays an engine makes its own things of, and tables the conventions, the
meter, Y up and right-handed, the winding, the heights' order, the texture coordinates, the splat
layers as linear weights, no light or color, and the threads. It gives the corner a Z-up engine, a
left-handed one and a left-handed Z-up one takes and which of them reverse the winding, the
half-texel a splat layer is sampled with, and the smallest binding, three calls of the engine's own,
which the next commit's host runs as written.

The owner's second point in the commit carrying this line. `BindingTests` holds a host with no
engine whose three calls keep copies of what they are given, and runs the README's smallest binding
word for word between two markers, a second test reading the README's block back against those
lines. After an edit that moves a cone the host holds the same tiles, splat layer and heights as a
host bound to the edited recipe from nothing, so the tiles a change reaches are every tile it
changes, and the edit made 4 of the 16 tiles again, the four its places span. The README's comment
in the block is wrapped so it holds to 100 columns where the host indents it. The tests pass, 56.

Hydraulic erosion in the commit carrying this line, the next part in the order after the three.
`HydraulicErosion` runs droplets one after another by Beyer's rule of 2015 from the module's PCG,
with addition, multiplication, division and square root alone. Two faults showed on the sheet.
Beyer's numbers are given for heights from nothing to one, and in meters a droplet's speed and
capacity grew past any fall and wore hills of 40 m to plains, so the step takes a scale, 40 m by
default, that its fall is reckoned against for capacity and speed, read from the trial of 10, 40 and
160 m in the dump with no test of its own, since it is a tuning. And the field's edge was worn by
every droplet that left over it and filled by none, which dug pits hundreds of meters deep and
deeper with each droplet their slopes drew, a ramp draining off its edge dug 16.4 m below it; the
edge points are held as the base level, which no droplet wears or fills, and the ramp's test, which
failed so before the mend, reads 0.009 m. Six tests come with it, the pinned hash of 20,000 droplets
on a cone, the flank worn by 6,911 cubic meters and the foot filled by 5,945, a closed bowl's volume
to 0.0007, the ramp, level ground staying level and the refusals. 150,000 droplets over 193 by 193
points still wear the hills far lower, so the count is the strength. The reading is
`2026-11-18-item-3-droplets.webp`, before, the edge worn and the edge held. The strict build is
clean and the tests pass, 62 where there were 56.

The scale's test in the commit carrying this line, with a correction to the Reply above. The hills
worn to plains were the edge fault's, which stood in that render, and not the scale's. With the
edge held, 66,000 droplets over a dome of 40 m reckoned against 1 m keep 80.3 hundredths of its
peak and leave 749 pits, points lower than all eight about them, where against 40 m they keep 85.55
and leave 330, and a dome of 160 m against 160 m keeps 85.56 and leaves 266, so the scale decides
clean gullies against a pitted ground and a share of the peak, and a hill against its own height
erodes alike at either size. The test holds both domes to 84 hundredths of their peaks and 500 pits
at most, and the scale's remarks say so. The tests pass, 64.

Tools' shared checks in the commit carrying this line, after Levels' trial. The repository takes
Tools as a submodule at `modules/Tools` pinned at `f92df1c`, the test project references it with
the `NeedsTools` target that stops a clone without it, the workflow checks submodules out, and
`ModuleTests` holds the README's four facts with `Library`, `Root` and `Host` set above them. The
host's markers are Tools' two lines. The local check of the README's block against the host goes,
since the fourth fact holds it, and the comment width test stays, since Tools holds none, and skips
`modules/` so it reads the module's sources alone. The library and its package take nothing of
Tools'. On the first run the references, the engine names and the binding block passed, and the
README's naming of the public types failed on `LandscapeStep`, which no local copy checked, so the
README's row for `Recipe` names it. AGENTS.md takes a row for `modules`. The tests pass, 67.

The placement in the commit carrying this line. `Placement.Keep` takes the places a planting gives,
a place and a seed each, as Vegetation's forest gives them, with no reference to its project, and
keeps each where a number drawn from its own seed falls below its chance, the `PlacementRule`'s
shares of height and steepness, the ranges and blends a `SplatRule` has, times a `DensityMap`'s
share, which an engine paints a point each or takes from a splat material's weights. A kept place is
a `Planted`, lifted to the ground's height with the ground's normal and its seed, in the order it
came, and a place off the field is not kept. `Heightfield` gains a steepness anywhere over the field
and whether a place lies over it. Eight tests come with it, the places on the ground with their
normals and seeds, nothing kept outside the ranges and blends and everything well inside them, a
density of a half keeping between 48 and 52 hundredths of 7,225 places and of nothing none, a seed
deciding its place whatever comes before it, a splat material as a density map, places off the
field, the refusals and a pinned hash of the kept places. The reading is
`2026-11-18-item-3-placement.webp`, trees on the splat ground standing on grass alone. DEVLOG.md is
linked from the README's top line and AGENTS.md names its command, `python3 -I
modules/Tools/scripts/devlog.py . Landscape`, and regenerated it holds 2 days, 5 pictures and 2
items done. The strict build is clean and the tests pass, 75.

**Rule:** the README's opening says each engine implements the interfaces the module declares,
where the module declares none and its binding section says so. The words under the header stand
as written (STYLE.md), so they wait for the owner's word.

The recipe as JSON and the picture stamps in the commit carrying this line. `Recipe.Write` and
`Recipe.Read` keep a recipe as JSON on a stream, format 1, each step by its kind in camel case, an
enumeration by its name and a vector as two numbers, each number the shortest text that reads back
to the same float, written by the source generator so an engine that trims reads it with no
reflection. A file of another format, a step the module has not, a property no step has and a
number a step refuses are each refused as a `JsonException` naming it. `PictureStamp` presses a
picture's heights in over a rectangle, drawn toward by the stamps' blends and faded in over an edge,
the picture a `HeightPicture` an engine gives by its name through `ILandscapePictures`, the one
interface the module declares, which `Recipe.Build` and `LandscapeBuilder` take. The README's
opening and its binding section say so, the stale fact the review named mended, and the conventions
give the pictures' rows and the recipe's file. Tools moves to `7d56e0f` and the four facts take its
revised form, a fault's whole message given to `Assert.True`. Seven tests come with it, 12 cases,
among them a recipe of every step read back equal and building the same bits with a pinned hash,
the file of one stamp as written, six refusals by name, and the picture stamp's rectangle, edge,
missing picture and rebuild. The reading is `2026-11-18-item-3-picture.webp`. The strict build is
clean and the tests pass, 87 where there were 75.

The cheatsheet in the commit carrying this line. Tools moves to `3720852`, `ModuleTests` holds
the fifth fact, and CHEATSHEET.md is written by `Cheatsheet.Write` with `CHEATSHEET_WRITE=1`
under a short opening by hand, linked from the README's top line beside the devlog and named in
AGENTS.md's documents table with its command. It lists 25 types and 149 members. The first writing
showed the four positional records, `LandscapeChange`, `LandscapeArea`, `Planted` and
`LandscapeMesh`, with constructors of no summary, since a record's primary constructor takes none
of its own, so each is a record with a constructor and properties written out and a summary each,
the same members and equality. The tests pass, 88.

The cost reading in the commit carrying this line. `build/bench.cs` builds a recipe of every step at
513, 1,025, 2,049 and 4,097 points a side, each step through a builder's rebuild less its own
overhead, each stage the fastest of three runs after a first pass at 513 that is not reported, under
the exclusive lock with its waiting file, its runs of under three minutes each. Every stage
allocates the same bytes a point at every size, and the most were the tiles', 57 bytes a point, 24
of them an index array a tile though every whole tile's indices are the same, so a bound of 34
failed at 56.94 first and the tiles of one size share one array of indices, 32.99 bytes a point
after and 957 MB at 4,097 a side falling to 554, the mesh's remarks saying an engine reads the
shared indices and never writes them. Hydraulic erosion's time a point rises at 4,097, 583 ns where
it is 400 to 434 below, a field of 67 MB that no cache holds, and the noise and the splat map grow
cheaper a point as the bench widens the noise with the field, its lattice's cells and the materials
repeating across more neighbors. A first run's smaller fields ran before the runtime had optimized
the code, which the warm-up pass removed for the splat map and not for the noise, and tiered
compilation turned off slowed the droplets, so the bench keeps it on. The reading is
`2026-11-18-item-3-bench.webp` with its numbers in `2026-11-18-item-3-bench.md`, and AGENTS.md's
build row and the README name the bench. In the same commit the owner's rules of today. The two
lines naming an engine by its name and the five more that spoke of the two engines, seven in all,
read 0 after in both searches, the build's and the props' comments, the README's and AGENTS.md's
openings and the binding section. The README opens with its link row above the title, and cheatsheet
is one word in the row and the page's title. Tools moves to `75d77f2` with its facts of the link row
and of the tracked text naming no engine. DEVLOG.md holds 2 days, 7 pictures and 2 items done,
CHEATSHEET.md is regenerated, and the tests pass, 91 where there were 88.

The next three, from what a stranger binding the module meets first, each opening with a test that
fails.

1. **Tiles an engine can cull and draw far off.** A field of 4,097 points a side is 16.8 million
   corners, which no engine draws whole, and a tile gives no bounds to cull it by. `LandscapeMesh`
   gains its bounds, and `LandscapeGeometry.Tile` takes a step, a corner every 1, 2, 4 and on up to
   the tile's cells, with skirts, a ring of corners hung below each edge by a depth
   `LandscapeGeometry.SkirtDepth` reckons from the field and the coarsest step, so two tiles at
   different steps meet with no crack. The coarse corners are the field's own points, so neighbors
   share them to the bit. Its tests come first and fail: a tile's corners at each step, its bounds
   tight about every corner, no crack at any sample of an edge between tiles at steps 1 and 8, the
   skirts facing outward, one index array a size and step, and the refusals of a step that does not
   divide the tile.

2. **A ray onto the ground.** An editor places a stamp where the pointer falls and a game drops what
   falls, and neither can without a ray met with the field. `Heightfield.Raycast` walks the ray cell
   by cell over the field and meets each cell's two triangles, the mesh's own, giving the place, the
   distance and the normal, or nothing. Its tests fail first: a ray straight down onto a dome
   meeting its height, a slanted ray meeting a ramp where the arithmetic puts it, a ray grazing past
   an edge and one leaving the field missing, and the hit lying on the drawn triangles to a
   millimeter.

3. **A splat map made again over a change alone.** A rebuild at 4,097 a side makes the whole splat
   map again in about a second, where an edit changes a few tiles. `SplatMap.Update(field, area)`
   weighs the points of a changed area and the point beyond it, and the README's binding block
   uploads the layers' rectangle. Its tests fail first: an update over a moved stamp's area equal to
   a whole map of the new field to the byte, and nothing outside the area touched.

The droplets' 583 ns a point at 4,097 come after them. Ordering the droplets by tile would keep the
field in the caches, but every pinned hash of the step would move and the step runs at authoring
time, where the three above run at every edit or every frame. The first starts now.

The first of the three in the commit carrying this line, tiles an engine culls and draws far off.
Its tests came first and failed for want of the members. `LandscapeMesh` holds its `Bounds`, the
least and the greatest corner of the box about every corner, made in its constructor so two equal
tiles stay equal. `LandscapeGeometry.Tile` and `Tiles` take a step, a corner every step-th point of
the field and its last where a tile is cut short, so a coarse tile's corners are the field's own
points with their normals and texture coordinates and two tiles at different steps share the
coarser's corners along their edge to the bit, and a skirt, a corner below each edge corner and two
triangles a segment facing out of the tile. `SkirtDepth` gives the skirt for the coarsest step a
binding draws, twice the most any step up to it stands off a tile's edge and a millimeter, and
`StepError` the most in meters a tile at a step stands off the points it covers, read against the
triangles it draws, by which a binding picks its steps by distance or by the error on screen, as the
README's conventions say. The indices stay one array a size, a step and a skirt. Eight tests, 11
cases, hold the corners at four steps, the coarse corners the field's, the bounds tight, no crack at
any of the 65 points of an edge between tiles at steps 1 and 8, the skirt's triangles facing out and
the surface's up, the shared indices, the error against every point read under the coarse triangles,
and the refusals. The reading is `2026-11-18-item-3-distance.webp`, tiles at steps 1, 4 and 16 by
distance with white cracks along the borders without skirts and none with them. The tests pass, 102
where there were 91.

The second of the three in the commit carrying this line, a ray onto the ground. Its tests came
first and failed for want of the member. `Heightfield.Raycast` cuts a ray to the field's rectangle
and walks it a cell at a time in the order it crosses them, meeting each cell's two triangles as the
tiles draw them at a step of one, from either side, and gives a `RayHit` of the place, the distance
and the field's normal there, or nothing past its reach, off the field or above the ground. Five
tests hold a ray straight down meeting a dome at its point's height, a slanted ray meeting a ramp at
50 thirds of a meter along it, rays above everything, leaving the field and out of reach missing and
one coming in from beyond the edge meeting, 60 rays at random against the nearest crossing of every
one of the tiles' 8,192 triangles to a millimeter, and the refusals of a ray of no way and an origin
not finite. The test of the dome first asked the hit's height exactly and the ray's arithmetic gave
it 3 units of the last place apart, so it reads to a ten-thousandth. The README's row for the field
names the ray and `RayHit`. The cheatsheet's writer prints a default of infinity as `Infinityf`,
which C# does not read, in the ray's reach and in the two rules' ranges, a finding for Tools. The
tests pass, 107 where there were 102.

The third of the three in the commit carrying this line, a splat map weighed again over a change
alone. Its tests came first and failed for want of the member. `SplatMap` keeps its rules, and
`SplatMap.Update` weighs again the points of a rebuild's changed area and those beside it, whose
steepness reads the changed points, writes the layers in place and gives the rectangle of texels it
weighed, cut to the field. The README's binding block gives the engine that rectangle of each layer
by a fourth call where it made the whole map again, the host in the tests copying the rectangle a
row at a time as a texture's region is uploaded, and the host bound through the edit holds the same
layer as one bound to the edited recipe from nothing, so the update reaches every texel the change
does. Three tests hold an update over a moved cone equal to a whole map of the new field to the byte
and the weight, the texels outside the grown rectangle as they were, and an area at the edge cut to
the field with another field's size and an area past the field refused. The tests pass, 110 where
there were 107.
