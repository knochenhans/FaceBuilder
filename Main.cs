using Godot;
using Godot.Collections;

public partial class Main : Node2D
{
    TextureRect textureRectRandom => GetNode<TextureRect>("%TextureRect1");
    TextureRect textureRectCustom => GetNode<TextureRect>("%TextureRect2");
    Control ButtonsL => GetNode<Control>("%VBoxContainerL");
    Control ButtonsR => GetNode<Control>("%VBoxContainerR");
    Button SaveButton => GetNode<Button>("%SaveButton");
    Button RandomButton => GetNode<Button>("%RandomButton");

    Array<Texture2D> FaceParts = [];

    public override void _Ready()
    {
        var faceBuilder = new FaceBuilder("res://Resources/Images/FaceParts/", "res://Resources/face_definition.json");

        GD.Print(faceBuilder.GetFacePartCounts());

        textureRectRandom.Texture = faceBuilder.BuildRandomFace();
        var customIndices = new Dictionary<string, int>
        {
            { "head", 0 },
            { "mouth", 0 },
            { "beard", 0 },
            { "eyes", 0 },
            { "nose", 0 },
            { "hair", 0 }
        };
        textureRectCustom.Texture = faceBuilder.BuildFaceByIndices(customIndices);

        SaveButton.Pressed += OnSaveButtonPressed;
        RandomButton.Pressed += () => textureRectRandom.Texture = faceBuilder.BuildRandomFace();
    }

    public void SaveTextureToFile(Texture2D texture, string filePath)
    {
        var image = texture.GetImage();
        image.SavePng(filePath);
    }

    private void OnSaveButtonPressed()
    {
        const int scaleFactor = 6;
        var randomFilename = "custom_face_" + GD.Randi().ToString() + ".png";
        var originalTexture = textureRectRandom.Texture;
        var originalImage = originalTexture.GetImage();
        Image scaledImage = (Image)originalImage.Duplicate();
        scaledImage.Resize(originalImage.GetWidth() * scaleFactor, originalImage.GetHeight() * scaleFactor, Image.Interpolation.Nearest);
        var scaledTexture = ImageTexture.CreateFromImage(scaledImage);
        SaveTextureToFile(scaledTexture, "user://" + randomFilename);
        GD.Print("Scaled custom face saved to " + randomFilename);
    }
}
