﻿using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;
using Land.VisualStudioExtension.Listeners;
using System.Windows;

namespace Land.VisualStudioExtension
{
	/// <summary>
	///  Наследуем пакет от AsyncPackage, так как загрузка 
	///  при большом количестве парсеров может быть медленной
	/// </summary>
	[ProvideBindingPath]
	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[ProvideToolWindow(typeof(LandExplorer))]
	[Guid(LandExplorerPackage.PackageGuidString)]
	[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
	public sealed class LandExplorerPackage : AsyncPackage
	{
		public const string PackageGuidString = "92dd57dc-aa42-446e-b5bc-9cd875a9e9ec";

		public LandExplorerPackage()
		{
			// Inside this method you can place any initialization code that does not require
			// any Visual Studio service because at this point the package object is created but
			// not sited yet inside Visual Studio environment. The place to do all the other
			// initialization is the Initialize method.
		}

		#region Package Members

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
		/// <param name="progress">A provider for progress updates.</param>
		/// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
		protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			// When initialized asynchronously, the current thread may be a background thread at this point.
			// Do any initialization that requires the UI thread after switching to the UI thread.
			await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
			await LandExplorerCommand.InitializeAsync(this);

			ServiceEventAggregator.InitializeInstance(
				await GetServiceAsync(typeof(SVsSolution)) as IVsSolution,
				await GetServiceAsync(typeof(SVsShell)) as IVsShell,
				() => GetService(typeof(SDTE)) as DTE2
			);
		}

		protected override void Dispose(bool disposing)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			base.Dispose(disposing);
			ServiceEventAggregator.DisposeInstance();
		}

		protected override int QueryClose(out bool pfCanClose)
		{
			var toolWindow = (LandExplorer)this.FindToolWindow(typeof(LandExplorer), 0, false);

			if(toolWindow != null
				&& toolWindow.Control.IsLoaded
				&& toolWindow.Control.HasUnsavedChanges)
			{
				switch (MessageBox.Show(
					"Сохранить изменения текущей разметки?",
					"Закрытие панели разметки",
					MessageBoxButton.YesNoCancel,
					MessageBoxImage.Question))
				{
					case MessageBoxResult.Yes:
						toolWindow.Control.Save();
						break;
					case MessageBoxResult.Cancel:
						pfCanClose = false;
						return VSConstants.S_OK;
				}
			}

			pfCanClose = true;
			return VSConstants.S_OK;
		}

		#endregion

		#region Methods

		#endregion
	}
}
