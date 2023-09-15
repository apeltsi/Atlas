using Vortice.Dxc;
namespace Atlas.Tools.AssetCompiler;

public static class AssetBuilder
{
    private static string[] PNGToKTX(string dirPath, string path)
    {
        string pngPath = Path.Join(dirPath, path);
        string ktxPath = path.Substring(0, path.Length - Path.GetExtension(path).Length) + ".ktx";
            
        System.Diagnostics.Process process = new System.Diagnostics.Process();
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        startInfo.FileName = "toktx.exe";
        startInfo.Arguments = "\"" + Path.Join(dirPath, ktxPath) + "\" \"" + pngPath + "\"";
        process.StartInfo = startInfo;
        process.OutputDataReceived += (sender, args) => Compiler.Errors.Add(args.Data);
        process.ErrorDataReceived += (sender, args) => Compiler.Errors.Add(args.Data);
        process.Start();
        process.WaitForExit();
        File.Delete(pngPath);
        return new[] { ktxPath };
    }
    private static string[] HLSLToSPV(string dirPath, string path)
    {
        string vertPath = path.Substring(0, path.Length - Path.GetExtension(path).Length) +".vert";
        string fragPath = path.Substring(0, path.Length - Path.GetExtension(path).Length) + ".frag";
        File.WriteAllBytes(Path.Join(dirPath, vertPath), CompileToSPV(DxcShaderStage.Vertex, Path.Join(dirPath, path), "vert"));
        File.WriteAllBytes(Path.Join(dirPath,fragPath), CompileToSPV(DxcShaderStage.Pixel, Path.Join(dirPath,path), "pixel"));
        File.Delete(Path.Join(dirPath,path));
        return new[] { vertPath, fragPath };
    }
    
    private static string[] ComputeHLSLToSPV(string dirPath, string path)
    {
        string compPath = path.Substring(0, path.Length - Path.GetExtension(path).Length);
        File.WriteAllBytes(Path.Join(dirPath, compPath), CompileToSPV(DxcShaderStage.Compute, Path.Join(dirPath, path), "main"));
        File.Delete(Path.Join(dirPath, path));
        return new[] { compPath };
    }

    private static byte[] CompileToSPV(DxcShaderStage shaderStage, string path, string entry)
    {
        IDxcResult result = Vortice.Dxc.DxcCompiler.Compile(shaderStage, File.ReadAllText(path), entry, new DxcCompilerOptions
        {
            EnableStrictness = true,
            AllResourcesBound = true,
            GenerateSpirv = true,
            VkUseDXLayout = true,
        }, Path.GetFileName(path));
        string error = result.GetErrors();
        if (error != "")
        {
            Compiler.Errors.Add("Shader compilation on stage '" + shaderStage +  "' had some error(s): \n" + error);
        }
        return result.GetObjectBytecodeArray();
    }

    public static void BuildFiles(string dirPath, string[] files, string pack)
    {
        foreach (var file in files)
        {
            string fp = Path.Join(dirPath, file);
            string[] assetFiles = new[] { file };
            switch (Path.GetExtension(fp))
            {
                case ".hlsl":
                    if(fp.EndsWith(".compute.hlsl"))
                        assetFiles = ComputeHLSLToSPV(dirPath, file);
                    else 
                        assetFiles = HLSLToSPV(dirPath, file);
                    break;
                case ".png":
                    assetFiles = PNGToKTX(dirPath, file);
                    break;
            }

            foreach (var asset in assetFiles)    
            {
                Compiler.AssetMap.TryAdd(asset.Replace("\\", "/"), pack);
            }
        }
    }
}