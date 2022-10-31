using System.Diagnostics;

namespace WolfRenderer;

public partial class MainForm : Form
{
	public MainForm()
	{
		InitializeComponent();
	}

	private void redraw()
	{
		var stopwatch = new Stopwatch();

		var bitmap = new Bitmap(pictureBox.Width, pictureBox.Height);
		stopwatch.Start();

		Renderer.PlayerAngle = Math.PI * trackBar.Value / 180 + 0.00001;
		Renderer.Render(bitmap);

		stopwatch.Stop();
		labelMs.Text = $"{stopwatch.Elapsed.TotalMilliseconds:0.00}ms {trackBar.Value}";

		var oldBitMap = pictureBox.Image;
		pictureBox.Image = bitmap;
		oldBitMap?.Dispose();
	}

	private void buttonStart_Click(object sender, EventArgs e)
	{
		redraw();
	}

	private void MainForm_ResizeEnd(object sender, EventArgs e)
	{
	}

	private void MainForm_ResizeBegin(object sender, EventArgs e)
	{
	}

	private void MainForm_SizeChanged(object sender, EventArgs e)
	{
		redraw();
	}

	private void MainForm_KeyPress(object sender, KeyPressEventArgs e)
	{
	}

	private void trackBar_ValueChanged(object sender, EventArgs e)
	{
		redraw();
	}
}
