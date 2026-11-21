using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FurinaPlayer.Core.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace FurinaPlayer.UI.Helpers;

/// <summary>
/// 运行时遍历 WinUI 3 可视化树，对嵌入 XBF 静态文本控件执行 Loc.T(...) 本地化，并缓存原始 zh-CN 文本以支持任意语言间实时切换。
/// </summary>
public static class UiLocalizer
{
	private static readonly ConditionalWeakTable<DependencyObject, Dictionary<string, string>> _originals = new();

	public static void LocalizeTree(DependencyObject? root)
	{
		if (root == null) return;
		var visited = new HashSet<DependencyObject>();
		LocalizeNode(root, visited);
	}

	private static void TranslateAndStore(DependencyObject target, string propKey, string? currentValue, Action<string> setter)
	{
		if (string.IsNullOrEmpty(currentValue)) return;
		var map = _originals.GetOrCreateValue(target);
		if (!map.TryGetValue(propKey, out string? original))
		{
			original = currentValue;
			map[propKey] = original;
		}
		string translated = Loc.T(original);
		if (!string.Equals(currentValue, translated, StringComparison.Ordinal))
		{
			setter(translated);
		}
	}

	private static void LocalizeNode(DependencyObject node, HashSet<DependencyObject> visited)
	{
		if (!visited.Add(node)) return;

		if (node is TextBlock tb)
		{
			TranslateAndStore(tb, "Text", tb.Text, v => tb.Text = v);
			if (tb.Inlines != null)
			{
				foreach (var inline in tb.Inlines)
				{
					if (inline is Run run)
					{
						TranslateAndStore(run, "Text", run.Text, v => run.Text = v);
					}
				}
			}
		}

		if (node is ContentControl cc)
		{
			if (cc.Content is string s)
			{
				TranslateAndStore(cc, "Content", s, v => cc.Content = v);
			}
			else if (cc.Content is DependencyObject dep)
			{
				LocalizeNode(dep, visited);
			}
		}

		if (node is TextBox box)
		{
			TranslateAndStore(box, "PlaceholderText", box.PlaceholderText, v => box.PlaceholderText = v);
			if (box.Header is string h)
			{
				TranslateAndStore(box, "Header", h, v => box.Header = v);
			}
		}

		if (node is AutoSuggestBox asb)
		{
			TranslateAndStore(asb, "PlaceholderText", asb.PlaceholderText, v => asb.PlaceholderText = v);
			if (asb.Header is string h)
			{
				TranslateAndStore(asb, "Header", h, v => asb.Header = v);
			}
		}

		if (node is ComboBox cb)
		{
			if (cb.Header is string h)
			{
				TranslateAndStore(cb, "Header", h, v => cb.Header = v);
			}
			TranslateAndStore(cb, "PlaceholderText", cb.PlaceholderText, v => cb.PlaceholderText = v);
			if (cb.Items != null)
			{
				foreach (var item in cb.Items)
				{
					if (item is ComboBoxItem cbi)
					{
						if (cbi.Content is string s)
						{
							TranslateAndStore(cbi, "Content", s, v => cbi.Content = v);
						}
						else if (cbi.Content is DependencyObject cbiDep)
						{
							LocalizeNode(cbiDep, visited);
						}
						LocalizeNode(cbi, visited);
					}
					else if (item is DependencyObject dep)
					{
						LocalizeNode(dep, visited);
					}
				}
			}
		}

		if (node is Slider slider && slider.Header is string sliderHeader)
		{
			TranslateAndStore(slider, "Header", sliderHeader, v => slider.Header = v);
		}

		if (node is ToggleSwitch ts)
		{
			if (ts.Header is string h)
			{
				TranslateAndStore(ts, "Header", h, v => ts.Header = v);
			}
			if (ts.OnContent is string on)
			{
				TranslateAndStore(ts, "OnContent", on, v => ts.OnContent = v);
			}
			if (ts.OffContent is string off)
			{
				TranslateAndStore(ts, "OffContent", off, v => ts.OffContent = v);
			}
		}

		if (node is NavigationViewItem nvi)
		{
			if (nvi.Content is string s)
			{
				TranslateAndStore(nvi, "Content", s, v => nvi.Content = v);
			}
			if (nvi.MenuItems != null)
			{
				foreach (var m in nvi.MenuItems)
				{
					if (m is DependencyObject dep)
					{
						LocalizeNode(dep, visited);
					}
				}
			}
		}

		if (ToolTipService.GetToolTip(node) is string tip)
		{
			TranslateAndStore(node, "ToolTip", tip, v => ToolTipService.SetToolTip(node, v));
		}

		int count = VisualTreeHelper.GetChildrenCount(node);
		for (int i = 0; i < count; i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(node, i);
			LocalizeNode(child, visited);
		}
	}
}
