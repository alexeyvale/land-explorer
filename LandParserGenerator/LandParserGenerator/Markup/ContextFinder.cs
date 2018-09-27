﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Land.Core.Parsing.Tree;

namespace Land.Core.Markup
{
	public class NodeSimilarityPair
	{
		private const double HeaderContextWeight = 1;
		private const double AncestorsContextWeight = 0.5;
		private const double InnerContextWeight = 0.7;
		private const double SiblingsContextWeight = 0.4;

		public Node Node { get; set; }
		public PointContext Context { get; set; }

		public double? HeaderSimilarity { get; set; }
		public double? AncestorSimilarity { get; set; }
		public double? InnerSimilarity { get; set; }
		public double? SiblingsSimilarity { get; set; }

		public double Similarity => 
			((HeaderSimilarity ?? 1) * HeaderContextWeight 
			+ (AncestorSimilarity ?? 1) * AncestorsContextWeight)
			//+ (InnerSimilarity ?? 1) * InnerContextWeight
			//+ (SiblingsSimilarity ?? 1) * SiblingsContextWeight)
			/ (HeaderContextWeight + AncestorsContextWeight /*+ InnerContextWeight + SiblingsContextWeight*/);
	}

	public static class ContextFinder
	{
		/// Веса операций для Левенштейна
		private const double InsertionCost = 1;
		private const double DeletionCost = 1;
		private const double SubstitutionCost = 0.8;

		/// <summary>
		/// Поиск узла дерева, соответствующего заданному контексту
		/// </summary>
		/// <param name="context"></param>
		/// <param name="tree"></param>
		/// <returns>Список кандидатов, отсортированных по степени похожести</returns>
		public static List<NodeSimilarityPair> Find(PointContext context, string fileName, Node tree, bool fullComparison = false)
		{
			var candidates = new List<NodeSimilarityPair>();
			var groupVisitor = new GroupNodesByTypeVisitor(context.NodeType);

			tree.Accept(groupVisitor);

			if (groupVisitor.Grouped.ContainsKey(context.NodeType))
			{
				candidates = groupVisitor.Grouped[context.NodeType].Select(node => new NodeSimilarityPair()
				{
					Node = node,
					Context = new PointContext()
					{
						FileName = fileName,
						NodeType = node.Type
					}
				}).ToList();

				foreach (var candidate in candidates)
				{
					candidate.Context.HeaderContext = PointContext.GetHeaderContext(candidate.Node);
					candidate.HeaderSimilarity = Levenshtein(context.HeaderContext, candidate.Context.HeaderContext);
				}

				foreach (var candidate in candidates)
				{
					candidate.Context.AncestorsContext = PointContext.GetAncestorsContext(candidate.Node);
					candidate.AncestorSimilarity = Levenshtein(context.AncestorsContext, candidate.Context.AncestorsContext);
				}

				foreach (var candidate in candidates)
				{
					PointContext.ComplementContexts(candidate.Context, candidate.Node);
				}
			}

			return candidates.OrderByDescending(p=>p.Similarity).ToList();
		}

		private static double Similarity(List<AncestorsContextElement> originContext, List<AncestorsContextElement> candidateContext)
		{
			var candidateAncestorsContext =
				candidateContext.GroupBy(c => c.Type).ToDictionary(g => g.Key, g => g);
			var ancestorsMapping = new Dictionary<AncestorsContextElement, AncestorsContextElement>();
			var rawSimilarity = 0.0;

			foreach (var ancestor in originContext
				.Where(oc => candidateAncestorsContext.ContainsKey(oc.Type)))
			{
				var similarities = new Dictionary<AncestorsContextElement, double>();

				foreach (var candidateAncestor in candidateAncestorsContext[ancestor.Type])
				{
					similarities[candidateAncestor] = Similarity(ancestor.HeaderContext, candidateAncestor.HeaderContext);
				}

				var bestCandidate = similarities.OrderByDescending(s => s.Value).FirstOrDefault();

				if (bestCandidate.Key != null)
				{
					ancestorsMapping[ancestor] = bestCandidate.Key;
					rawSimilarity += bestCandidate.Value;
				}
			}

			return rawSimilarity / originContext.Count;
		}

		private static double Similarity(List<HeaderContextElement> originContext, List<HeaderContextElement> candidateContext)
		{
			var candidateChildrenContext =
				candidateContext.GroupBy(c => c.Type).ToDictionary(g => g.Key, g => g);
			var childrenMapping = new Dictionary<HeaderContextElement, HeaderContextElement>();
			var rawSimilarity = 0.0;

			foreach (var child in originContext
				.Where(oc => candidateChildrenContext.ContainsKey(oc.Type)))
			{
				var similarities = new Dictionary<HeaderContextElement, double>();

				foreach (var candidateChild in candidateChildrenContext[child.Type])
					similarities[candidateChild] = Levenshtein(child.Value, candidateChild.Value);

				var bestCandidate = similarities.OrderByDescending(s => s.Value).FirstOrDefault();

				if (bestCandidate.Key != null)
				{
					childrenMapping[child] = bestCandidate.Key;
					rawSimilarity += bestCandidate.Value * child.Priority;
				}
			}

			return rawSimilarity / originContext.Sum(c => c.Priority);
		}

		private static double Similarity(List<InnerContextElement> originContext, List<InnerContextElement> candidateContext)
		{
			var candidateInnerContext =
				candidateContext.GroupBy(c => c.Type).ToDictionary(g => g.Key, g => g);
			var childrenMapping = new Dictionary<InnerContextElement, InnerContextElement>();
			var rawSimilarity = 0.0;

			foreach (var child in originContext
				.Where(oc => candidateInnerContext.ContainsKey(oc.Type)))
			{
				var similarities = new Dictionary<InnerContextElement, double>();

				foreach (var candidateChild in candidateInnerContext[child.Type])
					similarities[candidateChild] = Similarity(child.HeaderContext, candidateChild.HeaderContext);

				var bestCandidate = similarities.OrderByDescending(s => s.Value).FirstOrDefault();

				if (bestCandidate.Key != null)
				{
					childrenMapping[child] = bestCandidate.Key;
					rawSimilarity += bestCandidate.Value * child.Priority;
				}
			}

			return rawSimilarity / originContext.Sum(c => c.Priority);
		}

		private static double Similarity(List<SiblingsContextElement> originContext, List<SiblingsContextElement> candidateContext)
		{
			return 1;
		}

		///  Похожесть на основе расстояния Левенштейна
		private static double Levenshtein<T>(IEnumerable<T> a, IEnumerable<T> b)
		{
			if (a.Count() == 0 ^ b.Count() == 0)
				return 0;
			if (a.Count() == 0 && b.Count() == 0)
				return 1;

			/// Сразу отбрасываем общие префиксы и суффиксы
			var commonPrefixLength = 0;
			while (commonPrefixLength < a.Count() && commonPrefixLength < b.Count()
				&& a.ElementAt(commonPrefixLength).Equals(b.ElementAt(commonPrefixLength)))
				++commonPrefixLength;
			a = a.Skip(commonPrefixLength).ToList();
			b = b.Skip(commonPrefixLength).ToList();

			var commonSuffixLength = 0;
			while (commonSuffixLength < a.Count() && commonSuffixLength < b.Count()
				&& a.ElementAt(a.Count() - 1 - commonSuffixLength).Equals(b.ElementAt(b.Count() - 1 - commonSuffixLength)))
				++commonSuffixLength;
			a = a.Take(a.Count() - commonSuffixLength).ToList();
			b = b.Take(b.Count() - commonSuffixLength).ToList();

			if (a.Count() == 0 && b.Count() == 0)
				return 1;

			/// Согласно алгоритму Вагнера-Фишера, вычисляем матрицу расстояний
			var distances = new double[a.Count() + 1, b.Count() + 1];
			distances[0, 0] = 0;

			/// Заполняем первую строку и первый столбец
			for (int i = 1; i <= a.Count(); ++i)
				distances[i, 0] = distances[i - 1, 0] + DeletionCost;
			for (int j = 1; j <= b.Count(); ++j)
				distances[0, j] = distances[0, j - 1] + InsertionCost;

			for (int i = 1; i <= a.Count(); i++)
				for (int j = 1; j <= b.Count(); j++)
				{
					/// Если элементы - это тоже перечислимые наборы элементов, считаем для них расстояние
					double cost = 1 - DispatchLevenstein(a.ElementAt(i - 1), b.ElementAt(j - 1));
					distances[i, j] = Math.Min(Math.Min(
						distances[i - 1, j] + DeletionCost,
						distances[i, j - 1] + InsertionCost),
						distances[i - 1, j - 1] + cost);
				}

			return 1 - distances[a.Count(), b.Count()] /
				Math.Max(a.Count() + commonSuffixLength + commonPrefixLength, b.Count() + commonSuffixLength + commonPrefixLength);
		}

		private static double Denominator<T>(IEnumerable<T> a, IEnumerable<T> b)
		{
			if (a is IEnumerable<HeaderContextElement>)
				return Math.Max(
					((IEnumerable<HeaderContextElement>)a).Sum(elem => elem.Priority), 
					((IEnumerable<HeaderContextElement>)b).Sum(elem => elem.Priority)
				);
			else
				return Math.Max(a.Count(), b.Count());
		}

		private static double PriorityCoefficient<T>(T elem)
		{
			if (elem is HeaderContextElement)
				return (elem as HeaderContextElement).Priority * DeletionCost;
			else
				return DeletionCost;
		}

		private static double DispatchLevenstein<T>(T a, T b)
		{
			if (a is IEnumerable<string>)
				return Levenshtein((IEnumerable<string>)a, (IEnumerable<string>)b);
			if (a is IEnumerable<HeaderContextElement>)
				return Levenshtein((IEnumerable<HeaderContextElement>)a, (IEnumerable<HeaderContextElement>)b);
			if (a is IEnumerable<AncestorsContextElement>)
				return Levenshtein((IEnumerable<AncestorsContextElement>)a, (IEnumerable<AncestorsContextElement>)b);
			else if (a is string)
				return Levenshtein((IEnumerable<char>)a, (IEnumerable<char>)b);
			else if (a is HeaderContextElement)
			{
				var aContext = a as HeaderContextElement;
				var bContext = b as HeaderContextElement;

				if (aContext.Type == bContext.Type && aContext.Priority == bContext.Priority)
				{
					return Levenshtein(aContext.Value, bContext.Value);
				}
				else
					return 0;
			}
			else if (a is AncestorsContextElement)
			{
				var aContext = a as AncestorsContextElement;
				var bContext = b as AncestorsContextElement;

				if (aContext.Type == bContext.Type)
				{
					return Levenshtein(aContext.HeaderContext, bContext.HeaderContext);
				}
				else
					return 0;
			}
			else return a.Equals(b) ? 1 : 1 - SubstitutionCost;
		}
	}
}