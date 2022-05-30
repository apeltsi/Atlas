namespace SolidCode.Caerus.Rendering
{
    static class ShaderManager
    {
        public static Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();
        /// <summary>
        /// Gets a shader from memory, compiles the shader if it hasn't been compiled yet
        /// </summary>
        public static Shader GetShader(string path)
        {
            if (Window._graphicsDevice == null)
            {
                throw new NullReferenceException("No graphics device available yet!");
            }
            if (shaders.ContainsKey(path))
            {
                return shaders[path];
            }
            Debug.Log("Generating shader");
            Shader shader = new Shader(Window._graphicsDevice.ResourceFactory, path + ".vert", path + ".frag");
            shaders.Add(path, shader);
            return shader;
        }

    }
}