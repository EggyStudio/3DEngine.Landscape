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
public abstract record LandscapeStep
{
    /// <summary>For the module's steps alone, which are the only ones a recipe builds.</summary>
    private protected LandscapeStep() { }

    /// <summary>Changes the field's heights as the step says.</summary>
    internal abstract void Apply(Heightfield field);
}
