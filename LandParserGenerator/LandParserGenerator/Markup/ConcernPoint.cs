﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Runtime.Serialization;

using Land.Core.Parsing.Tree;

namespace Land.Core.Markup
{
	[DataContract(IsReference = true)]
	public class ConcernPoint: MarkupElement, INotifyPropertyChanged
	{
		[DataMember]
		public PointContext Context { get; set; }

		private SegmentLocation _location;
		public SegmentLocation Location
		{
			get => _location;

			set
			{
				_location = value;

				if(PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("Location"));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public ConcernPoint(TargetFileInfo targetInfo, Concern parent = null)
		{
			Context = PointContext.Create(targetInfo);

			Location = targetInfo.TargetNode.Anchor;
			Parent = parent;
			Name = targetInfo.TargetNode.Type;

			if (targetInfo.TargetNode.Value.Count > 0)
				Name += ": " + String.Join(" ", targetInfo.TargetNode.Value);
			else
			{
				if (targetInfo.TargetNode.Children.Count > 0)
				{
					Name += ": " + String.Join(" ", targetInfo.TargetNode.Children.SelectMany(c => c.Value.Count > 0 ? c.Value
						: new List<string>() { '"' + (String.IsNullOrEmpty(c.Alias) ? c.Symbol : c.Alias) + '"' }));
				}
			}
		}

		public ConcernPoint(string name, TargetFileInfo targetInfo, Concern parent = null)
		{
			Name = name;
			Context = PointContext.Create(targetInfo);
			Parent = parent;
		}

		public void Relink(TargetFileInfo targetInfo)
		{
			Location = targetInfo.TargetNode.Anchor;
			Context = PointContext.Create(targetInfo);
		}

		public override void Accept(BaseMarkupVisitor visitor)
		{
			visitor.Visit(this);
		}
	}

	public class TargetFileInfo
	{
		public string FileName { get; set; }
		public string FileText { get; set; }
		public Node TargetNode { get; set; }
	}
}
