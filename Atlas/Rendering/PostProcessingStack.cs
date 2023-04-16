namespace SolidCode.Atlas.Rendering
{
    public class PostProcessingStack
    {

    }

    /// <summary>
    /// A group of post processing steps that make up an effect.
    /// Example for bloom: Separating bright pixels, horizontal blur, vertical blur, combine with main texture
    /// </summary>

    public class PostProcessEffect
    {
        List<PostProcessStep> effects;
        public PostProcessEffect(List<PostProcessStep> effects)
        {
            this.effects = effects;
        }


    }
}