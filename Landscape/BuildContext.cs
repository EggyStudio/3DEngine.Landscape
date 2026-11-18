namespace Landscape;

// What a step is built with, the engine's pictures and the build's options.
internal sealed record BuildContext(ILandscapePictures? Pictures, BuildOptions Options)
{
    public static BuildContext Alone { get; } = new(null, BuildOptions.Default);
}
