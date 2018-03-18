﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LandParserGenerator.Lexing;
using LandParserGenerator.Parsing.Tree;

namespace LandParserGenerator.Parsing
{
	public class MappingOptionsProcessingVisitor : BaseVisitor
	{
		public const double DEFAULT_BASE_PRIORITY = 1;

		private HashSet<string> Land { get; set; }
		private double BasePriority { get; set; }

		public MappingOptionsProcessingVisitor(Grammar g)
		{
			Land = g.Options.GetSymbols(MappingOption.LAND);

			var temp = g.Options.GetParams(MappingOption.BASEPRIORITY, OptionsManager.GLOBAL_PARAMETERS_SYMBOL);
			BasePriority = temp.Count > 0 ? (double)temp[0] : DEFAULT_BASE_PRIORITY;
		}

		public override void Visit(Node node)
		{
			if (Land.Contains(node.Symbol))
				node.Options.Set(MappingOption.LAND);

			if (!node.Options.Priority.HasValue)
				node.Options.Priority = BasePriority;

			base.Visit(node);
		}
	}
}