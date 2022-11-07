namespace SolidCode.Atlas.Rendering
{
    public class PostProcessingStack
    {

    }

    /// <summary>
    /// A group of post processing actions that make up an effect.
    /// Example for bloom: Separating bright pixels, horizontal blur, vertical blur, combine with main texture
    /// </summary>

    public class PostProcessingGroup
    {
        List<PostProcess> effects;
        public PostProcessingGroup(List<PostProcess> effects)
        {
            this.effects = effects;
        }


    }
}