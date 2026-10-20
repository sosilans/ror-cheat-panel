using UnityEngine;
using UnityEngine.Sprites;

namespace RoRCheatPanel.UI;

internal struct GuiIcon
{
	public Texture Tex;
	public Rect Uv;

	public static GuiIcon FromTexture(Texture tex)
	{
		if (!tex)
			return default;
		return new GuiIcon
		{
			Tex = tex,
			Uv = new Rect(0f, 0f, 1f, 1f)
		};
	}

	public static GuiIcon FromSprite(Sprite sprite)
	{
		if (!sprite || !sprite.texture)
			return default;
		return new GuiIcon
		{
			Tex = sprite.texture,
			Uv = SpriteUv(sprite)
		};
	}

	private static Rect SpriteUv(Sprite sprite)
	{
		try
		{
			var outer = DataUtility.GetOuterUV(sprite);
			var w = outer.z - outer.x;
			var h = outer.w - outer.y;
			if (w > 0.0001f && h > 0.0001f)
				return new Rect(outer.x, outer.y, w, h);
		}
		catch
		{
		}

		var tex = sprite.texture;
		var r = sprite.textureRect;
		if (r.width < 1f || r.height < 1f)
			r = sprite.rect;
		if (!tex || tex.width < 1 || tex.height < 1)
			return new Rect(0f, 0f, 1f, 1f);
		return new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);
	}
}
