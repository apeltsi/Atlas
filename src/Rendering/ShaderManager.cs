namespace SolidCode.Atlas.Rendering
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
            Debug.Log(LogCategory.Rendering, "Generating shader \"" + path + "\"");
            Shader shader = new Shader(Window._graphicsDevice.ResourceFactory, path + ".vert", path + ".frag");
            shaders.Add(path, shader);
            return shader;
        }

        public static void ClearAllShaders()
        {
            shaders.Clear();
        }

        public static void RecompileAllShaders()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Debug.Log(LogCategory.Rendering, "Recompiling shaders...");
            foreach (KeyValuePair<string, Shader> shader in shaders)
            {
                shaders[shader.Key] = new Shader(Window._graphicsDevice.ResourceFactory, shader.Key + ".vert", shader.Key + ".frag");
            }
            watch.Stop();
            Debug.Log(LogCategory.Rendering, "All shaders have been recompiled [" + watch.ElapsedMilliseconds + "ms]");
        }

    }
}