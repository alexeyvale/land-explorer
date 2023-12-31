﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Land.Core.Parsing.Tree;
using Land.Markup.CoreExtension;
using System.Threading.Tasks;
using System.Diagnostics;
using Land.Core;

namespace Land.Markup.Binding
{
	public enum ContextType
	{
		HeaderCore,
		HeaderNonCore,
		Ancestors,
		Inner,
		SiblingsAll,
		SiblingsNearest
	}

	public class ContextFinder
	{
		private const string LINE_END_SYMBOLS = "\u000A\u000D\u0085\u2028\u2029";
		private const string DEFAULT_LINE_END = "\r\n";

		public enum SearchType { Local, Global }

		public const double CANDIDATE_SIMILARITY_THRESHOLD = 0.6;

		public const double GAP_MAX = 2;
		public const double GAP_MIN = 1.5;
		public const double GAP_THRESHOLD = 0.8;

		public bool UseOldApproach { get; set; } = false;

		public Func<string, ParsedFile> GetParsed { get; set; }

		public PointContextManager ContextManager { get; private set; } = new PointContextManager();

		private LocationManager LocationManager { get; set; }

		public IPreHeuristic PreHeuristic { get; set; }
		public List<IWeightsHeuristic> TuningHeuristics { get; private set; } = new List<IWeightsHeuristic>();
		public List<ISimilarityHeuristic> ScoringHeuristics { get; private set; } = new List<ISimilarityHeuristic>();

		public void SetHeuristic(Type type)
		{
			void AddToList<T>(ref List<T> heuristics) where T : IPostHeuristic
			{
				if (typeof(T).IsAssignableFrom(type))
				{
					var element = heuristics.FirstOrDefault(e => e.GetType().Equals(type));

					if (element == null)
					{
						var constructor = type.GetConstructor(Type.EmptyTypes);

						if (constructor != null)
						{
							heuristics.Add((T)constructor.Invoke(null));
						}
					}
				}
			}

			var tuningHeuristics = TuningHeuristics;
			AddToList(ref tuningHeuristics);
			TuningHeuristics = tuningHeuristics;

			var scoringHeuristics = ScoringHeuristics;
			AddToList(ref scoringHeuristics);
			ScoringHeuristics = scoringHeuristics;
		}

		public void ResetHeuristic(Type type)
		{
			void RemoveFromList<T>(List<T> heuristics)
			{
				if (typeof(T).IsAssignableFrom(type))
				{
					var element = heuristics.FirstOrDefault(e => e.GetType().Equals(type));

					if (element != null)
					{
						heuristics.Remove(element);
					}
				}
			}

			RemoveFromList(TuningHeuristics);
			RemoveFromList(ScoringHeuristics);
		}

		public List<CandidateFeatures> GetFeatures(
			PointContext point,
			List<RemapCandidateInfo> candidates)
		{
			//if (candidates.Count > 0)
			//{
			//	double CountRatio<T1, T2>(List<T1> num, List<T2> denom) =>
			//		denom.Count > 1 ? num.Count / (double)denom.Count : 0;

			//	double CountRatioConditional<T>(List<T> list, Func<T, bool> checkFunction) =>
			//		list.Count > 1 ? list.Where(e => checkFunction(e)).Count() / (double)list.Count : 0;

			//	double MaxIfAny(List<RemapCandidateInfo> list, Func<RemapCandidateInfo, double> getValue) =>
			//		list.Count > 0 ? list.Max(getValue) : 0;

			//	int BoolToInt(bool val) => val ? 1 : 0;

			//	var existsA_Point = point.AncestorsContext?.Count > 0;
			//	var existsHSeq_Point = point.HeaderContext?.NonCore?.Count > 0;
			//	var existsHCore_Point = point.HeaderContext?.Core?.Count > 0;
			//	var existsI_Point = point.InnerContext?.Content.TextLength > 0;
			//	var existsSBefore_Point = point.SiblingsContext?.Before.GlobalHash.TextLength > 0;
			//	var existsSAfter_Point = point.SiblingsContext?.After.GlobalHash.TextLength > 0;

			//	return candidates.Select(c =>
			//	{
			//		var sameAncestorsCandidates = candidates.Where(cd => cd.AncestorSimilarity == c.AncestorSimilarity).ToList();
			//		sameAncestorsCandidates.Remove(c);

			//		var candidatesExceptCurrent = candidates.Except(new List<RemapCandidateInfo> { c }).ToList();

			//		var existsHCore_Candidate = c.Context.HeaderContext?.Core?.Count > 0;
			//		var existsI_Candidate = c.Context.InnerContext?.Content.TextLength > 0;

			//		var maxSimA = MaxIfAny(candidatesExceptCurrent, cd => cd.AncestorSimilarity);
			//		var maxSimHSeq = MaxIfAny(candidatesExceptCurrent, cd => cd.HeaderNonCoreSimilarity);
			//		var maxSimHCore = MaxIfAny(candidatesExceptCurrent, cd => cd.HeaderCoreSimilarity);
			//		var maxSimI = MaxIfAny(candidatesExceptCurrent, cd => cd.InnerSimilarity);
			//		var maxSimSBefore = MaxIfAny(candidatesExceptCurrent, cd => cd.SiblingsBeforeSimilarity);
			//		var maxSimSAfter = MaxIfAny(candidatesExceptCurrent, cd => cd.SiblingsAfterSimilarity);

			//		var candidatesWithMaxSimA = candidatesExceptCurrent.Where(cd => cd.AncestorSimilarity == maxSimA).ToList();
			//		var candidatesWithMaxSimHSeq = candidatesExceptCurrent.Where(cd => cd.HeaderNonCoreSimilarity == maxSimHSeq).ToList();
			//		var candidatesWithMaxSimHCore = candidatesExceptCurrent.Where(cd => cd.HeaderCoreSimilarity == maxSimHCore).ToList();
			//		var candidatesWithMaxSimI = candidatesExceptCurrent.Where(cd => cd.InnerSimilarity == maxSimI).ToList();
			//		var candidatesWithMaxSimSBefore = candidatesExceptCurrent.Where(cd => cd.SiblingsBeforeSimilarity == maxSimSBefore).ToList();
			//		var candidatesWithMaxSimSAfter = candidatesExceptCurrent.Where(cd => cd.SiblingsAfterSimilarity == maxSimSAfter).ToList();

			//		return new CandidateFeatures
			//		{
			//			ExistsA_Point = BoolToInt(existsA_Point),
			//			ExistsHSeq_Point = BoolToInt(existsHSeq_Point),
			//			ExistsHCore_Point = BoolToInt(existsHCore_Point),
			//			ExistsI_Point = BoolToInt(existsI_Point),
			//			ExistsSBefore_Point = BoolToInt(existsSBefore_Point),
			//			ExistsSAfter_Point = BoolToInt(existsSAfter_Point),

			//			ExistsHCore_Candidate = BoolToInt(existsHCore_Candidate),
			//			ExistsI_Candidate = BoolToInt(existsI_Candidate),
			//			IsSingleCandidate = BoolToInt(candidates.Count == 1),

			//			SimHSeq = c.HeaderNonCoreSimilarity,
			//			SimHCore = c.HeaderCoreSimilarity,
			//			SimI = c.InnerSimilarity,
			//			SimA = c.AncestorSimilarity,
			//			SimSBefore = c.SiblingsBeforeSimilarity,
			//			SimSAfter = c.SiblingsAfterSimilarity,

			//			//AncestorHasBeforeSibling = BoolToInt(c.SiblingsSearchResult?.BeforeSiblingOffset.HasValue ?? false),
			//			//AncestorHasAfterSibling = BoolToInt(c.SiblingsSearchResult?.AfterSiblingOffset.HasValue ?? false),
			//			//CorrectBefore = BoolToInt(c.SiblingsSearchResult?.BeforeSiblingOffset < c.Node.Location.Start.Offset),
			//			//CorrectAfter = BoolToInt(c.SiblingsSearchResult?.AfterSiblingOffset > c.Node.Location.Start.Offset),

			//			MaxSimA = maxSimA,
			//			MaxSimHSeq = maxSimHSeq,
			//			MaxSimHCore = maxSimHCore,
			//			MaxSimI = maxSimI,
			//			MaxSimSBeforeGlobal = maxSimSBefore,
			//			MaxSimSAfterGlobal = maxSimSAfter,

			//			MaxSimHSeq_SameA = MaxIfAny(sameAncestorsCandidates, cd => cd.HeaderNonCoreSimilarity),
			//			MaxSimHCore_SameA = MaxIfAny(sameAncestorsCandidates, cd => cd.HeaderCoreSimilarity),
			//			MaxSimI_SameA = MaxIfAny(sameAncestorsCandidates, cd => cd.InnerSimilarity),
			//			MaxSimSBefore_SameA = MaxIfAny(sameAncestorsCandidates, cd => cd.SiblingsBeforeSimilarity),
			//			MaxSimSAfter_SameA = MaxIfAny(sameAncestorsCandidates, cd => cd.SiblingsAfterSimilarity),

			//			RatioBetterSimA = CountRatioConditional(candidatesExceptCurrent, cd => cd.AncestorSimilarity > c.AncestorSimilarity),
			//			RatioBetterSimI = CountRatioConditional(candidatesExceptCurrent, cd => cd.InnerSimilarity > c.InnerSimilarity),
			//			RatioBetterSimHSeq = CountRatioConditional(candidatesExceptCurrent, cd => cd.HeaderNonCoreSimilarity > c.HeaderNonCoreSimilarity),
			//			RatioBetterSimSBefore = CountRatioConditional(candidatesExceptCurrent, cd => cd.SiblingsBeforeSimilarity > c.SiblingsBeforeSimilarity),
			//			RatioBetterSimSAfter = CountRatioConditional(candidatesExceptCurrent, cd => cd.SiblingsAfterSimilarity > c.SiblingsAfterSimilarity),

			//			RatioSameAncestor = CountRatio(sameAncestorsCandidates, candidates),

			//			RatioBetterSimI_SameA = CountRatioConditional(sameAncestorsCandidates, cd => cd.InnerSimilarity > c.InnerSimilarity),
			//			RatioBetterSimHSeq_SameA = CountRatioConditional(sameAncestorsCandidates, cd => cd.HeaderNonCoreSimilarity > c.HeaderNonCoreSimilarity),
			//			RatioBetterSimSBefore_SameA = CountRatioConditional(sameAncestorsCandidates, cd => cd.SiblingsBeforeSimilarity > c.SiblingsBeforeSimilarity),
			//			RatioBetterSimSAfter_SameA = CountRatioConditional(sameAncestorsCandidates, cd => cd.SiblingsAfterSimilarity > c.SiblingsAfterSimilarity),

			//			IsCandidateInnerContextLonger = BoolToInt(existsI_Candidate && (!existsI_Point
			//				|| c.Context.InnerContext.Content.TextLength > point.InnerContext.Content.TextLength)),
			//			InnerLengthRatio = existsI_Candidate && existsI_Point
			//				? Math.Min(c.Context.InnerContext.Content.TextLength, point.InnerContext.Content.TextLength)
			//					/ (double)Math.Max(c.Context.InnerContext.Content.TextLength, point.InnerContext.Content.TextLength)
			//				: 0,
			//			InnerLengthRatio1000_Point = Math.Min(point.InnerContext.Content.TextLength / (double)1000, 1),
			//			InnerLengthRatio1000_Candidate = Math.Min(c.Context.InnerContext.Content.TextLength / (double)1000, 1),

			//			IsAuto = BoolToInt(c.IsAuto),
			//		};
			//	}).ToList();
			//}

			return new List<CandidateFeatures>();
		}

		private Dictionary<ConcernPoint, List<RemapCandidateInfo>> DoMultiTypeSearch(
			Dictionary<string, List<ConcernPoint>> points,
			List<ParsedFile> searchArea,
			SearchType searchType)
		{
			var candidates = new Dictionary<string, List<RemapCandidateInfo>>();
			var ancestorToSiblingsCache = new Dictionary<Node, SiblingsContextConstructionCache>();

			/// Инициализируем коллекции кандидатов для каждого типа
			foreach (var type in points.Keys)
			{
				candidates[type] = new List<RemapCandidateInfo>();
			}

			/// В каждом файле находим кандидатов нужного типа
			foreach (var currentFile in searchArea)
			{
				if (!EnsureRootExists(currentFile))
				{
					continue;
				}

				var neighboursCache = GetNeighbours(currentFile, points.Keys.ToList());

				var visitor = new GroupNodesByTypeVisitor(points.Keys.ToList());
				currentFile.Root.Accept(visitor);

				foreach (var type in points.Keys)
				{
					/// Анализируем контекст соседей только при локальном поиске
					var checkSiblings = searchType == SearchType.Local;
					var siblingsArgs = checkSiblings ? new SiblingsConstructionArgs { ContextFinder = this } : null;

					candidates[type].AddRange(visitor.Grouped[type]
						.Select(n =>
						{
							var candidate = new RemapCandidateInfo
							{
								Node = n,
								File = currentFile,
								Context = ContextManager.GetContext(n, currentFile)
							};

							/// Если нужно проверить соседей, проверяем, не закешировали ли их
							if (checkSiblings)
							{
								SiblingsContextConstructionCache cache = null;

								/// Ищем предка, относительно которого нужно искать соседей
								var ancestor = PointContext.GetAncestor(n)
									?? (n != currentFile.Root ? currentFile.Root : null);

								/// Если таковой есть, пытаемся найти инфу о нём в кеше
								if (ancestor != null)
								{
									cache = ancestorToSiblingsCache.ContainsKey(ancestor)
										? ancestorToSiblingsCache[ancestor]
										: new SiblingsContextConstructionCache
										{
											Ancestor = ancestor,
											Neighbours = neighboursCache
										};
								}

								candidate.Context.SiblingsContext = PointContext.GetSiblingsContext(
									n, 
									currentFile,
									siblingsArgs,
									cache
								);

								candidate.Context.SiblingsContext_old = PointContext.GetSiblingsContext_old(n, currentFile, cache);

								if (ancestor != null && !ancestorToSiblingsCache.ContainsKey(ancestor))
								{
									ancestorToSiblingsCache[ancestor] = cache;
								}
							}

							return candidate;
						})
						.ToList()
					);
				}
			}

			var result = new Dictionary<ConcernPoint, List<RemapCandidateInfo>>();

			foreach (var type in points.Keys)
			{
				var currentResult = DoSingleTypeSearch(points[type], candidates[type], searchType);

				foreach (var kvp in currentResult)
				{
					result[kvp.Key] = kvp.Value;
				}
			}

			return result;
		}

		private Dictionary<ConcernPoint, List<RemapCandidateInfo>> DoSingleTypeSearch(
			List<ConcernPoint> points,
			List<RemapCandidateInfo> candidates,
			SearchType searchType)
		{
			if(candidates.Count == 0)
			{
				return points.ToDictionary(e => e, e => new List<RemapCandidateInfo>());
			}

			var checkSiblings = searchType == SearchType.Local && !UseOldApproach;
			var checkClosest = searchType == SearchType.Local && !UseOldApproach;

			/// Запоминаем соответствие контекстов точкам привязки
			var contextsToPoints = points.GroupBy(p => p.Context)
				.ToDictionary(g => g.Key, g => g.ToList());

			/// В отдельном списке запомниаем вспомогательные контексты,
			/// не совпадающие с контекстами точек привязки
			var auxiliaryContexts = checkClosest
				? contextsToPoints.Keys
					.SelectMany(p => p.ClosestContext)
					.ToList()
				: new List<PointContext>();

			if(checkSiblings)
			{
				foreach (var context in contextsToPoints.Keys)
				{
					auxiliaryContexts.AddRange(context.SiblingsContext.Before.Nearest);
					auxiliaryContexts.AddRange(context.SiblingsContext.After.Nearest);

					if (checkClosest)
					{
						foreach (var closest in context.ClosestContext)
						{
							auxiliaryContexts.AddRange(closest.SiblingsContext.Before.Nearest);
							auxiliaryContexts.AddRange(closest.SiblingsContext.After.Nearest);
						}
					}
				}
			}

			auxiliaryContexts = auxiliaryContexts.Distinct()
				.Except(contextsToPoints.Keys)
				.ToList();

			/// Запоминаем, каким элементам какой элемент предшествовал
			LocationManager = new LocationManager(contextsToPoints.Keys.Concat(auxiliaryContexts));

			/// Результаты поиска элемента, которые вернём пользователю
			var result = new Dictionary<ConcernPoint, List<RemapCandidateInfo>>();

			/// Контексты, которые нашлись на первом этапе (эвристикой)
			var heuristicallyMatched = new HashSet<PointContext>();

			/// При локальном поискк пробуем перепривязаться простым алгоритмом
			if (searchType == SearchType.Local
				&& PreHeuristic != null)
			{
				/// Для каждой точки оцениваем похожесть кандидатов, 
				/// если находим 100% соответствие, исключаем кандидата из списка
				foreach (var pointContext in contextsToPoints.Keys.Concat(auxiliaryContexts))
				{
					var perfectMatch = PreHeuristic.GetSameElement(pointContext, candidates);

					if (perfectMatch != null)
					{
						candidates.Remove(perfectMatch);
						heuristicallyMatched.Add(pointContext);

						perfectMatch.IsAuto = true;
						perfectMatch.Similarity = 1;

						if (contextsToPoints.ContainsKey(pointContext))
						{
							ComputeContextSimilarities(pointContext, new List<RemapCandidateInfo> { perfectMatch }, checkSiblings);

							foreach (var point in contextsToPoints[pointContext])
							{
								result[point] = new List<RemapCandidateInfo> { perfectMatch };
							}
						}

						LocationManager?.Mapped(pointContext, perfectMatch.Context);
					}
				}
			}

			/// Если всё перепривязали простым алгоритмом, можно вернуть результат
			if (contextsToPoints.Keys.Except(heuristicallyMatched).Count() == 0)
			{
				return result;
			}

			/// Контексты, для которых первично посчитаны похожести кандидатов
			var evaluated = new Dictionary<PointContext, List<RemapCandidateInfo>>();

			foreach (var context in auxiliaryContexts.Except(heuristicallyMatched))
			{
				evaluated[context] = ComputeContextSimilarities(
					context,
					candidates.Select(c => new RemapCandidateInfo { Node = c.Node, File = c.File, Context = c.Context }).ToList(),
					checkSiblings
				);
			}

			foreach (var pointContext in contextsToPoints.Keys.Except(heuristicallyMatched))
			{
				var currentCandidates = candidates
					.Select(c => new RemapCandidateInfo
					{
						Node = c.Node,
						File = c.File,
						Context = c.Context
					})
					.ToList();

				evaluated[pointContext] = !UseOldApproach
					? ComputeContextSimilarities(pointContext, currentCandidates, checkSiblings)
					: ComputeContextSimilarities_old(pointContext, currentCandidates, checkSiblings);
			}

			if (!UseOldApproach)
			{
				Parallel.ForEach(
					evaluated.Keys.ToList(),
					key => ComputeTotalSimilarities(key, evaluated[key])
				);		
			}
			else
			{
				Parallel.ForEach(
					evaluated.Keys.ToList(),
					key => ComputeTotalSimilarities_old(evaluated[key])
				);
			}

			if (searchType == SearchType.Local)
			{
				SelectOptimalBests(evaluated);
			}
			else
			{
				SelectBests(evaluated);
			}

			foreach (var kvp in evaluated.Where(kvp => contextsToPoints.ContainsKey(kvp.Key)))
			{
				foreach (var point in contextsToPoints[kvp.Key])
				{
					result[point] = kvp.Value;
				}
			}

			return result;
		}

		private Dictionary<string, List<BorderPoint>> GetNeighbours(ParsedFile file, List<string> types)
		{
			var visitor = new GroupNodesByTypeVisitor(types);
			file.Root.Accept(visitor);

			return visitor.Grouped.ToDictionary(
				e=>e.Key, 
				e=>e.Value
					.SelectMany(n => new List<BorderPoint>
					{
						new BorderPoint
						{
							Node = n,
							Offset = n.Location.Start.Offset,
						},
						new BorderPoint
						{
							Node = n,
							Offset = n.Location.End.Offset,
						},
					})
					.OrderBy(n => n.Offset)
					.ToList()
				);
		}

		#region Auto Result Selection

		private void SelectOptimalBests(
			Dictionary<PointContext, List<RemapCandidateInfo>> evaluationResults)
		{
			/// Для старого алгоритма сортируем один раз
			if(UseOldApproach)
			{
				Parallel.ForEach(
					evaluationResults.Keys.ToList(),
					key =>
					{
						/// Сортируем результаты по похожести
						evaluationResults[key] = evaluationResults[key]
							.OrderByDescending(c => c.Similarity)
							.ToList();
					}
				);
			}

			var unmapped = new HashSet<PointContext>(evaluationResults.Keys);
			var oldCount = unmapped.Count;

			do
			{
				/// Для нового алгоритма после очередных перепривязок надо пересчитать оценки
				/// и отсортировать кандидатов заново
				if (!UseOldApproach)
				{
					Parallel.ForEach(
						unmapped,
						unmappedContext =>
						{
							/// Пересчитываем похожесть ближайших соседей
							if (LocationManager != null)
							{
								foreach (var c in evaluationResults[unmappedContext].Where(c => !c.Deleted))
								{
									c.SiblingsNearestSimilarity =
										LocationManager.GetSimilarity(unmappedContext, c.Context);
								}
							}

							/// Пересчитываем общую похожесть
							ComputeTotalSimilarities(unmappedContext, evaluationResults[unmappedContext]);

							/// Сортируем результаты по похожести
							evaluationResults[unmappedContext] = evaluationResults[unmappedContext]
								.OrderByDescending(c => c.Similarity)
								.ToList();
						}
					);
				}

				oldCount = unmapped.Count;

				foreach (var context in unmapped.ToList())
				{
					var actualCandidates = evaluationResults[context]
						.Where(c => !c.Deleted)
						.ToList();

					if (actualCandidates.Count > 0)
					{
						/// Берём самый похожий элемент и следующий за ним
						var first = actualCandidates[0];
						var second = actualCandidates.Count > 1 ? actualCandidates[1] : null;

						/// Берём наилучшее соответствие текущего кандидата другому исходному элементу
						var otherBestMatch = unmapped
							.Where(e => e != context)
							.Select(e => evaluationResults[e].First(r => !r.Deleted))
							.Where(e => e.Context == first.Context)
							.Max(e => e.Similarity);

						/// Автоматически перепривязываемся, если выполняются локальные условия
						/// и этот элемент не похож в большей степени на что-то другое
						if (IsSimilarEnough(first)
							&& AreDistantEnough(first, second, GAP_MAX)
							&& (otherBestMatch == null 
								|| AreDistantEnough(first.Similarity.Value, otherBestMatch.Value, GAP_MAX)))
						{
							first.IsAuto = true;

							evaluationResults[context].Remove(first);
							evaluationResults[context].Insert(0, first);
							unmapped.Remove(context);

							if (!UseOldApproach)
							{
								LocationManager?.Mapped(context, first.Context);
							}

							foreach (var unmappedContext in unmapped)
							{
								var itemToRemove = evaluationResults[unmappedContext].Single(e => e.Context == first.Context);
								//evaluationResults[unmappedContext].Remove(itemToRemove);
								itemToRemove.Deleted = true;
							}
						}
					}
				}
			}
			while (oldCount != unmapped.Count);
		}

		private void SelectBests(Dictionary<PointContext, List<RemapCandidateInfo>> evaluated)
		{
			Parallel.ForEach(
				evaluated.Keys.ToList(),
				key =>
				{
					evaluated[key] = evaluated[key].OrderByDescending(c => c.Similarity).ToList();

					if (evaluated[key].Count > 0)
					{
						var first = evaluated[key][0];
						var second = evaluated[key].Count > 1 ? evaluated[key][1] : null;

						if (IsSimilarEnough(first)
							&& (second == null || AreDistantEnough(first, second)))
						{
							first.IsAuto = true;
							LocationManager?.Mapped(key, first.Context);
						}
					}
				}
			);
		}

		#endregion

		public void ComputeCoreSimilarities(PointContext point, RemapCandidateInfo candidate)
		{
			candidate.HeaderNonCoreSimilarity =
				Levenshtein(point.HeaderContext.NonCore, candidate.Context.HeaderContext.NonCore);
			candidate.HeaderCoreSimilarity =
				Levenshtein(point.HeaderContext.Core, candidate.Context.HeaderContext.Core);

			candidate.AncestorSimilarity =
				Levenshtein(point.AncestorsContext, candidate.Context.AncestorsContext);
			candidate.InnerSimilarity =
				EvalSimilarity(point.InnerContext, candidate.Context.InnerContext);
		}

		public List<RemapCandidateInfo> ComputeCoreContextSimilarities(
			PointContext point,
			List<RemapCandidateInfo> candidates)
		{
			Parallel.ForEach(
				candidates,
				c => ComputeCoreSimilarities(point, c)
			);

			return candidates;
		}

		public List<RemapCandidateInfo> ComputeContextSimilarities(
			PointContext point,
			List<RemapCandidateInfo> candidates,
			bool checkSiblings)
		{
			var actualCandidates = candidates.Where(c => !c.Deleted).ToList();
			var checkAllSiblings = checkSiblings && (candidates.FirstOrDefault()?.Node.Options.GetNotUnique() ?? false);

			Parallel.ForEach(
				actualCandidates,
				c =>
				{
					ComputeCoreSimilarities(point, c);

					if (checkSiblings && point.SiblingsContext != null)
					{
						c.SiblingsNearestSimilarity = LocationManager.GetSimilarity(point, c.Context);

						if (checkAllSiblings && point.SiblingsContext.Before?.All != null)
						{
							c.SiblingsAllSimilarity = EvalSimilarity(point.SiblingsContext, c.Context.SiblingsContext);
						}
					}
				}
			);

			return candidates;
		}

		public void ComputeTotalSimilarities(
			PointContext sourceContext,
			List<RemapCandidateInfo> candidates)
		{
			var actualCandidates = candidates.Where(c => !c.Deleted).ToList();

			if (actualCandidates.Count == 0)
			{
				return;
			}

			foreach(var candidate in actualCandidates)
			{
				candidate.Similarity = null;
			}

			var weights = new Dictionary<ContextType, double?>();

			foreach (var val in Enum.GetValues(typeof(ContextType)).Cast<ContextType>())
			{
				weights[val] = null;
			};

			foreach (var h in TuningHeuristics)
			{
				h.TuneWeights(sourceContext, actualCandidates, weights);
			}

			var finalWeights = weights.ToDictionary(e => e.Key, e => e.Value ?? 0);

			actualCandidates.ForEach(c =>
			{
				c.Similarity = c.Similarity ??
					(finalWeights[ContextType.Ancestors] * c.AncestorSimilarity
						+ finalWeights[ContextType.Inner] * c.InnerSimilarity
						+ finalWeights[ContextType.HeaderNonCore] * c.HeaderNonCoreSimilarity
						+ finalWeights[ContextType.HeaderCore] * c.HeaderCoreSimilarity
						+ finalWeights[ContextType.SiblingsNearest] * c.SiblingsNearestSimilarity
						+ finalWeights[ContextType.SiblingsAll] * c.SiblingsAllSimilarity)
					/ finalWeights.Values.Sum();
				c.Weights = finalWeights;
			});

			foreach (var h in ScoringHeuristics)
			{
				 h.PredictSimilarity(sourceContext, actualCandidates);
			}
		}

		public void RunML(Dictionary<PointContext, List<RemapCandidateInfo>> elements)
		{
			/// Создаём временный файл с данными о кандидатах для перепривязываемых точек
			var featuresFilePath = Path.GetTempFileName();

			/// Записываем информацию обо всех кандидатах
			using (var fileWriter = new StreamWriter(featuresFilePath))
			{
				fileWriter.WriteLine(CandidateFeatures.ToHeaderString(";"));

				foreach (var kvp in elements)
				{
					foreach (var str in GetFeatures(kvp.Key, kvp.Value).Select(e => e.ToString(";")))
					{
						fileWriter.WriteLine(str);
					}
				}
			}

			var predictionsFilePath = Path.GetTempFileName();

			/// Ищем подходящую модель для предсказания
			var availableModels = Directory.GetFiles("Resources/Models")
				.Select(m => Path.GetFullPath(m).ToLower())
				.ToList();
			/// Сначала ищем модель для типа файла и типа сущности
			var modelPath = availableModels.FirstOrDefault(m =>
				m.Contains(Path.GetExtension(elements.Keys.First().FileName).Trim('.'))
			);

			/// TODO если не нашли, нужно подгружать общую модель

			/// Запускаем питоновский скрипт
			var process = new Process();
			ProcessStartInfo startInfo = new ProcessStartInfo()
			{
				FileName = "python",
				Arguments = $"\"{Path.GetFullPath("Resources/apply_ml.py")}\" \"{modelPath}\" \"{featuresFilePath}\" \"{predictionsFilePath}\"",
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				UseShellExecute = false
			};
			process.StartInfo = startInfo;
			process.Start();

			process.WaitForExit();

			var predictions = File.ReadAllText(predictionsFilePath.Trim())
				.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(l => double.Parse(l))
				.ToList();

			var idx = 0;
			foreach (var kvp in elements)
			{
				foreach (var candidate in kvp.Value)
				{
					candidate.Similarity = predictions[idx++];
				}
			}
		}

		/// <summary>
		/// Поиск узлов дерева, соответствующих точкам привязки
		/// </summary>
		public Dictionary<ConcernPoint, List<RemapCandidateInfo>> FindPoints(
			List<ConcernPoint> points,
			List<ParsedFile> searchArea,
			SearchType searchType)
		{
			List<ParsedFile> files = null;
			Dictionary<string, List<ConcernPoint>> groupedByType = null;

			var overallResult = new Dictionary<ConcernPoint, List<RemapCandidateInfo>>();

			switch (searchType)
			{
				case SearchType.Global:
					groupedByType = points
						.GroupBy(p => p.Context.Type)
						.ToDictionary(e => e.Key, e => e.ToList());
					files = searchArea;

					var globalResult = DoMultiTypeSearch(groupedByType, files, searchType);

					foreach (var elem in globalResult)
					{
						overallResult[elem.Key] = elem.Value;
					}

					break;
				case SearchType.Local:
					var groupedPoints = points
						.GroupBy(p => p.Context.FileName)
						.ToDictionary(e => e.Key, e =>
							e.GroupBy(p => p.Context.Type).ToDictionary(el => el.Key, el => el.ToList())
						);

					foreach (var fileName in groupedPoints.Keys)
					{
						/// При поиске в том же файле ищем тот же файл по полному совпадению пути
						files = searchArea.Where(f => f.Name == fileName).ToList();

						var localResult = DoMultiTypeSearch(groupedPoints[fileName], files, searchType);

						foreach (var elem in localResult)
						{
							overallResult[elem.Key] = elem.Value;
						}
					}

					break;
			}

			return overallResult;
		}

		public List<(LineContext, SegmentLocation, double)> FindLine(
			LineContext context,
			Node outerNode,
			ParsedFile file)
		{
			const double MIN_WEIGHT = 0.25;
			const double MAX_WEIGHT = 1;

			var currentOffset = outerNode.Location.Start.Offset;
			var lineEnd = GetLineEnd(file.Text);

			/// Разбиваем текст объемлющей сущности на строки
			var rawLines = file.Text
				.Substring(outerNode.Location.Start.Offset, outerNode.Location.Length.Value)
				.Split(new string[] { lineEnd }, StringSplitOptions.None);

			if (rawLines.Length == 0)
			{
				return new List<(LineContext, SegmentLocation, double)> { (null, null, 0) };
			}

			/// Для каждой строки вычисляем контекст строки и считаем похожести на искомую строку
			var lines = rawLines.Select((e, i) => {
				var location = new SegmentLocation
				{
					Start = new PointLocation(outerNode.Location.Start.Line + i, 0, currentOffset),
					End = new PointLocation(outerNode.Location.Start.Line + i, 0, (currentOffset += e.Length + lineEnd.Length) - lineEnd.Length)
				};

				var lineContext = new LineContext(
					outerNode.Location,
					location,
					file.Text,
					new TextOrHash(e)
				);

				return new
				{
					Context = lineContext,
					Location = location,
					InnerSimilarity = EvalSimilarity(context.InnerContext, lineContext.InnerContext),
					OuterSimilarity = (EvalSimilarity(context.OuterContext.Item1, lineContext.OuterContext.Item1)
						+ EvalSimilarity(context.OuterContext.Item2, lineContext.OuterContext.Item2)) / 2.0
				};
			}).ToList();

			/// Сортируем строки по убыванию внутренней похожести
			var orderedByInner = lines.OrderByDescending(l => l.InnerSimilarity).ToList();

			/// Признак того, что можно перепутать искомую строчку с какой-то другой
			var mayBeConfusedByInner = context.HadSame 
				|| orderedByInner.TakeWhile(e => !AreDistantEnough(orderedByInner[0].InnerSimilarity, e.InnerSimilarity, GAP_MAX)).Count() > 1;

			var innerWeight = mayBeConfusedByInner ? MIN_WEIGHT : MAX_WEIGHT;
			var outerWeight = mayBeConfusedByInner ? MAX_WEIGHT : MIN_WEIGHT;

			var totalSimilarities = lines.ToDictionary(l => l,
				l => (l.InnerSimilarity * innerWeight + l.OuterSimilarity * outerWeight) / (innerWeight + outerWeight));

			lines = lines
				.OrderByDescending(l => totalSimilarities[l])
				.ToList();

			return lines.Select(e=>(e.Context, e.Location, totalSimilarities[e])).ToList();
		}

		#region EvalSimilarity

		public double EvalSimilarity(HeaderContextElement a, HeaderContextElement b)
		{
			return a.EqualsIgnoreValue(b)
				? a.ExactMatch
					? a.Value.SequenceEqual(b.Value) ? 1 : 0
					: Levenshtein(a.Value, b.Value)
				: double.MinValue;
		}

		public double EvalSimilarity(InnerContext a, InnerContext b)
		{
			return EvalSimilarity(a.Content, b.Content);
		}

		public double EvalSimilarity(AncestorsContextElement a, AncestorsContextElement b)
		{
			return a.Type == b.Type 
				? Levenshtein(a.HeaderContext.Sequence, b.HeaderContext.Sequence) 
				: double.MinValue;
		}

		public double EvalSimilarity(PrioritizedWord a, PrioritizedWord b)
		{
			return a.Priority == b.Priority 
				? Levenshtein(a.Text, b.Text, true)
				: double.MinValue;
		}

		public double EvalSimilarity(TextOrHash a, TextOrHash b)
		{
			var score = a.Text != null && b.Text != null
				? Levenshtein(a.Text, b.Text)
				: a.Hash != null && b.Hash != null
					? FuzzyHashing.CompareHashes(a.Hash, b.Hash)
					: 0;

			return score < FuzzyHashing.MIN_TEXT_LENGTH / (double)TextOrHash.MAX_TEXT_LENGTH
				? 0 : score;
		}

		public double EvalSimilarity(SiblingsContext a, SiblingsContext b)
		{
			if (a.Before.All.TextLength == 0 && a.After.All.TextLength == 0)
			{
				return b.Before.All.TextLength == 0 && b.After.All.TextLength == 0 ? 1 : 0;
			}

			var beforeSimilarity = EvalSimilarity(a.Before.All, b.Before.All);
			var afterSimilarity = EvalSimilarity(a.After.All, b.After.All);

			return (beforeSimilarity + afterSimilarity) / 2;
		}

		#endregion

		#region Methods 

		/// Похожесть новой последовательности на старую 
		/// при переходе от последовательности a к последовательности b
		private double DispatchLevenshtein<T>(T a, T b)
		{
			if (a is IEnumerable<string>)
				return Levenshtein((IEnumerable<string>)a, (IEnumerable<string>)b);
			if (a is IEnumerable<HeaderContextElement>)
				return Levenshtein((IEnumerable<HeaderContextElement>)a, (IEnumerable<HeaderContextElement>)b);
			else if (a is string)
				return Levenshtein(a as string, b as string);
			else if (a is HeaderContextElement)
				return EvalSimilarity(a as HeaderContextElement, b as HeaderContextElement);
			else if (a is InnerContext)
				return EvalSimilarity(a as InnerContext, b as InnerContext);
			else if (a is AncestorsContextElement)
				return EvalSimilarity(a as AncestorsContextElement, b as AncestorsContextElement);
			else if (a is PrioritizedWord)
				return EvalSimilarity(a as PrioritizedWord, b as PrioritizedWord);
			else
				return a.Equals(b) ? 1 : 0;
		}

		///  Похожесть на основе расстояния Левенштейна
		private double Levenshtein<T>(IEnumerable<T> a, IEnumerable<T> b)
		{
			if (a.Count() == 0 ^ b.Count() == 0)
				return 0;
			if (a.Count() == 0 && b.Count() == 0)
				return 1;

			var denominator = 0.0;

			if (a is IEnumerable<HeaderContextElement>)
			{
				var comparer = new EqualsIgnoreValueComparer();

				var aSockets = (a as IEnumerable<IEqualsIgnoreValue>)
					.GroupBy(e => e, comparer).ToDictionary(g => g.Key, g => g.Count());
				var bSockets = (b as IEnumerable<IEqualsIgnoreValue>)
					.GroupBy(e => e, comparer).ToDictionary(g => g.Key, g => g.Count());

				denominator += aSockets.Sum(kvp
					=> ((HeaderContextElement)kvp.Key).Priority * kvp.Value);

				foreach (var kvp in aSockets)
				{
					var sameKey = bSockets.Keys.FirstOrDefault(e => e.EqualsIgnoreValue(kvp.Key));

					if (sameKey != null)
						bSockets[sameKey] -= kvp.Value;
				}

				denominator += bSockets.Where(kvp => kvp.Value > 0)
					.Sum(kvp => ((HeaderContextElement)kvp.Key).Priority * kvp.Value);
			}
			//else if (a is List<string>)
			//{
			//	denominator = Math.Max(((List<string>)a).Sum(e => e.Length), ((List<string>)b).Sum(e => e.Length));
			//}
			else if (a is IEnumerable<PrioritizedWord>)
			{
				var aSockets = (a as IEnumerable<PrioritizedWord>)
					.GroupBy(e => e.Priority).ToDictionary(g => g.Key, g => g.Count());
				var bSockets = (b as IEnumerable<PrioritizedWord>)
					.GroupBy(e => e.Priority).ToDictionary(g => g.Key, g => g.Count());

				denominator += aSockets.Sum(kvp => kvp.Key * kvp.Value);

				foreach (var kvp in aSockets)
				{
					if (bSockets.ContainsKey(kvp.Key))
					{
						bSockets[kvp.Key] -= kvp.Value;
					}
				}

				denominator += bSockets.Where(kvp => kvp.Value > 0)
					.Sum(kvp => kvp.Key * kvp.Value);
			}
			else
			{
				denominator = Math.Max(a.Count(), b.Count());
			}

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
				distances[i, 0] = distances[i - 1, 0] + PriorityCoefficient(a.ElementAt(i - 1));
			for (int j = 1; j <= b.Count(); ++j)
				distances[0, j] = distances[0, j - 1] + PriorityCoefficient(b.ElementAt(j - 1));

			for (int i = 1; i <= a.Count(); i++)
				for (int j = 1; j <= b.Count(); j++)
				{
					/// Если элементы - это тоже перечислимые наборы элементов, считаем для них расстояние
					double cost = 1 - DispatchLevenshtein(a.ElementAt(i - 1), b.ElementAt(j - 1));
					distances[i, j] = Math.Min(Math.Min(
						distances[i - 1, j] + PriorityCoefficient(a.ElementAt(i - 1)),
						distances[i, j - 1] + PriorityCoefficient(b.ElementAt(j - 1))),
						distances[i - 1, j - 1] + PriorityCoefficient(a.ElementAt(i - 1)) * cost);
				}

			return 1 - distances[a.Count(), b.Count()] / denominator;
		}

		private double Levenshtein(string a, string b, bool areWords = false)
		{
			if (a.Length == 0 ^ b.Length == 0)
				return 0;
			if (a.Length == 0 && b.Length == 0)
				return 1;

			var denominator = (double)Math.Max(a.Length, b.Length);

			/// Сразу отбрасываем общие префиксы и суффиксы
			var commonPrefixLength = 0;
			while (commonPrefixLength < a.Length && commonPrefixLength < b.Length
				&& a[commonPrefixLength].Equals(b[commonPrefixLength]))
				++commonPrefixLength;
			a = a.Substring(commonPrefixLength);
			b = b.Substring(commonPrefixLength);

			var commonSuffixLength = 0;
			while (commonSuffixLength < a.Length && commonSuffixLength < b.Length
				&& a[a.Length - 1 - commonSuffixLength].Equals(b[b.Length - 1 - commonSuffixLength]))
				++commonSuffixLength;
			a = a.Substring(0, a.Length - commonSuffixLength);
			b = b.Substring(0, b.Length - commonSuffixLength);

			if (a.Length == 0 && b.Length == 0)
				return 1;

			/// Согласно алгоритму Вагнера-Фишера, вычисляем матрицу расстояний
			var distances = new double[a.Length + 1, b.Length + 1];
			distances[0, 0] = 0;

			/// Заполняем первую строку и первый столбец
			for (int i = 1; i <= a.Length; ++i)
				distances[i, 0] = distances[i - 1, 0] + 1;
			for (int j = 1; j <= b.Length; ++j)
				distances[0, j] = distances[0, j - 1] + 1;

			for (int i = 1; i <= a.Length; i++)
				for (int j = 1; j <= b.Length; j++)
				{
					/// Если элементы - это тоже перечислимые наборы элементов, считаем для них расстояние
					double cost = a[i - 1] == b[j - 1] ? 0 : 1;
					distances[i, j] = Math.Min(Math.Min(
						distances[i - 1, j] + 1,
						distances[i, j - 1] + 1),
						distances[i - 1, j - 1] + cost);
				}

			var similarity = 1 - distances[a.Length, b.Length] / denominator;

			return areWords 
				? similarity >= 0.5 ? similarity : 0 
				: similarity;
		}

		private static double PriorityCoefficient(object elem)
		{
			switch (elem)
			{
				case HeaderContextElement headerContext:
					return headerContext.Priority;
				case PrioritizedWord word:
					return word.Priority;
				//case string str:
				//	return str.Length;
				default:
					return 1;
			}
		}

		private bool EnsureRootExists(ParsedFile file)
		{
			if (file.Root == null)
				file.Root = GetParsed(file.Name)?.Root;

			return file.Root != null;
		}

		public static string GetLineEnd(string text)
		{
			var lineEnd = String.Join("", text
				.SkipWhile(c => !LINE_END_SYMBOLS.Contains(c))
				.TakeWhile(c => LINE_END_SYMBOLS.Contains(c))
			);
			
			return !String.IsNullOrEmpty(lineEnd)
				? lineEnd : DEFAULT_LINE_END;
		}

		public static bool IsSimilarEnough(RemapCandidateInfo candidate) =>
			candidate.Similarity >= CANDIDATE_SIMILARITY_THRESHOLD;

		public static bool AreDistantEnough(RemapCandidateInfo first, RemapCandidateInfo second, double? staticGap = null) =>
			/// Либо у нас один кандидат
			second == null 
			/// Либо первый похож на 100%, а второй - нет
			|| first.Similarity == 1 && second.Similarity != 1
			/// Либо оба не похожи на 100% и достаточно отстоят друг от друга
			|| second.Similarity != 1 && 1 - second.Similarity >= (1 - first.Similarity) * (staticGap ?? Math.Max(GAP_MIN, GAP_MAX - (GAP_MAX - GAP_MIN) * (1 - first.Similarity.Value) / (1 - GAP_THRESHOLD)));

		public static bool AreDistantEnough(double first, double second, double? staticGap = null) =>
			first == 1 && second != 1
			|| second != 1 && 1 - second >= (1 - first) * (staticGap ?? Math.Max(GAP_MIN, GAP_MAX - (GAP_MAX - GAP_MIN) * (1 - first) / (1 - GAP_THRESHOLD)));

		#endregion

		#region Old

		public void ComputeTotalSimilarities_old(List<RemapCandidateInfo> candidates)
		{
			if (candidates.Count == 0)
			{
				return;
			}

			candidates.ForEach(c =>
			{
				c.Similarity = (2 * c.AncestorSimilarity
					+ 1 * c.InnerSimilarity
					+ 1 * c.HeaderNonCoreSimilarity
					+ 3 * c.HeaderCoreSimilarity) / 7.0;
				c.Weights = null;
			});
		}

		public void ComputeCoreSimilarities_old(PointContext point, RemapCandidateInfo candidate)
		{
			candidate.HeaderNonCoreSimilarity = Levenshtein_old(
				point.HeaderContext.Sequence_old,
				candidate.Context.HeaderContext.Sequence_old
			);
			candidate.HeaderCoreSimilarity = Levenshtein_old(
				point.HeaderContext.Core_old,
				candidate.Context.HeaderContext.Core_old
			);
			candidate.AncestorSimilarity = Levenshtein_old(
				point.AncestorsContext,
				candidate.Context.AncestorsContext
			);
			candidate.InnerSimilarity = EvalSimilarity_old(
				point.InnerContext_old,
				candidate.Context.InnerContext_old
			);
		}

		private double EvalSimilarity_old(List<ContextElement> a, List<ContextElement> b) =>
			a.Count == 0 && b.Count == 0 ? 1 : a.Intersect(b).Count() / (double)Math.Max(a.Count, 1);

		public List<RemapCandidateInfo> ComputeContextSimilarities_old(
			PointContext point,
			List<RemapCandidateInfo> candidates,
			bool checkSiblings)
		{
			var checkAllSiblings = checkSiblings
				&& (candidates.FirstOrDefault()?.Node.Options.GetNotUnique() ?? false);

			Parallel.ForEach(
				candidates,
				c =>
				{
					ComputeCoreSimilarities_old(point, c);

					if (checkAllSiblings)
					{
						c.SiblingsAllSimilarity =
							(EvalSimilarity_old(point.SiblingsContext_old.Item1, c.Context.SiblingsContext_old.Item1)
							+ EvalSimilarity_old(point.SiblingsContext_old.Item2, c.Context.SiblingsContext_old.Item2)) / 2.0;
					}
				}
			);

			return candidates;
		}

		private double Levenshtein_old<T>(IEnumerable<T> a, IEnumerable<T> b)
		{
			if (a.Count() == 0 ^ b.Count() == 0)
				return 0;
			if (a.Count() == 0 && b.Count() == 0)
				return 1;

			var denominator = Math.Max(a.Count(), b.Count());

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
				distances[i, 0] = distances[i - 1, 0] + 1;
			for (int j = 1; j <= b.Count(); ++j)
				distances[0, j] = distances[0, j - 1] + 1;

			for (int i = 1; i <= a.Count(); i++)
				for (int j = 1; j <= b.Count(); j++)
				{
					/// Если элементы - это тоже перечислимые наборы элементов, считаем для них расстояние
					double cost = 1 - DispatchLevenshtein_old(a.ElementAt(i - 1), b.ElementAt(j - 1));
					distances[i, j] = Math.Min(Math.Min(
						distances[i - 1, j] + 1,
						distances[i, j - 1] + 1),
						distances[i - 1, j - 1] + cost);
				}

			return 1 - distances[a.Count(), b.Count()] / denominator;
		}

		private double DispatchLevenshtein_old<T>(T a, T b)
		{
			if (a is string)
			{
				return Levenshtein(a as string, b as string);
			}
			else if (a is AncestorsContextElement)
			{
				var aElem = a as AncestorsContextElement;
				var bElem = b as AncestorsContextElement;

				return aElem.Type == bElem.Type
					? Levenshtein_old(aElem.HeaderContext.Sequence_old, bElem.HeaderContext.Sequence_old) : 0;
			}
			else
			{
				return a.Equals(b) ? 1 : 0;
			}
		}

		#endregion
	}
}
