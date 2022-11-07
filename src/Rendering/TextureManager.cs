namespace SolidCode.Atlas.Rendering
{
    static class TextureManager
    {
        public static Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
        /// <summary>
        /// Gets a texture from memory, loads the texure from disk if it hasn't been loaded yet
        /// </summary>
        public static Texture GetTexture(string path)
        {
            if (Window._graphicsDevice == null)
            {
                throw new NullReferenceException("No graphics device available yet!");
            }
            if (textures.ContainsKey(path))
            {
                return textures[path];
            }
            Debug.Log(LogCategory.Rendering, "Loading texture '" + path + "'");
            Texture texture = new Texture(path + ".ktx", path, Window._graphicsDevice, Window._graphicsDevice.ResourceFactory);
            textures.Add(path, texture);
            return texture;
        }
    }
}