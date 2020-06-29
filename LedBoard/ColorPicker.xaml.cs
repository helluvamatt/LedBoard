using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LedBoard
{
	/// <summary>
	/// Interaction logic for ColorPicker.xaml
	/// </summary>
	public partial class ColorPicker : UserControl
	{
		public ColorPicker()
		{
			InitializeComponent();
		}

		#region Dependency properties

		#region SelectedColor

		public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(nameof(SelectedColor), typeof(Color), typeof(ColorPicker), new FrameworkPropertyMetadata(Colors.Transparent, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));

		public Color SelectedColor
		{
			get => (Color)GetValue(SelectedColorProperty);
			set => SetValue(SelectedColorProperty, value);
		}

		private static void OnSelectedColorChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var control = (ColorPicker)owner;
			control._SelectedColorSetting = true;
			control.RedComponent = control.SelectedColor.R;
			control.GreenComponent = control.SelectedColor.G;
			control.BlueComponent = control.SelectedColor.B;
			Utils.RgbToHsv(control.SelectedColor.R, control.SelectedColor.G, control.SelectedColor.B, out double hue, out double sat, out double val);
			control.HueComponent = hue;
			control.SatComponent = sat;
			control.ValComponent = val;
			control._SelectedColorSetting = false;
		}

		#endregion

		#region RedComponent

		public static readonly DependencyProperty RedComponentProperty = DependencyProperty.Register(nameof(RedComponent), typeof(byte), typeof(ColorPicker), new PropertyMetadata((byte)0, OnComponentPropertyChanged));

		public byte RedComponent
		{
			get => (byte)GetValue(RedComponentProperty);
			set => SetValue(RedComponentProperty, value);
		}

		#endregion

		#region GreenComponent

		public static readonly DependencyProperty GreenComponentProperty = DependencyProperty.Register(nameof(GreenComponent), typeof(byte), typeof(ColorPicker), new PropertyMetadata((byte)0, OnComponentPropertyChanged));

		public byte GreenComponent
		{
			get => (byte)GetValue(GreenComponentProperty);
			set => SetValue(GreenComponentProperty, value);
		}

		#endregion

		#region BlueComponent

		public static readonly DependencyProperty BlueComponentProperty = DependencyProperty.Register(nameof(BlueComponent), typeof(byte), typeof(ColorPicker), new PropertyMetadata((byte)0, OnComponentPropertyChanged));

		public byte BlueComponent
		{
			get => (byte)GetValue(BlueComponentProperty);
			set => SetValue(BlueComponentProperty, value);
		}

		#endregion

		#region HueComponent

		public static readonly DependencyProperty HueComponentProperty = DependencyProperty.Register(nameof(HueComponent), typeof(double), typeof(ColorPicker), new PropertyMetadata(0.0, OnHsvComponentPropertyChanged));

		public double HueComponent
		{
			get => (double)GetValue(HueComponentProperty);
			set => SetValue(HueComponentProperty, value);
		}

		#endregion

		#region SatComponent

		public static readonly DependencyProperty SatComponentProperty = DependencyProperty.Register(nameof(SatComponent), typeof(double), typeof(ColorPicker), new PropertyMetadata(0.0, OnHsvComponentPropertyChanged));

		public double SatComponent
		{
			get => (double)GetValue(SatComponentProperty);
			set => SetValue(SatComponentProperty, value);
		}

		#endregion

		#region ValComponent

		public static readonly DependencyProperty ValComponentProperty = DependencyProperty.Register(nameof(ValComponent), typeof(double), typeof(ColorPicker), new PropertyMetadata(0.0, OnHsvComponentPropertyChanged));

		public double ValComponent
		{
			get => (double)GetValue(ValComponentProperty);
			set => SetValue(ValComponentProperty, value);
		}

		#endregion

		#region Shared

		private static void OnComponentPropertyChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var control = (ColorPicker)owner;
			control.OnComponentChanged();
		}

		private void OnComponentChanged()
		{
			if (_SelectedColorSetting) return;
			SelectedColor = Color.FromRgb(RedComponent, GreenComponent, BlueComponent);
		}

		private static void OnHsvComponentPropertyChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
		{
			var control = (ColorPicker)owner;
			control.OnHsvComponentChanged();
		}

		private void OnHsvComponentChanged()
		{
			if (_SelectedColorSetting) return;
			Utils.HsvToRgb(HueComponent, SatComponent, ValComponent, out byte red, out byte green, out byte blue);
			SelectedColor = Color.FromRgb(red, green, blue);
		}

		private bool _SelectedColorSetting = false;

		#endregion

		#endregion

		private void OnSatValCanvasMouseMove(object sender, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				Canvas canvas = (Canvas)sender;
				Point pos = e.GetPosition(canvas);
				HandleSatValCanvas(canvas, pos);
			}
		}

		private void OnSatValCanvasMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				Canvas canvas = (Canvas)sender;
				Point pos = e.GetPosition(canvas);
				HandleSatValCanvas(canvas, pos);
			}
		}

		private void HandleSatValCanvas(Canvas canvas, Point pos)
		{
			double sat = pos.X / canvas.ActualWidth;
			double val = (canvas.ActualHeight - pos.Y) / canvas.ActualHeight;
			if (sat < 0) sat = 0;
			if (val < 0) val = 0;
			if (sat > 1.0) sat = 1.0;
			if (val > 1.0) val = 1.0;
			_SelectedColorSetting = true;
			SatComponent = sat;
			ValComponent = val;
			_SelectedColorSetting = false;
			OnHsvComponentChanged();
		}

		private void OnHueCanvasMouseMove(object sender, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				Canvas canvas = (Canvas)sender;
				Point pos = e.GetPosition(canvas);
				HandleHueCanvas(canvas, pos.Y);
			}
		}

		private void OnHueCanvasMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				Canvas canvas = (Canvas)sender;
				Point pos = e.GetPosition(canvas);
				HandleHueCanvas(canvas, pos.Y);
			}
		}

		private void HandleHueCanvas(Canvas canvas, double y)
		{
			double hue = y / canvas.ActualHeight;
			if (hue < 0) hue = 0;
			if (hue > 1.0) hue = 1.0;
			HueComponent = hue * 360;
		}
	}
}
