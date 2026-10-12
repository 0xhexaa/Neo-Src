using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace NeoLauncher.Extensions;

public static class DependencyObjectExtension
{
	public static T? FindChildByName<T>(this DependencyObject parent, string name) where T : FrameworkElement
	{
		int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
		for (int i = 0; i < childrenCount; i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(parent, i);
			if (child is T val && val.Name == name)
			{
				return val;
			}
			T val2 = child.FindChildByName<T>(name);
			if (val2 != null)
			{
				return val2;
			}
		}
		return null;
	}

	public static IEnumerable<T> FindChildrenByType<T>(this DependencyObject parent) where T : DependencyObject
	{
		int childCount = VisualTreeHelper.GetChildrenCount(parent);
		for (int i = 0; i < childCount; i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(parent, i);
			if (child is T val)
			{
				yield return val;
			}
			foreach (T item in child.FindChildrenByType<T>())
			{
				yield return item;
			}
		}
	}
}
