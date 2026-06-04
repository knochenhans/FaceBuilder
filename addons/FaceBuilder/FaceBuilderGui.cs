using Godot;
using Godot.Collections;

using System.Linq;

public partial class FaceBuilderGui : Control
{
	[Export(PropertyHint.Dir)] public string ResourcesPath = "res://FaceBuilderResources";
	[Export(PropertyHint.File)] public string DefinitionFile = "res://FaceBuilderResources/face_definition.json";

	[Export] public Sprite2D FaceSprite;

	[Export] public Control FacePartRowContainer;
	[Export] public PackedScene FacePartRowScene;

	[Export] public Button SavePNGButton;
	[Export] public Button SaveButton;
	[Export] public Button LoadButton;
	[Export] public Button StoreButton;
	[Export] public Button ToggleAllButton;
	[Export] public Button RandomizeButton;

	FaceBuilder FaceBuilder;

	public override void _Ready()
	{
		FaceBuilder = new FaceBuilder(ResourcesPath);
		FaceBuilder.LoadFaceDefinitionFile(DefinitionFile);

		Dictionary<string, int> indices = new()
		{
			{ "Neck", 0 },
			{ "Ears", 0 },
			// { "Ears Extra", 0 },
			{ "Chin", 0 },
			{ "Face", 0 },
			{ "Nose", 0 },
			{ "Mouth", 0 },
			{ "Beard", 0 },
			// { "Nose Extra", 0 },
			{ "Eyes", 0 },
			// { "Face Extra", 0 },
			// { "Eyes Extra", 0 },
			{ "Hair", 0 },
			// { "Hat", 0 }
		};

		FaceBuilder.SetFaceDefinition(indices);
		FaceSprite.Texture = FaceBuilder.BuildFace();

		foreach (string partName in FaceBuilder.GetAvailablePartNames())
		{
			FacePartRow row = FacePartRowScene.Instantiate<FacePartRow>();
			row.Init(partName);
			row.PreviousPart += OnPreviousPart;
			row.NextPart += OnNextPart;
			row.TogglePartVisibility += OnTogglePartVisibility;
			FacePartRowContainer.AddChild(row);

			// Set initial visibility state
			row.CheckBox.ButtonPressed = FaceBuilder.IsPartVisible(partName);
		}

		SavePNGButton.Pressed += OnSavePNGButtonPressed;
		SaveButton.Pressed += OnSaveButtonPressed;
		RandomizeButton.Pressed += OnRandomizeButtonPressed;
		ToggleAllButton.Pressed += OnToggleAllButtonPressed;
		LoadButton.Pressed += OnLoadButtonPressed;
	}

	private void OnPreviousPart(string partName, string variationName)
	{
		FaceBuilder.PreviousPart(partName);
		UpdateFace();

		// Update variation label
		FaceTextureEntry currentEntry = FaceBuilder.GetCurrentPartTexture(partName);
	}

	private void OnNextPart(string partName, string variationName)
	{
		FaceBuilder.NextPart(partName);
		UpdateFace();
	}

	private void OnTogglePartVisibility(string partName, bool visible)
	{
		FaceBuilder.SetPartVisibility(partName, visible);
		UpdateFace();
	}

	private void OnLoadButtonPressed()
	{
		var fileDialog = new FileDialog
		{
			FileMode = FileDialog.FileModeEnum.OpenFile,
			Access = FileDialog.AccessEnum.Resources,
			Filters = ["*.json"]
		};

		AddChild(fileDialog);

		fileDialog.FileSelected += path =>
		{
			FaceBuilder.LoadFaceConfigFile(path);
			UpdateFace();
			GD.Print($"Face definition loaded successfully from {path}");
			fileDialog.QueueFree();
		};

		fileDialog.PopupCentered();
	}

	private void OnSaveButtonPressed()
	{
		var saveData = FaceBuilder.GetFaceDefinition();

		var fileDialog = new FileDialog
		{
			FileMode = FileDialog.FileModeEnum.SaveFile,
			Access = FileDialog.AccessEnum.Resources,
			CurrentFile = "saved_face.json",
			Filters = ["*.json"]
		};

		AddChild(fileDialog);

		fileDialog.FileSelected += path =>
		{
			using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
			var jsonString = Json.Stringify(saveData, indent: "\t");
			file.StoreString(jsonString);
			GD.Print($"Face definition saved successfully to {path}");
			fileDialog.QueueFree();
		};

		fileDialog.PopupCentered();
	}

	private void OnSavePNGButtonPressed()
	{
		string savePath = "user://saved_face.png";
		Image faceImage = FaceBuilder.BuildFace().GetImage();
		Error saveError = faceImage.SavePng(savePath);
		if (saveError == Error.Ok)
		{
			GD.Print($"Face saved successfully to {savePath}");
		}
		else
		{
			GD.PrintErr($"Failed to save face: {saveError}");
		}
	}

	private void OnToggleAllButtonPressed()
	{
		bool allVisible = FaceBuilder.AreAllPartsVisible();
		FaceBuilder.SetAllPartsVisibility(!allVisible);
		UpdateFace();

		foreach (FacePartRow row in FacePartRowContainer.GetChildren().Cast<FacePartRow>())
			row.CheckBox.ButtonPressed = !allVisible;
	}

	private void OnRandomizeButtonPressed()
	{
		FaceBuilder.Randomize();
		UpdateFace();
	}

	public void UpdateFace()
	{
		FaceSprite.Texture = FaceBuilder.BuildFace();
	}
}
