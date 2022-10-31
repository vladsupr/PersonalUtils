namespace WolfRenderer;

internal static class Renderer
{
	private static readonly byte[,] map = new byte[,]
	{
		{ 0, 0, 0, 0, 0, 0},
		{ 0, 0, 0, 0, 0, 0},
		{ 0, 1, 1, 1, 0, 0},
		{ 0, 0, 0, 0, 0, 0},
		{ 0, 0, 0, 0, 0, 0},
		{ 0, 0, 0, 0, 0, 0},
	};

	public const double GRID_SIZE = 100;
	public static readonly double playerX = 250;
	public static readonly double playerY = 250;
	public static double PlayerAngle = 30 * Math.PI / 180;

	private static double RenderRay(double x, double y, double angle)
	{
		double distance = (300 - playerY) / Math.Sin(angle);

		return distance;
	}

	public static void Render(Bitmap bitmap)
	{
		var random = new Random(Environment.TickCount);
		var g = Graphics.FromImage(bitmap);
		g.FillRectangle(Brushes.Black, Rectangle.FromLTRB(0, 0, bitmap.Width, bitmap.Height));

		var deltaAngle = Math.PI / 2 / bitmap.Width;
		var currentAngle = PlayerAngle - (bitmap.Width * deltaAngle / 2);
		for (int dx = 0; dx < bitmap.Width; dx++)
		{
			var distance = RenderRay(playerX, playerY, currentAngle);
			var height = (double)bitmap.Height * 200 / distance / Math.Tan(Math.PI / 4);
			if (height > 0)
			{
				var top = (bitmap.Height - height) / 2;
				if (top < 0) top = 0;

				var bottom = (bitmap.Height + height) / 2;
				if (bottom > bitmap.Height) bottom = bitmap.Height - 1;

				g.DrawLine(Pens.White, new Point(dx, (int)top), new Point(dx, (int)bottom));
			}
			currentAngle += deltaAngle;

		}
	}
}
