﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Land.Markup;

namespace Land.Control
{
	public class MarkupTreeIconVisibilityConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var label = (Label)values[0];

			/// Признак актуальности координат есть только у точки привязки
			var hasMissingLocation = values[1] == DependencyProperty.UnsetValue 
				? (bool?)null : (bool)values[1];

			switch (label.Name)
			{
				case "MissingIcon":
					return hasMissingLocation.HasValue
						? hasMissingLocation.Value
							? Visibility.Visible : Visibility.Collapsed 
						: Visibility.Collapsed;

				case "PointIcon":
					return hasMissingLocation.HasValue
						? hasMissingLocation.Value
							? Visibility.Collapsed : Visibility.Visible
						: Visibility.Collapsed;

				case "ConcernIcon":
					return !hasMissingLocation.HasValue
						? Visibility.Visible : Visibility.Collapsed;
			}

			return Visibility.Collapsed;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			return new object[] { };
		}
	}
}
