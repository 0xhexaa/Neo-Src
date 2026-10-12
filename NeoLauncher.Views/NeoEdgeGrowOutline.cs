using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace NeoLauncher.Views;

internal sealed class NeoEdgeGrowOutline : Canvas
{
	private const double OpenMs = 380.0;

	private const double CloseMs = 260.0;

	private const double PressMs = 130.0;

	private const double PressProgress = 0.84;

	private const double FadePressOpacity = 0.62;

	private readonly Path[] _paths = new Path[4];

	private readonly double[] _lengths = new double[4];

	private readonly double _thickness;

	private readonly double _radius;

	private readonly double _inset;

	private readonly bool _fade;

	private bool _hovered;

	private bool _pressed;

	private bool _lastPressed;

	private double _progress;

	private double _from;

	private double _to;

	private double _durationMs = 380.0;

	private double _elapsedMs;

	private long _lastTicks;

	private bool _ticking;

	private bool _easeOut = true;

	public NeoEdgeGrowOutline(Color color, double radius, double thickness = 1.5, NeoEdgeOutlineStyle style = NeoEdgeOutlineStyle.Grow)
	{
		_fade = style == NeoEdgeOutlineStyle.Fade;
		_thickness = thickness;
		_inset = thickness / 2.0;
		_radius = Math.Max(radius - _inset, 0.0);
		IsHitTestVisible = false;
		HorizontalAlignment = HorizontalAlignment.Stretch;
		VerticalAlignment = VerticalAlignment.Stretch;
		SolidColorBrush stroke = new SolidColorBrush(color);
		for (int i = 0; i < 4; i++)
		{
			Path path = new Path
			{
				Stroke = stroke,
				StrokeThickness = thickness,
				StrokeDashCap = PenLineCap.Flat,
				IsHitTestVisible = false,
				Visibility = Visibility.Collapsed
			};
			_paths[i] = path;
			Children.Add(path);
		}
		SizeChanged += (object _, SizeChangedEventArgs _) =>
		{
			BuildGeometry();
		};
		Unloaded += (object _, RoutedEventArgs _) =>
		{
			StopTicking();
		};
	}

	public void SetHovered(bool hovered)
	{
		if (_hovered != hovered)
		{
			_hovered = hovered;
			if (!hovered)
			{
				_pressed = false;
			}
			Retarget();
		}
	}

	public void SetPressed(bool pressed)
	{
		if (_pressed != pressed)
		{
			_pressed = pressed;
			Retarget();
		}
	}

	private void Retarget()
	{
		bool flag = !_pressed && _lastPressed;
		_from = _progress;
		ref double to = ref _to;
		double num;
		if (_hovered)
		{
			if (_pressed)
			{
				num = (_fade ? 0.62 : 0.84);
			}
			else
			{
				num = 1.0;
			}
		}
		else
		{
			num = 0.0;
		}
		to = num;
		_elapsedMs = 0.0;
		ref double durationMs = ref _durationMs;
		double num2;
		if (_hovered)
		{
			num2 = ((_pressed | flag) ? 130.0 : 380.0);
		}
		else
		{
			num2 = 260.0;
		}
		durationMs = num2;
		_easeOut = _hovered && !_pressed;
		_lastPressed = _pressed;
		if (Math.Abs(_to - _from) < 0.0005)
		{
			_progress = _to;
			ApplyProgress();
			StopTicking();
		}
		else
		{
			StartTicking();
		}
	}

	private void BuildGeometry()
	{
		double actualWidth = ActualWidth;
		double actualHeight = ActualHeight;
		if (!(actualWidth <= 0.0) && !(actualHeight <= 0.0))
		{
			double inset = _inset;
			double inset2 = _inset;
			double num = actualWidth - _inset;
			double num2 = actualHeight - _inset;
			if (!(num <= inset) && !(num2 <= inset2))
			{
				double num3 = Math.Max(0.0, Math.Min(_radius, Math.Min((num - inset) / 2.0, (num2 - inset2) / 2.0)));
				double num4 = num3 - num3 / Math.Sqrt(2.0);
				Point point = new Point(inset + num4, inset2 + num4);
				Point point2 = new Point(num - num4, inset2 + num4);
				Point point3 = new Point(num - num4, num2 - num4);
				Point point4 = new Point(inset + num4, num2 - num4);
				SetSegment(0, point, new Point(inset + num3, inset2), new Point(num - num3, inset2), point2, num3);
				SetSegment(1, point2, new Point(num, inset2 + num3), new Point(num, num2 - num3), point3, num3);
				SetSegment(2, point3, new Point(num - num3, num2), new Point(inset + num3, num2), point4, num3);
				SetSegment(3, point4, new Point(inset, num2 - num3), new Point(inset, inset2 + num3), point, num3);
				double num5 = Math.PI * num3 / 4.0;
				_lengths[0] = (_lengths[2] = num - inset - 2.0 * num3 + 2.0 * num5);
				_lengths[1] = (_lengths[3] = num2 - inset2 - 2.0 * num3 + 2.0 * num5);
				ApplyProgress();
			}
		}
	}

	private void SetSegment(int index, Point start, Point arcEnd, Point lineEnd, Point end, double r)
	{
		PathFigure pathFigure = new PathFigure
		{
			StartPoint = start,
			IsClosed = false,
			IsFilled = false
		};
		if (r > 0.01)
		{
			pathFigure.Segments.Add(new ArcSegment
			{
				Point = arcEnd,
				Size = new Size(r, r),
				RotationAngle = 0.0,
				IsLargeArc = false,
				SweepDirection = SweepDirection.Clockwise
			});
		}
		pathFigure.Segments.Add(new LineSegment
		{
			Point = lineEnd
		});
		if (r > 0.01)
		{
			pathFigure.Segments.Add(new ArcSegment
			{
				Point = end,
				Size = new Size(r, r),
				RotationAngle = 0.0,
				IsLargeArc = false,
				SweepDirection = SweepDirection.Clockwise
			});
		}
		PathGeometry pathGeometry = new PathGeometry();
		pathGeometry.Figures.Add(pathFigure);
		_paths[index].Data = pathGeometry;
	}

	private void ApplyProgress()
	{
		if (_fade)
		{
			Opacity = _progress;
			for (int i = 0; i < 4; i++)
			{
				Path path = _paths[i];
				path.StrokeDashArray = new DoubleCollection();
				path.Visibility = ((_progress <= 0.001 || (object)path.Data == null) ? Visibility.Collapsed : Visibility.Visible);
			}
			return;
		}
		Opacity = 1.0;
		for (int j = 0; j < 4; j++)
		{
			Path path2 = _paths[j];
			double num = _lengths[j];
			if (num <= 0.0 || (object)path2.Data == null)
			{
				path2.Visibility = Visibility.Collapsed;
				continue;
			}
			double num2 = num * _progress;
			if (num2 <= 0.05)
			{
				path2.Visibility = Visibility.Collapsed;
				continue;
			}
			path2.Visibility = Visibility.Visible;
			if (num2 >= num - 0.05)
			{
				path2.StrokeDashArray = new DoubleCollection();
				continue;
			}
			double item = (num - num2) / 2.0 / _thickness;
			path2.StrokeDashArray = new DoubleCollection
			{
				0.0,
				item,
				num2 / _thickness,
				item
			};
		}
	}

	private void StartTicking()
	{
		if (!_ticking)
		{
			if (!IsLoaded)
			{
				_progress = _to;
				ApplyProgress();
			}
			else
			{
				_ticking = true;
				_lastTicks = Stopwatch.GetTimestamp();
				CompositionTarget.Rendering += OnRendering;
			}
		}
	}

	private void StopTicking()
	{
		if (_ticking)
		{
			_ticking = false;
			CompositionTarget.Rendering -= OnRendering;
		}
	}

	private void OnRendering(object? sender, object e)
	{
		long timestamp = Stopwatch.GetTimestamp();
		double num = (double)(timestamp - _lastTicks) * 1000.0 / (double)Stopwatch.Frequency;
		_lastTicks = timestamp;
		_elapsedMs += num;
		double num2 = ((_durationMs <= 0.0) ? 1.0 : Math.Clamp(_elapsedMs / _durationMs, 0.0, 1.0));
		double num3 = (_easeOut ? CubicBezier(0.22, 1.0, 0.36, 1.0, num2) : CubicBezier(0.4, 0.0, 0.2, 1.0, num2));
		_progress = _from + (_to - _from) * num3;
		ApplyProgress();
		if (num2 >= 1.0)
		{
			_progress = _to;
			ApplyProgress();
			StopTicking();
		}
	}

	private static double CubicBezier(double x1, double y1, double x2, double y2, double x)
	{
		if (x <= 0.0)
		{
			return 0.0;
		}
		if (x >= 1.0)
		{
			return 1.0;
		}
		double num = 0.0;
		double num2 = 1.0;
		double num3 = x;
		for (int i = 0; i < 24; i++)
		{
			num3 = (num + num2) / 2.0;
			if (BezierAxis(x1, x2, num3) < x)
			{
				num = num3;
			}
			else
			{
				num2 = num3;
			}
		}
		return BezierAxis(y1, y2, num3);
	}

	private static double BezierAxis(double a, double b, double t)
	{
		double num = 1.0 - t;
		return 3.0 * num * num * t * a + 3.0 * num * t * t * b + t * t * t;
	}
}
