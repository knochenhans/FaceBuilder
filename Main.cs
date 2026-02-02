using Godot;
using Godot.Collections;

public partial class Main : Node2D
{
    TextureRect textureRect => GetNode<TextureRect>("TextureRect");
    Array<Texture2D> FaceParts = [];

    public override void _Ready()
    {
        var faceBuilder = new FaceBuilder("res://Resources/Images/Layers/", "res://Resources/face_definition.json");
        textureRect.Texture = faceBuilder.BuildRandomFace();
    }

    public override void _Process(double delta)
    {
    }
}
