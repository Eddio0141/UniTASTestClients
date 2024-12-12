using System.IO;
using UnityEditor;

namespace Editor
{
    public static class Build
    {
        public static void BuildWin64()
        {
            var scenes = Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.AllDirectories);
            Directory.CreateDirectory("build");
            BuildPipeline.BuildPlayer(scenes, "build/build.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        }
    }
}