using Godot;
using System;

public partial class TripleView : Window
{
	public Control Textures;

	// all the textures:
	public TextureRect TextureRect1;
	public TextureRect TextureRect2;
	public TextureRect TextureRect3;
	public TextureRect TextureRect4;
	public TextureRect TextureRect5;
	public TextureRect TextureRect6;
	public TextureRect TextureRect7;
	public TextureRect TextureRect8;
	public TextureRect TextureRect9;

	public TextureRect[] TextureRects;

	public TextureRect Background;

	public float AspectRatio;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		CloseRequested += () => QueueFree();
	}

	public void GetReady()
	{
		AspectRatio = (float)Size.X / (float)Size.Y;

		Textures = GetNode<Control>("Textures");

		// // load all the texture rects
		// for (int i = 0; i < TextureRects.Length; i++)
		// {
		// 	TextureRects[i] = GetNode<TextureRect>($"Textures/TextureRect{i + 1}");
		// }

		string path = "Textures/TextureRect";

		TextureRect1 = GetNode<TextureRect>(path + 1);
		TextureRect2 = GetNode<TextureRect>(path + 2);
		TextureRect3 = GetNode<TextureRect>(path + 3);
		TextureRect4 = GetNode<TextureRect>(path + 4);
		TextureRect5 = GetNode<TextureRect>(path + 5);
		TextureRect6 = GetNode<TextureRect>(path + 6);
		TextureRect7 = GetNode<TextureRect>(path + 7);
		TextureRect8 = GetNode<TextureRect>(path + 8);
		TextureRect9 = GetNode<TextureRect>(path + 9);


		TextureRects = new TextureRect[] { TextureRect1, TextureRect2, TextureRect3, TextureRect4, TextureRect5, TextureRect6, TextureRect7, TextureRect8, TextureRect9 };

		Textures.Size = Size;

		int width = Mathf.FloorToInt(Textures.Size.X / 3);
		int height = Mathf.FloorToInt(Textures.Size.Y / 3);


		for (int i = 0; i < TextureRects.Length; i++)
		{
			TextureRects[i].Size = new Vector2(width, height);
			TextureRects[i].Texture = new Texture2Drd();
			TextureRects[i].ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
			TextureRects[i].StretchMode = TextureRect.StretchModeEnum.KeepAspect;
		}


		TextureRect1.Position = new Vector2(0, 0);
		TextureRect2.Position = new Vector2(width, 0);
		TextureRect3.Position = new Vector2(width * 2, 0);
		TextureRect4.Position = new Vector2(0, height);
		TextureRect5.Position = new Vector2(width, height);
		TextureRect6.Position = new Vector2(width * 2, height);
		TextureRect7.Position = new Vector2(0, height * 2);
		TextureRect8.Position = new Vector2(width, height * 2);
		TextureRect9.Position = new Vector2(width * 2, height * 2);


		Vector2 viewportSize = Size;
		Background = GetNode<TextureRect>("Background");
		Image bgImage = Image.CreateEmpty((int)viewportSize.X, (int)viewportSize.Y, false, Image.Format.Rgba8);
		bgImage.Fill(new(0, 0, 0, 1));
		ImageTexture bgTexture = ImageTexture.CreateFromImage(bgImage);
		Background.Texture = bgTexture;
		Background.Size = viewportSize;

		SizeChanged += () => ScaleAllTextures();
	}

	public void ScaleAllTextures()
	{
		// GD.Print(AspectRatio);
		if (AspectRatio > Size.X / Size.Y)
		{
			Textures.Size = new Vector2(Size.X, Size.X / AspectRatio);
		}
		else
		{
			Textures.Size = new Vector2(Size.Y * AspectRatio, Size.Y);
		}


		int width = Mathf.FloorToInt(Textures.Size.X / 3);
		int height = Mathf.FloorToInt(Textures.Size.Y / 3);


		for (int i = 0; i < TextureRects.Length; i++)
		{
			TextureRects[i].Size = new Vector2(width, height);
		}


		TextureRect1.Position = new Vector2(0, 0);
		TextureRect2.Position = new Vector2(width, 0);
		TextureRect3.Position = new Vector2(width * 2, 0);
		TextureRect4.Position = new Vector2(0, height);
		TextureRect5.Position = new Vector2(width, height);
		TextureRect6.Position = new Vector2(width * 2, height);
		TextureRect7.Position = new Vector2(0, height * 2);
		TextureRect8.Position = new Vector2(width, height * 2);
		TextureRect9.Position = new Vector2(width * 2, height * 2);

		Background.Size = Size;

	}

	public void SetTexture(Rid texture)
	{
		for (int i = 0; i < TextureRects.Length; i++)
		{
			(TextureRects[i].Texture as Texture2Drd).TextureRdRid = texture;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
