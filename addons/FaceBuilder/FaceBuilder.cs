using Godot;
using Godot.Collections;

public class FaceBuilder
{
    readonly Dictionary<string, Array<Texture2D>> FaceParts = [];
    readonly Array<Variant> PartsOrder = null;
    readonly Array<Texture2D> RandomFaceParts = [];
    readonly Dictionary<string, int> PartIndices = [];
    readonly int PartNameIndex = 1;

    public FaceBuilder(string resourcesPath, string definitionFile, int partNameNumber = 1)
    {
        PartNameIndex = partNameNumber;
        var textures = GetTextureFromDirectory(resourcesPath);
        FaceParts = GetTexturesByFacePart(textures);
        var definition = LoadFaceDefinition(definitionFile);
        PartsOrder = definition.ContainsKey("order") ? (Array<Variant>)definition["order"] : null;
    }

    private void ResetRandomFaceParts()
    {
        RandomFaceParts.Clear();
        PartIndices.Clear();
    }

    public Texture2D BuildRandomFace()
    {
        ResetRandomFaceParts();
        var faceParts = GetRandomFaceParts();
        return CombineTextures(faceParts);
    }

    public Dictionary<string, int> GetCurrentPartIndices()
    {
        return PartIndices;
    }

    public Texture2D BuildFaceByIndices(Dictionary<string, int> indices)
    {
        var selectedParts = GetFacePartsByIndices(indices);
        return CombineTextures(selectedParts);
    }

    private Array<Texture2D> GetFacePartsByIndices(Dictionary<string, int> indices)
    {
        Array<Texture2D> selectedParts = [];

        if (PartsOrder != null)
        {
            foreach (var partVariant in PartsOrder)
            {
                string partName = partVariant.ToString();
                if (indices.ContainsKey(partName) && FaceParts.ContainsKey(partName))
                {
                    var texturesList = FaceParts[partName];
                    int index = indices[partName];
                    if (index >= 0 && index < texturesList.Count)
                        selectedParts.Add(texturesList[index]);
                }
            }
        }
        return selectedParts;
    }

    private Array<Texture2D> GetRandomFaceParts()
    {
        if (PartsOrder != null)
        {
            foreach (var partVariant in PartsOrder)
            {
                string partName = partVariant.ToString();
                if (FaceParts.ContainsKey(partName))
                {
                    var texturesList = FaceParts[partName];
                    if (texturesList.Count > 0)
                    {
                        var randomIndex = GD.Randi() % texturesList.Count;
                        RandomFaceParts.Add(texturesList[(int)randomIndex]);
                        PartIndices[partName] = (int)randomIndex;
                    }
                }
            }
        }
        else
        {
            foreach (var kvp in FaceParts)
            {
                var texturesList = kvp.Value;
                if (texturesList.Count > 0)
                {
                    var randomIndex = GD.Randi() % texturesList.Count;
                    RandomFaceParts.Add(texturesList[(int)randomIndex]);
                    PartIndices[kvp.Key] = (int)randomIndex;
                }
            }
        }

        return RandomFaceParts;
    }

    private string SplitAlpha(string input, out string numberPart)
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

        foreach (var kvp in FaceParts)
        {
            partCounts[kvp.Key] = kvp.Value.Count;
        }

        return partCounts;
    }

    private Dictionary<string, Texture2D> GetTextureFromDirectory(string path)
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

    private Dictionary<string, Array<Texture2D>> GetTexturesByFacePart(Dictionary<string, Texture2D> textures)
    {
        Dictionary<string, Array<Texture2D>> faceParts = [];

        foreach (var kvp in textures)
        {
            var fileName = kvp.Key;
            var texture = kvp.Value;

            var parts = fileName.Split('_');

            var partRaw = parts[PartNameIndex];
            var partName = SplitAlpha(partRaw, out _);
            if (!faceParts.ContainsKey(partName))
                faceParts[partName] = [];
            faceParts[partName].Add(texture);
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

    private Texture2D CombineTextures(Array<Texture2D> textures)
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
}
