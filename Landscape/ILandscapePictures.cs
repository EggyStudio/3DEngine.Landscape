namespace Landscape;

/// <summary>
/// What an engine says of the pictures a recipe's <see cref="PictureStamp"/> names, the heights
/// each holds, read from the engine's own files (N 2.1).
/// </summary>
/// <remarks>
/// The module does not know where an engine keeps its pictures, how it names them or how it decodes
/// them, so the engine that binds it answers for a picture by its name, with its gray read as a
/// share of the stamp's height. A builder takes a picture once a build, by its name, so an engine
/// that changes a picture's pixels under the same name builds the recipe afresh.
/// </remarks>
public interface ILandscapePictures
{
    /// <summary>
    /// The heights a picture holds, or nothing where the engine has no picture of the name.
    /// </summary>
    /// <param name="name">The picture's name, as the stamp gives it.</param>
    HeightPicture? Heights(string name);
}
