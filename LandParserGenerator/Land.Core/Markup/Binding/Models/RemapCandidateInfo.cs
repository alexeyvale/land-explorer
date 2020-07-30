﻿using System;
using System.Collections.Generic;
using System.Linq;
using Land.Core.Parsing.Tree;

namespace Land.Markup.Binding
{
	public class RemapCandidateInfo
	{
		public Node Node { get; set; }
		public ParsedFile File { get; set; }
		public PointContext Context { get; set; }

		public SiblingsSearchResult SiblingsSearchResult { get; set; }

		public double HeaderSequenceSimilarity { get; set; }
		public double HeaderCoreSimilarity { get; set; }
		public double HeaderWordsSimilarity { get; set; }

		public double AncestorSimilarity { get; set; }
		public double InnerSimilarity { get; set; }

		public double SiblingsBeforeGlobalSimilarity { get; set; }
		public double SiblingsAfterGlobalSimilarity { get; set; }
		public double SiblingsBeforeEntitySimilarity { get; set; }
		public double SiblingsAfterEntitySimilarity { get; set; }

		public double? Similarity { get; set; }

		public bool IsAuto { get; set; }

		public string DebugInfo { get; set; }

		public override string ToString()
		{
			return $"{String.Format("{0:f4}", Similarity)} [H: {String.Format("{0:f2}", HeaderSequenceSimilarity)}; A: {String.Format("{0:f2}", AncestorSimilarity)}; I: {String.Format("{0:f2}", InnerSimilarity)}]";
		}
	}
}
