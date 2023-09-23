using System.Numerics;
using SolidCode.Atlas.Components;
using SolidCode.Atlas.UI;
using SolidCode.Atlas;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.AssetManagement;

namespace emptyproject
{

    public static class Program
    {
        public static void Main()
        {
            // This should be the first thing called in your project
            Atlas.UseLogging();
            
            // Initialize primary features
            Atlas.StartCoreFeatures("emptyproject");
            
            // Here you could load your assets, initialize your systems, etc.
            AssetPack main = new AssetPack("main");
            main.Load();
            
            // Create an entity
            Entity logo = new Entity("Logo");
            // Add a component to the entity
            SpriteRenderer sr = logo.AddComponent<SpriteRenderer>();
            sr.Sprite = AssetManager.GetTexture("logo");
            
            // Actually start the engine
            Atlas.Start();
            // Everything after this point will only be called after the engine has been stopped
            
        }
    }
}