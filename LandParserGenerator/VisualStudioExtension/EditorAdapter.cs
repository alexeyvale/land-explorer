﻿using System;
using System.Collections.Generic;
using System.Windows.Media;

using Land.Core.Markup;
using Land.Core;
using Land.Control;

namespace Land.VisualStudioExtension
{
	public class EditorAdapter : IEditorAdapter
	{
		public string GetActiveDocumentName()
		{
			throw new NotImplementedException();
		}

		public int? GetActiveDocumentOffset()
		{
			throw new NotImplementedException();
		}

		public string GetActiveDocumentText()
		{
			throw new NotImplementedException();
		}

		public string GetDocumentText(string documentName)
		{
			throw new NotImplementedException();
		}

		public bool HasActiveDocument()
		{
			throw new NotImplementedException();
		}

		public void ProcessMessages(List<Message> messages, bool skipTrace, bool resetPrevious)
		{
			throw new NotImplementedException();
		}

		public void ProcessMessage(Message message)
		{
			throw new NotImplementedException();
		}

		public void ResetSegments()
		{
			throw new NotImplementedException();
		}

		public void SetActiveDocumentAndOffset(string documentName, PointLocation location)
		{
			throw new NotImplementedException();
		}

		public Color SetSegments(List<DocumentSegment> segments)
		{
			throw new NotImplementedException();
		}

		public void SaveSettings(LandExplorerSettings settings)
		{
			throw new NotImplementedException();
		}

		public LandExplorerSettings LoadSettings()
		{
			throw new NotImplementedException();
		}

		public void RegisterOnDocumentSaved(Action<string> callback)
		{
			throw new NotImplementedException();
		}

		public void RegisterOnDocumentChanged(Action<string> callback)
		{
			throw new NotImplementedException();
		}

		public void RegisterOnDocumentsSetChanged(Action<HashSet<string>> callback)
		{
			throw new NotImplementedException();
		}
	}
}
