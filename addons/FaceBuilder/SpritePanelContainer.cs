using Godot;
using System;

public partial class SpritePanelContainer : PanelContainer
{
	[Export] private Sprite2D Sprite;

	public override void _Ready()
	{
		Resized += CenterTheSprite;

		CenterTheSprite();
	}

	private void CenterTheSprite()
	{
		if (Sprite != null)
		{
			Sprite.Position = Size / 2;
		}
	}
}