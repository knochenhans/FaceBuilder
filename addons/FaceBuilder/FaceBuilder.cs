using System;
using System.Linq;
using Godot;
using Godot.Collections;
using Microsoft.VisualBasic;

public partial class FacePartConfig(string name, int maxIndex) : GodotObject
{
    public string Name = name;
    public string VariationName = name;
    public int Index = 0;
    public int MaxIndex = maxIndex;
    public bool Visible = false;

    public void Next()
    {
        Index = (Index + 1) % (MaxIndex + 1);
    }

    public void Previous()
    {
        Index--;

        if (Index < 0)
            Index = MaxIndex;
    }
}

public partial class FaceTextureEntry(Texture2D texture, string variationName) : GodotObject
{
    public Texture2D Texture = texture;
    public string VariationName = variationName;
}

public class FaceBuilder
{
    Array<Variant> PartsOrder = null;

    readonly Dictionary<string, FacePartConfig> Parts = [];
    readonly Dictionary<string, Array<FaceTextureEntry>> TexturesByPart = [];

    readonly int PartNameStartIndex = 0;

    public FaceBuilder(string resourcesPath, int partNameStartIndex = 1)
    {
        PartNameStartIndex = partNameStartIndex;
        TexturesByPart = GetTexturesByFacePart(GetTexturesFromDirectory(resourcesPath));
        CreatePartConfigs();
    }

    private void CreatePartConfigs()
    {
        Parts.Clear();

        foreach (var (partName, textures) in TexturesByPart)
            Parts[partName] = new FacePartConfig(partName, textures.Count - 1);
    }

    private FacePartConfig FindPart(string name)
    {
        if (Parts.TryGetValue(name, out var part))
            return part;

        return null;
    }

    public void Randomize()
    {
        foreach (var part in Parts.Values)
            part.Index = (int)(GD.Randi() % (uint)(part.MaxIndex + 1));
    }

    public Dictionary<string, int> GetFaceDefinition()
    {
        Dictionary<string, int> result = [];

        foreach (var part in Parts)
        {
            if (part.Value.Visible)
                result[part.Key] = part.Value.Index;
        }

        return result;
    }

    public void SetFaceDefinition(Dictionary<string, int> definition)
    {
        ResetParts();

        foreach (var (name, index) in definition)
        {
            var part = FindPart(name);
            if (part != null)
            {
                part.Visible = true;
                part.Index = index;
            }
        }
    }

    private void ResetParts()
    {
        foreach (var part in Parts.Values)
        {
            part.Visible = false;
            part.Index = 0;
        }
    }

    public Texture2D BuildFace()
    {
        Array<Texture2D> textures = [];

        foreach (string partName in PartsOrder.Select(v => (string)v))
        {
            var config = FindPart(partName);
            if (config == null)
                continue;

            if (!config.Visible)
                continue;

            if (!TexturesByPart.TryGetValue(partName, out var partTextures))
                continue;

            textures.Add(partTextures[config.Index].Texture);
        }

        return CombineTextures(textures);
    }

    public void NextPart(string partName)
    {
        var part = FindPart(partName);
        part?.Next();
    }

    public void PreviousPart(string partName)
    {
        var part = FindPart(partName);
        part?.Previous();
    }

    public void SetPartVisibility(string partName, bool visible)
    {
        var part = FindPart(partName);
        if (part != null)
            part.Visible = visible;
    }

    public void LoadFaceDefinitionFile(string definitionFile)
    {
        PartsOrder = LoadFaceDefinition(definitionFile).TryGetValue("order", out Variant value) ? (Array<Variant>)value : null;
    }

    public void LoadFaceConfigFile(string configFile)
    {
        if (FileAccess.FileExists(configFile))
        {
            var file = FileAccess.Open(configFile, FileAccess.ModeFlags.Read);
            string jsonContent = file.GetAsText();
            var jsonParser = new Json();
            Error error = jsonParser.Parse(jsonContent);

            if (error == Error.Ok)
            {
                foreach (var kvp in jsonParser.Data.AsGodotDictionary())
                {
                    string key = kvp.Key.ToString();
                    Parts[key].Index = (int)kvp.Value;
                    Parts[key].Visible = true;
                }

                file.Close();
            }
            else
            {
                GD.Print("Error parsing character JSON: ", error);
                file.Close();
            }
        }
        else
        {
            GD.Print("File not found: ", configFile);
        }
    }

    public string[] GetAvailablePartNames()
    {
        string[] partNames = [];

        foreach (var partName in PartsOrder.Select(v => (string)v))
        {
            if (TexturesByPart.ContainsKey(partName))
                partNames = [.. partNames, partName];
        }

        return partNames;
    }

    private static string SplitAlpha(string input, out string numberPart)
    {
        int index = 0;
        while (index < input.Length && !char.IsDigit(input[index]))
        {
            index++;
        }

        numberPart = input[index..];
        return input[..index];
    }

    public Dictionary<string, int> GetFacePartCounts()
    {
        Dictionary<string, int> partCounts = [];

        foreach (var kvp in TexturesByPart)
            partCounts[kvp.Key] = kvp.Value.Count;

        return partCounts;
    }

    private Dictionary<string, Texture2D> GetTexturesFromDirectory(string path)
    {
        Dictionary<string, Texture2D> textures = [];

        var dir = DirAccess.Open(path);
        if (dir == null)
        {
            GD.PrintErr("Could not open directory: " + path);
            return textures;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            if (!dir.CurrentIsDir())
            {
                // Ignore *.import files
                if (fileName.EndsWith(".import"))
                {
                    fileName = dir.GetNext();
                    continue;
                }

                var filePath = System.IO.Path.Combine(path, fileName);
                var texture = GD.Load<Texture2D>(filePath);
                if (texture != null)
                    textures[fileName.Replace(".ase_layer_tex", "")] = texture;
            }
            fileName = dir.GetNext();
        }
        dir.ListDirEnd();

        return textures;
    }

    private Dictionary<string, Array<FaceTextureEntry>> GetTexturesByFacePart(Dictionary<string, Texture2D> textures)
    {
        Dictionary<string, Array<FaceTextureEntry>> faceParts = [];

        foreach (var kvp in textures)
        {
            var fileName = kvp.Key;
            var texture = kvp.Value;

            var parts = fileName.Split('_');

            var partName = parts[PartNameStartIndex];
            var partVariationName = parts.Length > PartNameStartIndex + 1 ? parts[PartNameStartIndex + 1] : string.Empty;
            if (!faceParts.ContainsKey(partName))
                faceParts[partName] = [];
            faceParts[partName].Add(new FaceTextureEntry(texture, partVariationName));
        }

        return faceParts;
    }

    private Dictionary<string, Variant> LoadFaceDefinition(string definitionFile)
    {
        Dictionary<string, Variant> jsonData = [];

        if (FileAccess.FileExists(definitionFile))
        {
            var file = FileAccess.Open(definitionFile, FileAccess.ModeFlags.Read);
            string jsonContent = file.GetAsText();
            var jsonParser = new Json();
            Error error = jsonParser.Parse(jsonContent);

            if (error == Error.Ok)
            {
                foreach (var kvp in jsonParser.Data.AsGodotDictionary())
                {
                    string key = kvp.Key.ToString();
                    jsonData[key] = kvp.Value;
                }

                file.Close();
            }
            else
            {
                GD.Print("Error parsing character JSON: ", error);
                file.Close();
            }
        }
        else
        {
            GD.Print("File not found: ", definitionFile);
        }
        return jsonData;
    }

    private static Texture2D CombineTextures(Array<Texture2D> textures)
    {
        if (textures.Count == 0)
            return null;

        int width = textures[0].GetWidth();
        int height = textures[0].GetHeight();

        Image combinedImage = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        combinedImage.Fill(new Color(0, 0, 0, 0));

        foreach (var texture in textures)
        {
            Image img = texture.GetImage();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color srcColor = img.GetPixel(x, y);
                    Color destColor = combinedImage.GetPixel(x, y);

                    // Alpha blending
                    float srcAlpha = srcColor.A;
                    float destAlpha = destColor.A * (1 - srcAlpha);
                    float outAlpha = srcAlpha + destAlpha;

                    if (outAlpha > 0)
                    {
                        Color outColor = ((srcColor * srcAlpha) + (destColor * destAlpha)) / outAlpha;
                        outColor.A = outAlpha;
                        combinedImage.SetPixel(x, y, outColor);
                    }
                }
            }
        }

        return ImageTexture.CreateFromImage(combinedImage);
    }

    public bool AreAllPartsVisible()
    {
        foreach (var part in Parts.Values)
        {
            if (!part.Visible)
                return false;
        }
        return true;
    }

    public void SetAllPartsVisibility(bool v)
    {
        foreach (var part in Parts.Values)
            part.Visible = v;
    }

    public bool IsPartVisible(string partName)
    {
        if (Parts.TryGetValue(partName, out FacePartConfig value))
            return value.Visible;
        return false;
    }

    public string GetPartVariationName(string partName, int index)
    {
        if (TexturesByPart.TryGetValue(partName, out var arr) && index >= 0 && index < arr.Count)
            return arr[index].VariationName;
        return string.Empty;
    }

    internal FaceTextureEntry GetCurrentPartTexture(string partName)
    {
        // if (TexturesByPart.TryGetValue(partName, out var arr) && arr.Count > 0)
        // {
        //     int currentIndex = Parts[partName].CurrentIndex;
        //     return arr[currentIndex];
        // }
        return null;
    }
}
