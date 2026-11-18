# Landscape cheatsheet

Every public type of `3DEngine.Landscape` with its members, a line each of its declaration and the
first sentence of its summary. A field starts as a `Recipe` of steps, is built and built again by a
`LandscapeBuilder`, and is given out as tiles by `LandscapeGeometry`, a `SplatMap` and the places
`Placement` keeps. The README's section Binding it in your engine gives the conventions and the
smallest binding. The list below is written from the built library by `Cheatsheet.Write`, and a
test fails where it differs, so it is mended by running the tests with `CHEATSHEET_WRITE=1`.

<!-- written by Cheatsheet.Write from the built library -->
```csharp
interface ILandscapePictures                             // What an engine says of the pictures a recipe's PictureStamp names, the heights each holds, read from the engine's own files (N 2.1)
HeightPicture? Heights(string name);                     // The heights a picture holds, or nothing where the engine has no picture of the name

class Heightfield                                        // Heights in meters on a grid of points a spacing apart, columns along X and rows along Z, with Y up and the grid about its own middle, which a recipe builds and an engine draws, collides with and plants on
Heightfield(int columns, int rows, float spacing);       // A flat field at height zero
int Columns { get; }                                     // How many points a row has along X
int Rows { get; }                                        // How many rows of points the field has along Z
float Spacing { get; }                                   // The distance in meters between two points beside each other
float[] Heights { get; }                                 // The heights in meters, a row at a time, each row along X and the rows along Z
Vector2 Size { get; }                                    // The field's width along X in X and its depth along Z in Y, in meters, from its first point to its last
float this[int column, int row] { get; set; }            // The height of a point in meters
Vector2 Place(int column, int row);                      // Where a point lies across the ground, its X in X and its Z in Y, about the field's middle
float Height(Vector2 place);                             // The height in meters anywhere over the field, between its points bilinear and past its edge the edge's
Vector3 Normal(int column, int row);                     // The way up out of the ground at a point, at unit length
Vector3 Normal(Vector2 place);                           // The way up out of the ground anywhere over the field, at unit length, between its points blended from theirs
float Slope(Vector2 place);                              // How steep the ground is anywhere over the field, the angle in radians between its normal and up
float Steepness(int column, int row);                    // How steep the ground is at a point as a rise over a run, the rise along X and along Z taken together, nothing on flat ground and one at 45 degrees
float Steepness(Vector2 place);                          // How steep the ground is anywhere over the field as a rise over a run, between its points blended from theirs
bool Covers(Vector2 place);                              // Whether a place lies over the field, its edge counted in
RayHit? Raycast(Vector3 origin, Vector3 direction, float reach = Infinityf);  // Where a ray first meets the ground the field's tiles draw, or nothing
Heightfield Copy();                                      // A field of the same size holding the same heights, apart from this one

record struct RayHit                                     // Where a ray met the ground, how far along it and the ground's way up there
RayHit(Vector3 place, float distance, Vector3 normal);   // A hit of its place, its distance and the ground's normal
Vector3 Place { get; init; }                             // Where the ray met the ground, in meters about the field's middle
float Distance { get; init; }                            // How far along the ray from its origin, in meters
Vector3 Normal { get; init; }                            // The way up out of the ground there, at unit length, the field's own

record Recipe                                            // A field's size and the ordered steps that make its heights, which a level keeps in place of the heights and builds again after each change
int Format = 1;                                          // The format a recipe is written in, which Read takes and no other
Recipe(int columns, int rows, float spacing, IReadOnlyList<LandscapeStep> steps);  // A recipe of a field's size and its steps
int Columns { get; }                                     // How many points a row of the field has along X
int Rows { get; }                                        // How many rows of points the field has along Z
float Spacing { get; }                                   // The distance in meters between two points beside each other
IReadOnlyList<LandscapeStep> Steps { get; }              // The steps in the order they are applied
Heightfield Build(CancellationToken cancel = default);   // Builds the field from flat ground by the steps in their order
Heightfield Build(ILandscapePictures? pictures, CancellationToken cancel = default);  // Builds the field from flat ground, its picture stamps read through the engine's pictures
void Write(Stream stream);                               // Writes the recipe as JSON, its format, its size and its steps each by its kind
Recipe Read(Stream stream);                              // Reads a recipe Write wrote
bool Equals(Recipe? other);                              // Whether two recipes hold the same size and the same steps in the same order
int GetHashCode();                                       // A hash of the size and the steps, equal for equal recipes

record LandscapeStep                                     // One step of a Recipe, a stamp or a procedural operation, which changes the heights a field holds when the recipe is built

record Stamp                                             // A shape pressed into the field at a place, raising or lowering the points within its radius by its profile and its blend
Stamp(StampShape shape, Vector2 place, float radius, float height, StampBlend blend = StampBlend.Add);  // A stamp of a shape at a place
StampShape Shape { get; init; }                          // The profile it rises by
Vector2 Place { get; init; }                             // Its middle across the ground, X in X and Z in Y, about the field's middle
float Radius { get; init; }                              // How far from its middle it reaches, in meters
float Height { get; init; }                              // Its height at the profile's top in meters, below zero for a hollow
StampBlend Blend { get; init; }                          // How it meets the heights before it
float Profile(float distance);                           // The share of its height the profile gives at a distance from its middle

enum StampShape                                          // The profile a Stamp rises by from its radius to its middle, as a share of its height
Cone = 0,                                                // A straight rise from nothing at the radius to the whole height at the middle
Dome = 1,                                                // A round top that meets the ground with no edge, one less the square of the distance over the radius, squared
Plateau = 2,                                             // A flat top across the inner half of the radius and a smooth fall from there to nothing at the radius
Crater = 3,                                              // A rim at the whole height seven tenths of the way out, falling smoothly to nothing at the radius outside and to a bowl four tenths of the height below the ground at the middle

enum StampBlend                                          // How a Stamp meets the heights the steps before it left
Add = 0,                                                 // The stamp's height is added to the ground's
Max = 1,                                                 // The ground takes the higher of its own height and the stamp's
Min = 2,                                                 // The ground takes the lower of its own height and the stamp's
Replace = 3,                                             // The ground is drawn toward the stamp's whole height by the share its profile gives, so the middle of a plateau stands at the height and its edge keeps the ground's, as a flattened place for a building is made

record Noise                                             // Fractal gradient noise added to the field, octaves of Perlin's noise each half the size of the one before and weighted by the roughness, drawn from a seed
Noise(uint seed, float scale, int octaves, float amplitude, float roughness = 0.5f);  // Noise of a seed, a size, a count of octaves and an amplitude
uint Seed { get; init; }                                 // The seed its permutation and its octaves' offsets are drawn from
float Scale { get; init; }                               // The first octave's wavelength in meters, the size of its largest features
int Octaves { get; init; }                               // How many octaves are summed, from one to sixteen
float Amplitude { get; init; }                           // How far in meters the noise reaches above and below the ground
float Roughness { get; init; }                           // Each octave's weight over the one before, above nothing and at most one
float At(Vector2 place);                                 // The noise in meters at a place, from minus the amplitude to the amplitude

record Terrace                                           // Cuts the field into terraces, steps of a height each with a flat tread and a riser that rises smoothly to the next
Terrace(float height, float tread);                      // Terraces of a height with a tread of a share of each step
float Height { get; init; }                              // Each step's height in meters
float Tread { get; init; }                               // The share of each step that is flat, from nothing up to one
float Of(float height);                                  // The height a terraced ground takes in place of a height

record Smoothing                                         // Smooths the field by passes of a blur over each point and the eight about it, the binomial weights of one, two and one each way
Smoothing(int passes);                                   // Smoothing of a count of passes
int Passes { get; init; }                                // How many passes, from one to 256

record ThermalErosion                                    // Thermal erosion, ground steeper than its talus slumping to the lower points beside it until it stands no steeper, as loose rock and soil settle
ThermalErosion(float talus, int passes, float share = 0.5f);  // Thermal erosion by a talus over a count of passes
float Talus { get; init; }                               // The steepest rise a meter the ground keeps
int Passes { get; init; }                                // How many passes, from one to 10,000
float Share { get; init; }                               // The share of half the largest excess that moves each pass

record HydraulicErosion                                  // Hydraulic erosion by droplets, each run downhill from a random place carrying the ground it wears away and laying it down where it slows, so slopes are cut into channels and their feet filled
HydraulicErosion(uint seed, int droplets, int lifetime = 30, float inertia = 0.05f, float capacity = 4f, float leastCapacity = 0.01f, float deposition = 0.3f, float erosion = 0.3f, float evaporation = 0.01f, float gravity = 4f, int radius = 3, float scale = 40f);  // Hydraulic erosion of a count of droplets from a seed
uint Seed { get; init; }                                 // The seed the droplets' places are drawn from
int Droplets { get; init; }                              // How many droplets run
int Lifetime { get; init; }                              // How many steps a droplet runs at most
float Inertia { get; init; }                             // The share of a droplet's way it keeps each step
float Capacity { get; init; }                            // How much sediment a droplet carries for its fall, speed and water
float LeastCapacity { get; init; }                       // The least a droplet carries however little it falls
float Deposition { get; init; }                          // The share of its sediment over capacity a droplet lays down a step
float Erosion { get; init; }                             // The share of its room under capacity a droplet wears away a step
float Evaporation { get; init; }                         // The share of its water a droplet loses a step
float Gravity { get; init; }                             // How fast a droplet gains speed for its fall
int Radius { get; init; }                                // How many spacings from a droplet it wears the ground away
float Scale { get; init; }                               // The height in meters a droplet's capacity and speed reckon its fall against

class LandscapeBuilder                                   // Builds a recipe and builds it again after each change, keeping the field each step left so a change applies again the step it reaches and the steps after it alone
LandscapeBuilder(Recipe recipe, CancellationToken cancel = default);  // A builder that builds a recipe from flat ground
LandscapeBuilder(Recipe recipe, ILandscapePictures? pictures, CancellationToken cancel = default);  // A builder that reads the pictures its recipes' stamps name through the engine's answer
Recipe Recipe { get; }                                   // The recipe the field was last built from
Heightfield Field { get; }                               // The field the recipe builds, a copy of the builder's own, which a rebuild replaces and never changes
LandscapeChange Rebuild(Recipe recipe, CancellationToken cancel = default);  // Builds a changed recipe from the first step that changed

record struct LandscapeChange                            // What a rebuild did, which steps it applied and which points it changed
LandscapeChange(int firstStep, int stepsApplied, LandscapeArea? changed);  // A rebuild's account, its first step applied, how many it applied and the points it changed
int FirstStep { get; init; }                             // The first step applied, the first that differed from the recipe before
int StepsApplied { get; init; }                          // How many steps were applied, the first and those after it
LandscapeArea? Changed { get; init; }                    // The rectangle about the points whose heights changed, or nothing where none did

record struct LandscapeArea                              // A rectangle of a field's points, from its first column and row to its last, both kept
LandscapeArea(int firstColumn, int firstRow, int lastColumn, int lastRow);  // A rectangle from its first column and row to its last
int FirstColumn { get; init; }                           // The first column, from zero along X
int FirstRow { get; init; }                              // The first row, from zero along Z
int LastColumn { get; init; }                            // The last column, at or past the first
int LastRow { get; init; }                               // The last row, at or past the first
IReadOnlyList<(int, int)> Tiles(Heightfield field, int tileCells = 64);  // The tiles of a field cut by Tiles whose corners a change of the points in the rectangle reaches, a row of tiles at a time

class SplatMap                                           // How much of each of a few materials covers each point of a field, as weights that sum to one and as layers of RGBA bytes, four materials a layer, which an engine uploads as textures and blends its materials by
int MostMaterials = 8;                                   // The most materials a map holds, two layers
int Columns { get; }                                     // How many points a row has along X, the layers' width in texels
int Rows { get; }                                        // How many rows of points there are along Z, the layers' height in texels
IReadOnlyList<string> Materials { get; }                 // The materials' names in the order of the rules, four to a layer
IReadOnlyList<byte[]> Layers { get; }                    // The layers, four bytes a texel in red, green, blue and alpha, four materials each
SplatMap Of(Heightfield field, IReadOnlyList<SplatRule> rules);  // Makes a field's splat map by its materials' rules
float Weight(int material, int column, int row);         // A material's weight at a point, from nothing to one, the materials' summing to one

record SplatRule                                         // Where a material of a splat map lies, a range of height and a range of steepness, each with a width beyond it over which the material gives way
SplatRule(string material, float lowest = -Infinityf, float highest = Infinityf, float heightBlend = 0f, float leastSteep = 0f, float mostSteep = Infinityf, float steepBlend = 0f);  // A material's rule
string Material { get; }                                 // The material's name, which an engine draws the material by
float Lowest { get; }                                    // The lowest height in meters it covers whole
float Highest { get; }                                   // The highest height in meters it covers whole
float HeightBlend { get; }                               // How many meters beyond either height it gives way over
float LeastSteep { get; }                                // The least steepness it covers whole, a rise over a run
float MostSteep { get; }                                 // The most steepness it covers whole, a rise over a run
float SteepBlend { get; }                                // How far in steepness beyond either end it gives way over
float Weight(float height, float steepness);             // The material's weight before the weights are brought to one, from nothing to one

record PictureStamp                                      // A picture's heights pressed into the field over a rectangle, a mountain or a riverbed an artist drew, read through the engine's ILandscapePictures by its name
PictureStamp(string picture, Vector2 place, Vector2 size, float height, StampBlend blend = StampBlend.Add, float edge = 0f);  // A stamp of a picture over a rectangle
string Picture { get; init; }                            // The picture's name, which the engine answers for
Vector2 Place { get; init; }                             // The rectangle's middle across the ground, X in X and Z in Y
Vector2 Size { get; init; }                              // The rectangle's width along X in X and its depth along Z in Y, in meters
float Height { get; init; }                              // The height in meters a share of one stands at
StampBlend Blend { get; init; }                          // How the picture meets the heights before it
float Edge { get; init; }                                // How many meters in from each side the stamp fades over

record HeightPicture                                     // A picture's heights as shares of a stamp's height, a row at a time, which an engine reads from a file for ILandscapePictures
HeightPicture(int width, int height, float[] shares);    // A picture of a width and a height in texels and its shares
int Width { get; }                                       // How many texels a row has
int Height { get; }                                      // How many rows it has
IReadOnlyList<float> Shares { get; }                     // The shares, a row at a time from the stamp's least Z
float At(float u, float v);                              // The share at a place across the picture, from nothing at its first texel's middle to one at its last's along each way, bilinear between the texels

class Placement                                          // Which of the places a planting gives stand on a field, kept by a rule's ranges of height and steepness and a density map, and lifted onto the ground
IReadOnlyList<Planted> Keep(Heightfield field, IEnumerable<(Vector3, uint)> places, PlacementRule rule, DensityMap? density = null);  // Keeps the places that stand on a field by a rule and a density map
float Draw(uint seed);                                   // The number a seed draws against a place's chance, from nothing up to one

record PlacementRule                                     // Where a kind of plant may stand, a range of height and a range of steepness, each with a width beyond it over which the plant thins out, as a SplatRule gives a material's
PlacementRule(float lowest = -Infinityf, float highest = Infinityf, float heightBlend = 0f, float leastSteep = 0f, float mostSteep = Infinityf, float steepBlend = 0f);  // A plant's rule
float Lowest { get; }                                    // The lowest height in meters it grows at whole
float Highest { get; }                                   // The highest height in meters it grows at whole
float HeightBlend { get; }                               // How many meters beyond either height it thins out over
float LeastSteep { get; }                                // The least steepness it grows on whole
float MostSteep { get; }                                 // The most steepness it grows on whole
float SteepBlend { get; }                                // How far in steepness beyond either end it thins out over
float Chance(float height, float steepness);             // The chance from the ranges alone, before the density map's share
bool Equals(PlacementRule? other);                       // Whether two rules hold the same ranges and blends
int GetHashCode();                                       // A hash of the ranges and blends, equal for equal rules

class DensityMap                                         // How densely a kind of plant grows over a field, a share from nothing to one at each of the field's points, painted by an engine's editor or taken from a splat map's material
DensityMap(int columns, int rows, float[] shares);       // A map of shares an engine gives, a point each
int Columns { get; }                                     // How many points a row has along X
int Rows { get; }                                        // How many rows of points there are along Z
IReadOnlyList<float> Shares { get; }                     // The shares, a row at a time, each row along X and the rows along Z
DensityMap Of(SplatMap splat, int material);             // A map of one material's weights in a splat map, the plant growing where the material lies
float At(Heightfield field, Vector2 place);              // The density at a place over a field of the map's size, bilinear between its points

record struct Planted                                    // A place kept on the ground, where it stands, the ground's way up there and its seed
Planted(Vector3 place, Vector3 normal, uint seed);       // A kept place, where it stands, the ground's way up there and its seed
Vector3 Place { get; init; }                             // Where it stands, on the ground, in meters about the field's middle
Vector3 Normal { get; init; }                            // The way up out of the ground there, at unit length, which a plant may lean by
uint Seed { get; init; }                                 // The seed it came with

class LandscapeGeometry                                  // A field as the triangles an engine draws, cut into square tiles so an engine culls them apart and builds again only those a change reaches
int TileCells = 64;                                      // The number of cells along a tile's side an engine takes where it has no other
IReadOnlyList<LandscapeMesh> Tiles(Heightfield field, int tileCells = 64, int step = 1, float skirt = 0f);  // Cuts a field into tiles, the last of a row and a column cut to what is left
LandscapeMesh Tile(Heightfield field, int column, int row, int tileCells = 64, int step = 1, float skirt = 0f);  // One tile of a field, the cells from its column and row times the tile's side
float SkirtDepth(Heightfield field, int tileCells = 64, int coarsest = 1);  // How far a skirt hangs so tiles drawn at any steps up to the coarsest meet with no crack
float StepError(Heightfield field, int column, int row, int step, int tileCells = 64);  // The most in meters a tile drawn at a step stands off the field's points it covers, which a binding weighs against the distance or the error on screen it accepts to pick the step

record LandscapeMesh                                     // One tile of a field as triangles, in meters with Y up about the field's middle, which an engine builds a mesh of
LandscapeMesh(int column, int row, Vector3[] positions, Vector3[] normals, Vector2[] uvs, uint[] indices);  // A tile of its column and row among the field's tiles and its corners' arrays
(Vector3, Vector3) Bounds { get; }                       // The least and the greatest corner of the box about every corner of the tile, its skirt's among them, which an engine culls the tile by
int Column { get; init; }                                // The tile's column among the field's tiles, from zero along X
int Row { get; init; }                                   // The tile's row among the field's tiles, from zero along Z
Vector3[] Positions { get; init; }                       // A corner a point of the field the tile covers, a row at a time along X and the rows along Z
Vector3[] Normals { get; init; }                         // The way up out of the ground at each corner, the field's own at its point
Vector2[] Uvs { get; init; }                             // Where each corner falls over the whole field, from nothing at its first point to one at its last, X in X and Z in Y
uint[] Indices { get; init; }                            // Three corners a triangle, two triangles a cell, counter-clockwise seen from above
```
