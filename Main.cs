using Godot;
using Godot.Collections;

public partial class Main : Node2D
{
    TextureRect textureRectRandom => GetNode<TextureRect>("TextureRect1");
    TextureRect textureRectCustom => GetNode<TextureRect>("TextureRect2");

    Array<Texture2D> FaceParts = [];

    public override void _Ready()
    {
        var faceBuilder = new FaceBuilder("res://Resources/Images/Layers/", "res://Resources/face_definition.json");

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
    }
}
