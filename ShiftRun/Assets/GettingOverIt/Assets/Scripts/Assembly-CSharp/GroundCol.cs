using UnityEngine;

public class GroundCol : MonoBehaviour
{
	public enum SoundMaterial
	{
		rock = 0,
		wood = 1,
		metal = 2,
		plastic = 3,
		furniture = 4,
		snow = 5,
		cardboard = 6,
		none = 7,
		snake = 8,
		solidmetal = 9,
	}

	public Color groundCol;
	public SoundMaterial material;
}
