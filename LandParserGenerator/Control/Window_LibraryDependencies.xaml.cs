﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.ComponentModel;
using System.IO;

namespace Land.Control
{
	public class PossibleDependencyViewModel: INotifyPropertyChanged
	{
		public string Path { get; set; }
		public string Text { get; set; }
		public bool Exists { get; set; }

		private bool _isSelected;

		public bool IsSelected
		{
			get => _isSelected;

			set
			{
				_isSelected = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}

	public partial class Window_LibraryDependencies : Window
	{
		public HashSet<string> Selected { get; set; } = new HashSet<string>();
		public List<PossibleDependencyViewModel> PossibleDependencies { get; set; }

		public Window_LibraryDependencies(string libraryPath, HashSet<string> currentDependencies)
		{
			InitializeComponent();

			if (File.Exists(libraryPath))
			{
				PossibleDependencies = Directory.GetFiles(Path.GetDirectoryName(libraryPath), "*.dll")
					.Select(e => new PossibleDependencyViewModel()
					{
						Path = e,
						Text = Path.GetFileName(e),
						IsSelected = false,
						Exists = true
					}).OrderBy(e => e.Text).ToList();

				PossibleDependencies.RemoveAll(e => e.Path == libraryPath);
			}
			else
				PossibleDependencies = new List<PossibleDependencyViewModel>();

			foreach (var dependency in currentDependencies)
			{
				var sameInPossible = PossibleDependencies.FirstOrDefault(d => d.Path.ToLower() == dependency.ToLower());

				if (sameInPossible != null)
				{
					sameInPossible.IsSelected = true;
					Selected.Add(sameInPossible.Path);
				}
				else
					PossibleDependencies.Add(new PossibleDependencyViewModel
					{
						Exists = false,
						IsSelected = false,
						Path = dependency,
						Text = Path.GetFileName(dependency),
					});
			}

			PossibleDependenciesList.ItemsSource = PossibleDependencies;
		}

		private void Button_Save_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}

		private void Button_Cancel_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
		}

		private void Button_SelectAll_Click(object sender, RoutedEventArgs e)
		{
			foreach (var dep in PossibleDependencies.Where(d => d.Exists))
			{
				dep.IsSelected = true;
				Selected.Add(dep.Path);
			}
		}

		private void Button_Reset_Click(object sender, RoutedEventArgs e)
		{
			foreach (var dep in PossibleDependencies.Where(d => d.Exists))
			{
				dep.IsSelected = false;
				Selected.Remove(dep.Path);
			}
		}

		private void PossibleDependenciesList_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var item = VisualUpwardSearch<ListBoxItem>(e.OriginalSource as DependencyObject);

			if (item != null)
			{
				var context = (PossibleDependencyViewModel)item.DataContext;

				if (context.Exists)
				{
					context.IsSelected = !context.IsSelected;

					if (context.IsSelected)
						Selected.Add(context.Path);
					else
						Selected.Remove(context.Path);
				}
			}
		}

		private static T VisualUpwardSearch<T>(DependencyObject source) where T : class
		{
			while (source != null && !(source is T))
				source = VisualTreeHelper.GetParent(source);

			return source as T;
		}
	}
}
