﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandParserGenerator
{
	public class Alternative
	{
		public string NonterminalSymbolName { get; set; } = null;

		public List<Entry> Elements { get; private set; } = new List<Entry>();

		public int Count { get { return Elements.Count; } }

		public Alternative Add(string elem)
		{
			Elements.Add(new Entry(elem));

			return this;
		}

		public string this[int i]
		{
			get { return Elements[i]; }
		}

		public List<Entry>.Enumerator GetEnumerator()
		{
			return Elements.GetEnumerator();
		}

		/// <summary>
		/// Получение подпоследовательности элементов альтернативы
		/// </summary>
		/// <param name="pos">Позиция, с которой начинается подпоследовательность</param>
		/// <returns></returns>
		public Alternative Subsequence(int pos)
		{
			return new Alternative()
			{
				Elements = pos < Elements.Count ?
					this.Elements.GetRange(pos, this.Elements.Count - pos) :
					new List<Entry>()
			};
		}

		public override bool Equals(object obj)
		{
			if (obj is Alternative)
			{
				var b = (Alternative)obj;

				return NonterminalSymbolName == b.NonterminalSymbolName
					&& b.Elements.SequenceEqual(this.Elements);
			}
			else
				return false;
		}

		public override int GetHashCode()
		{
			return Elements.GetHashCode();
		}

		public override string ToString()
		{
			return Count > 0 ? String.Join(" ", Elements) : "eps";
		}

		public bool Contains(string symbol)
		{
			return Elements.Any(e => e.Value == symbol);
		}

        public bool ReplaceFirst(string smb1, string smb2)
        {
            var idx = Elements.FindIndex(e => e.Value == smb1);

            if (idx > -1)
            {
                Elements[idx] = new Entry(smb2);
                return true;
            }
            else
                return false;
        }
    }
}