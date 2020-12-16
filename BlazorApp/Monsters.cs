using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;


namespace BlazorApp
{
    public class Monsters
    {
        private List<string> animations = new List<string>();
        private List<string> models = new List<string>();

        public void AeriaMonstersPath(string inputPath, string outputPath)
        {
            if (Directory.Exists(inputPath))
            {
                foreach (string filename in Directory.GetFiles(inputPath))
                {
                    animations.Add(filename);
                }

                string sortedPath = outputPath + "\\Sorted Monsters";
                Directory.CreateDirectory(sortedPath);
                foreach (string animation in animations)
                {
                    Directory.CreateDirectory(sortedPath + "\\" + Path.GetFileName(animation));
                    // Copy the files into here
                }

            }
        }
    }

}
