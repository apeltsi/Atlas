namespace SolidCode.Atlas.Rendering
{
    public static class ShaderManager
    {
        public static Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();
        public static int ShaderGenerated;
        /// <summary>
        /// Gets a shader from memory, compiles the shader if it hasn't been compiled yet
        /// </summary>
        public static Shader GetShader(string path)
        {
            if (Renderer.GraphicsDevice == null)
            {
                throw new NullReferenceException("No graphics device available yet!");
            }
            if (shaders.ContainsKey(path))
            {
                return shaders[path];
            }
            Shader? shader = AssetManagement.AssetManager.GetAsset<Shader>(path);

            shaders.TryAdd(path, shader);
            return shader;
        }

        public static void PreloadShaders(string[] _shaders)
        {
            for (int i = 0; i < _shaders.Length; i++)
            {
                // It might be a bit crazy to give each shader its own thread but this will work for now...
                string shader = _shaders[i];
                Thread t = new Thread(() => GenerateShader(shader));
                t.Start();
            }
        }

        static void GenerateShader(string shader)
        {
            GetShader(shader);
            Interlocked.Increment(ref ShaderGenerated);
        }

        public static void ClearAllShaders()
        {
            shaders.Clear();
        }

        public static void RecompileAllShaders()
        {
            // TODO(amos) reimplement this

            throw new NotImplementedException("RecompileAllShaders has not been implemented yet!");
            /*
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Debug.Log(LogCategory.Rendering, "Recompiling shaders...");
            foreach (KeyValuePair<string, Shader> shader in shaders)
            {
                shaders[shader.Key] = new Shader();
                shaders[shader.Key].Load(shader.Key, "");
            }
            watch.Stop();
            Debug.Log(LogCategory.Rendering, "All shaders have been recompiled [" + watch.ElapsedMilliseconds + "ms]");*/
        }

    }
}