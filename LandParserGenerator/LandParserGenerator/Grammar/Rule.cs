﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandParserGenerator
{
	public class Rule: IGrammarElement
	{
		public string Name { get; private set; }
		private List<Alternative> Alternatives { get; set; } = new List<Alternative>();

		public int Count { get { return Alternatives.Count; } }

		public void Add(Alternative alt)
		{
			Alternatives.Add(alt);
		} 

		public Alternative this[int i]
		{
			get { return Alternatives[i]; }
		}

		public List<Alternative>.Enumerator GetEnumerator()
		{
			return Alternatives.GetEnumerator();
		}

		public override bool Equals(object obj)
		{
			return obj is Rule && ((Rule)obj).Name == Name;
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}
	}
}
