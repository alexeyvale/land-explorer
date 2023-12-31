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

		public double HeaderNonCoreSimilarity { get; set; }
		public double HeaderCoreSimilarity { get; set; }

		public double AncestorSimilarity { get; set; }
		public double InnerSimilarity { get; set; }

		public double SiblingsNearestSimilarity { get; set; }
		public double SiblingsAllSimilarity { get; set; }

		public double? Similarity { get; set; }
		public Dictionary<ContextType, double> Weights { get; set; }

		public bool IsAuto { get; set; }
		
		public bool Deleted { get; set; }
		public string DebugInfo { get; set; }

		public override string ToString()
		{
			return $"{String.Format("{0:f4}", Similarity)}\t[Core(H): {String.Format("{0:f2}", HeaderCoreSimilarity)}; " +
				$"NotCore(H): {String.Format("{0:f2}", HeaderNonCoreSimilarity)}; " +
				$"S: {String.Format("{0:f2}", AncestorSimilarity)}; " +
				$"I: {String.Format("{0:f2}", InnerSimilarity)}; " +
				$"Nearest(N): {String.Format("{0:f2}", SiblingsNearestSimilarity)}; " +
				$"All(N): {String.Format("{0:f2}", SiblingsAllSimilarity)}]";
		}
	}
}
