namespace Landscape;

/// <summary>
/// One step of a <see cref="Recipe"/>, a stamp or a procedural operation, which changes the heights
/// a field holds when the recipe is built.
/// </summary>
/// <remarks>
/// The steps are the module's own, so a recipe a level keeps means the same field in every engine
/// that takes the module, and a step's numbers are records' values, so two recipes of the same
/// steps are equal.
/// </remarks>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(Stamp), "stamp")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(PictureStamp), "pictureStamp")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(Noise), "noise")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(Terrace), "terrace")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(Smoothing), "smoothing")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(ThermalErosion), "thermalErosion")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(HydraulicErosion), "hydraulicErosion")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(Hole), "hole")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(Masked), "masked")]
public abstract record LandscapeStep
{
    /// <summary>For the module's steps alone, which are the only ones a recipe builds.</summary>
    private protected LandscapeStep() { }

    /// <summary>
    /// Changes the field's heights as the step says, reading the pictures a picture stamp names
    /// through the engine's answer.
    /// </summary>
    internal abstract void Apply(Heightfield field, BuildContext context);

    /// <summary>Changes the field's heights as a step that names no picture does.</summary>
    internal void Apply(Heightfield field) => Apply(field, BuildContext.Alone);
}
