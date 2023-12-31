﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Land.Core;
using Land.Core.Parsing.Tree;
using Land.Markup.Binding;
using Land.Markup.Relations;
using Land.Markup.Tree;
using Land.Markup.CoreExtension;
using Land.Core.Specification;

namespace Land.Markup
{
	public class MarkupManager
	{
		public bool HasUnsavedChanges { get; set; }

		public MarkupManager(Func<string, ParsedFile> getParsed, IPreHeuristic remappingHeuristic)
		{
			ContextFinder.GetParsed = getParsed;
			ContextFinder.PreHeuristic = remappingHeuristic;

			OnMarkupChanged += InvalidateRelations;
			OnMarkupChanged += SetHasUnsavedChanges;

			#region Подключение эвристик

			ContextFinder.SetHeuristic(typeof(EmptyContextHeuristic));
			ContextFinder.SetHeuristic(typeof(TuneInnerWeightAsFrequentlyChanging));
			ContextFinder.SetHeuristic(typeof(TuneSiblingsAllWeightAsFrequentlyChanging));
			ContextFinder.SetHeuristic(typeof(TuneSiblingsNearestWeight));
			ContextFinder.SetHeuristic(typeof(TuneInnerWeightAccordingToLength));
			ContextFinder.SetHeuristic(typeof(TuneAncestorsWeight));
			ContextFinder.SetHeuristic(typeof(TuneHeaderWeight));
			ContextFinder.SetHeuristic(typeof(DefaultWeightsHeuristic));

			#endregion
		}

		private RelationsManager Relations { get; set; } = new RelationsManager();

		public List<RelationNotification> TryGetRelations(out RelationsManager relationsManager)
		{
			if (IsValid)
			{
				if (!Relations.IsValid)
					Relations.RefreshCache(Markup);

				relationsManager = Relations;
				return Relations.CheckConsistency();
			}

			relationsManager = null;
			return new List<RelationNotification>();
		}

		public ContextFinder ContextFinder { get; private set; } = new ContextFinder();

		/// <summary>
		/// Коллекция точек привязки
		/// </summary>
		public ObservableCollection<MarkupElement> Markup = new ObservableCollection<MarkupElement>();

		/// <summary>
		/// Событие изменения разметки
		/// </summary>
		public event Action OnMarkupChanged;

		/// <summary>
		/// Очистка разметки
		/// </summary>
		public void Clear()
		{
			Markup.Clear();

			OnMarkupChanged?.Invoke();

			HasUnsavedChanges = false;
		}

		/// <summary>
		/// Проверка того, что вся разметка синхронизирована с кодом
		/// </summary>
		/// <returns></returns>
		public bool IsValid => !GetLinearSequenceVisitor.GetPoints(Markup).Any(p => p.HasInvalidLocation);

		/// <summary>
		/// Помечаем отношения как нерелевантные относительно разметки
		/// </summary>
		public void InvalidateRelations()
		{
			Relations.IsValid = false;
		}

		public void SetHasUnsavedChanges()
        {
			HasUnsavedChanges = true;
        }

		/// <summary>
		/// Сброс узлов дерева у всех точек, связанных с указанным файлом
		/// </summary>
		public void InvalidatePoints(string fileName)
		{
			DoWithMarkup((MarkupElement elem) =>
			{
				if (elem is ConcernPoint concernPoint
					&& concernPoint.Context.FileName == fileName)
				{
					/// Если координаты узла отсутствуют, локация уже обозначена как Missing
					if (concernPoint.NodeLocation != null)
					{
						concernPoint.NodeLocation = new SegmentLocation
						{
							Start = new PointLocation(0, 0, 0),
							End = new PointLocation(0, 0, 0)
						};

						if (concernPoint.LineContext != null)
						{
							concernPoint.LineLocation = new SegmentLocation
							{
								Start = new PointLocation(0, 0, 0),
								End = new PointLocation(0, 0, 0)
							};
						}

						concernPoint.HasIrrelevantLocation = true;
					}
				}
			});

			ContextFinder.ContextManager.ClearCache(fileName);
		}

		/// <summary>
		/// Удаление элемента разметки
		/// </summary>
		public void RemoveElement(MarkupElement elem)
		{
			if (elem.Parent != null)
			{
				elem.Parent.Elements.Remove(elem);
			}
			else
			{
				Markup.Remove(elem);
			}

			if(elem is ConcernPoint point)
			{
				point.Context.UnlinkPoint(point.Id);

				if(point.LineContext != null)
				{
					point.LineContext.UnlinkPoint(point.Id);
				}
			}

			OnMarkupChanged?.Invoke();
		}

		/// <summary>
		/// Добавление функциональности
		/// </summary>
		public Concern AddConcern(
			string name, 
			string comment = null, 
			MarkupElement targetElement = null)
		{
			var parent = targetElement is ConcernPoint concernPoint
				? concernPoint.Parent
				: targetElement as Concern;
			var concern = new Concern(name, comment, parent);

			AddElement(concern);

			OnMarkupChanged?.Invoke();
			return concern;
		}

		/// <summary>
		/// Добавление точки привязки
		/// </summary>
		public ConcernPoint AddConcernPoint(
			Node node,
			SegmentLocation line,
			ParsedFile file, 
			string name = null, 
			string comment = null, 
			MarkupElement targetElement = null)
		{
			Remap(node.Type, file, true);

			var parent = targetElement is ConcernPoint concernPoint
				? concernPoint.Parent
				: targetElement as Concern;

			var siblingsArgs = new SiblingsConstructionArgs
			{
				ContextFinder = ContextFinder
			};

			var context = ContextFinder.ContextManager.GetContext(
				node,
				file,
				siblingsArgs,
				new ClosestConstructionArgs
				{
					SearchArea = new List<ParsedFile> { file },
					GetParsed = ContextFinder.GetParsed,
					ContextFinder = ContextFinder,
					SiblingsArgs = siblingsArgs
				}
			);

			var lineContext = line != null 
				? new LineContext(node.Location, line, file.Text) 
				: null;

			var point = new ConcernPoint(
				name ?? ConcernPoint.GetDefaultName(node),
				comment,
				context,
				node.Location,
				lineContext,
				line,
				parent
			);

			AddElement(point);

			OnMarkupChanged?.Invoke();
			return point;
		}

		/// <summary>
		/// Добавление всей "суши", присутствующей в дереве разбора
		/// </summary>
		public void AddLand(ParsedFile file)
		{
			var visitor = new LandExplorerVisitor();
			file.Root.Accept(visitor);

			/// Группируем land-сущности по типу (символу)
			foreach (var group in visitor.Land.GroupBy(l => l.UserifiedSymbol))
			{
				Remap(group.Key, file, true);

				var concern = AddConcern(group.Key);
				/// В пределах символа группируем по псевдониму
				var subgroups = group.GroupBy(g => g.Alias);

				/// Для всех точек, для которых указан псевдоним
				foreach (var subgroup in subgroups.Where(s => !String.IsNullOrEmpty(s.Key)))
				{
					/// создаём подфункциональность
					var subconcern = AddConcern(subgroup.Key, null, concern);

					var subconcernSiblingsArgs = new SiblingsConstructionArgs
					{
						ContextFinder = ContextFinder
					};

					foreach (var node in subgroup)
					{
						AddElement(new ConcernPoint(
							ConcernPoint.GetDefaultName(node),
							null,
							ContextFinder.ContextManager.GetContext(
								node, 
								file,
								subconcernSiblingsArgs,
								new ClosestConstructionArgs
								{
									SearchArea = new List<ParsedFile> { file },
									GetParsed = ContextFinder.GetParsed,
									ContextFinder = ContextFinder,
									SiblingsArgs = subconcernSiblingsArgs
								}
							),
							node.Location,
							null,
							null,
							subconcern
						));
					}
				}

				/// Остальные добавляются напрямую к функциональности, соответствующей символу
				var nodes = subgroups.Where(s => String.IsNullOrEmpty(s.Key))
					.SelectMany(s => s).ToList();

				var siblingsArgs = new SiblingsConstructionArgs
				{
					ContextFinder = ContextFinder
				};

				foreach (var node in nodes)
				{
					AddElement(new ConcernPoint(
						ConcernPoint.GetDefaultName(node),
						null,
						ContextFinder.ContextManager.GetContext(
							node, 
							file,
							siblingsArgs,
							new ClosestConstructionArgs
							{
								SearchArea = new List<ParsedFile> { file },
								GetParsed = ContextFinder.GetParsed,
								ContextFinder = ContextFinder,
								SiblingsArgs = siblingsArgs
							}
						),
						node.Location,
						null,
						null,
						concern
					));
				}
			}

			OnMarkupChanged?.Invoke();
		}

		/// <summary>
		/// Получение всех узлов, к которым можно привязаться,
		/// если команда привязки была вызвана в позиции offset
		/// </summary>
		public LinkedList<Node> GetConcernPointCandidates(Node root, SegmentLocation selection)
		{
			var pointCandidates = new LinkedList<Node>();
			var currentNode = root;

			/// В качестве кандидатов на роль помечаемого участка рассматриваем узлы от корня,
			/// содержащие текущую позицию каретки
			while (currentNode != null)
			{
				if (currentNode.Options.IsSet(MarkupOption.GROUP_NAME, MarkupOption.LAND))
				{
					pointCandidates.AddFirst(currentNode);
				}

				currentNode = currentNode.Children
					.Where(c => c.Location != null && c.Location.Includes(selection))
					.FirstOrDefault();
			}

			return pointCandidates;
		}

		/// <summary>
		/// Смена узла, к которому привязана точка
		/// </summary>
		public void RelinkConcernPoint(
			ConcernPoint point, 
			Node node,
			SegmentLocation lineLocation,
			ParsedFile file)
		{
			var siblingsArgs = new SiblingsConstructionArgs
			{
				ContextFinder = ContextFinder
			};

			point.Relink(
				ContextFinder.ContextManager.GetContext(
					node, 
					file,
					siblingsArgs,
					new ClosestConstructionArgs
					{
						SearchArea = new List<ParsedFile> { file },
						GetParsed = ContextFinder.GetParsed,
						ContextFinder = ContextFinder,
						SiblingsArgs = siblingsArgs
					}
				),
				node.Location,
				lineLocation != null ? new LineContext(node.Location, lineLocation, file.Text) : null,
				lineLocation
			);

			OnMarkupChanged?.Invoke();
		}

		/// <summary>
		/// Смена узла, к которому привязана точка
		/// </summary>
		public void RelinkConcernPoint(ConcernPoint point, RemapCandidateInfo candidate)
		{
			var siblingsArgs = new SiblingsConstructionArgs
			{
				ContextFinder = ContextFinder
			};

			var context = ContextFinder.ContextManager.GetContext(
				candidate.Node,
				candidate.File,
				siblingsArgs,
				new ClosestConstructionArgs
				{
					SearchArea = new List<ParsedFile> { candidate.File },
					GetParsed = ContextFinder.GetParsed,
					ContextFinder = ContextFinder,
					SiblingsArgs = siblingsArgs
				}
			);

			var (lineContext, lineLocation, totalSimilarity) = point.LineContext != null
				? ContextFinder.FindLine(
					point.LineContext,
					candidate.Node,
					candidate.File
				)[0]
				: (null, null, 0);

			point.Relink(context, candidate.Node.Location, lineContext, lineLocation);

			OnMarkupChanged?.Invoke();
		}

		/// <summary>
		/// Получение списка точек привязки для текущего дерева разметки
		/// </summary>
		public List<ConcernPoint> GetConcernPoints()
		{
			return GetLinearSequenceVisitor.GetPoints(Markup);
		}

		public HashSet<PointContext> GetPointContexts()
		{
			var contextsSet = new HashSet<PointContext>();
			var points = GetConcernPoints();

			foreach (var point in points)
			{
				contextsSet.Add(point.Context);
				contextsSet.UnionWith(point.Context.ClosestContext);

				if(point.Context.SiblingsContext != null)
				{
					contextsSet.UnionWith(point.Context.SiblingsContext.Before.Nearest);
					contextsSet.UnionWith(point.Context.SiblingsContext.After.Nearest);
				}
			}

			return contextsSet;
		}

		/// <summary>
		/// Перемещение элемента разметки к новому родителю
		/// </summary>
		public void MoveTo(Concern newParent, MarkupElement elem)
		{
			if (elem.Parent != null)
				elem.Parent.Elements.Remove(elem);
			else
				Markup.Remove(elem);

			elem.Parent = newParent;

			if(newParent != null)
				newParent.Elements.Add(elem);
			else
				Markup.Add(elem);

			OnMarkupChanged?.Invoke();
		}

		/// <summary>
		/// Поместить элемент разметки после указанного элемента разметки
		/// </summary>
		public void MoveTo(MarkupElement before, MarkupElement elem)
		{
			var beforeIndex = before.Parent != null
				? before.Parent.Elements.IndexOf(before)
				: Markup.IndexOf(before);
			var collectionToInsert = before.Parent != null
				? before.Parent.Elements
				: Markup;

			if (elem.Parent != null)
			{
				elem.Parent.Elements.Remove(elem);
			}
			else
			{
				Markup.Remove(elem);
			}

			elem.Parent = before.Parent;

			if (collectionToInsert.Count == beforeIndex + 1)
			{
				collectionToInsert.Add(elem);
			}
			else
			{
				collectionToInsert.Insert(beforeIndex + 1, elem);
			}

			OnMarkupChanged?.Invoke();
		}

		public void Serialize(string fileName, bool useRelativePaths)
		{
			var pointContexts = GetPointContexts();
			var fileGroups = pointContexts.GroupBy(e => e.FileName);

			/// Правим связи, убираем из ближайших потерянные или удалённые точки
			var concernPointsDict = GetConcernPoints()
				.ToDictionary(cp => cp.Id, cp => cp);

			foreach(var cp in concernPointsDict.Values)
			{
				cp.Context.LinkedClosestPoints.IntersectWith(
					cp.Context.LinkedClosestPoints.Where(id => concernPointsDict.ContainsKey(id))
				);

				cp.Context.LinkedBeforeNeighbours.IntersectWith(
					cp.Context.LinkedBeforeNeighbours.Where(id => concernPointsDict.ContainsKey(id))
				);

				cp.Context.LinkedAfterNeighbours.IntersectWith(
					cp.Context.LinkedAfterNeighbours.Where(id => concernPointsDict.ContainsKey(id))
				);
			}

			/// Если нужно сохранить в файле разметки относиельные пути
			if (useRelativePaths)
			{
				/// Превращаем указанные в точках привязки абсолютные пути в пути относительно файла разметки
				var directoryUri = new Uri(Path.GetDirectoryName(fileName) + "/");

				foreach (var group in fileGroups)
				{
					var relatileName = Uri.UnescapeDataString(
						directoryUri.MakeRelativeUri(new Uri(group.Key)).ToString()
					);

					foreach(var e in group)
					{
						e.FileName = relatileName;
					}
				}
			}

			using (StreamWriter fs = new StreamWriter(fileName, false, System.Text.Encoding.UTF8))
			{
				var unit = new SerializationUnit()
				{
					Markup = Markup,
					PointContexts = pointContexts
						.GroupBy(c => c.AncestorsContext)
						.Select(c => new AncestorsPointsPair { Ancestors = c.Key, Points = c.ToList() })
						.ToList(),
					LineContexts = GetConcernPoints()
						.Where(p => p.LineContext != null)
						.Select(p => p.LineContext)
						.ToList(),
					ExternalRelatons = Relations.ExternalRelations.GetRelatedPairs()
				};

				fs.Write(JsonConvert.SerializeObject(unit, Formatting.Indented, 
					new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
			}

			if (useRelativePaths)
			{
				foreach (var group in fileGroups)
				{
					var fullName = Path.GetFullPath(
						Path.Combine(Path.GetDirectoryName(fileName), group.Key)
					);

					foreach (var e in group)
					{
						e.FileName = fullName;
					}
				}
			}

			HasUnsavedChanges = false;
		}

		public void Deserialize(string fileName, Dictionary<string, Grammar> grammars)
		{
			Clear();

			using (StreamReader fs = new StreamReader(fileName))
			{
				var unit = JsonConvert.DeserializeObject<SerializationUnit>(fs.ReadToEnd(),
					new JsonSerializerSettings()
					{
						Converters = { new MarkupElementConverter() }
					});
				var pointContexts = new List<PointContext>();

				/// Фиксируем разметку
				foreach (var elem in unit.Markup)
				{
					Markup.Add(elem);
				}

				/// Восстанавливаем связи с предками
				foreach(var pair in unit.PointContexts)
				{
					pointContexts.AddRange(pair.Points);

					foreach (var context in pair.Points)
					{
						context.AncestorsContext = pair.Ancestors;
					}
				}

				/// Восстанавливаем обратные связи между потомками и предками,
				/// восстанавливаем связи с контекстами
				var concernPoints = GetConcernPoints().ToDictionary(e => e.Id, e => e);

                #region Relative paths to absolute paths

				foreach (var pc in pointContexts)
				{
					if (!Path.IsPathRooted(pc.FileName))
					{
						pc.FileName = Path.GetFullPath(
							Path.Combine(Path.GetDirectoryName(fileName), pc.FileName)
						);
					}
				}

                #endregion

				SetGrammarBasedProperties(pointContexts, grammars);

				/// Связываем контексты с точками привязки в разметке
				foreach (var context in pointContexts)
				{
					foreach(var id in context.LinkedPoints)
					{
						concernPoints[id].Context = context;
					}
				}

				/// Если у каких-то точек нет ближайших или соседей, присваиваем пустой массив
				foreach (var point in pointContexts)
				{
					/// Контекст ближайших есть только у невспомогательных точек привязки
					if (point.ClosestContext == null && point.LinkedPoints.Count > 0)
					{
						point.ClosestContext = new HashSet<PointContext>();
					}

					if(point.SiblingsContext != null)
					{
						if (point.SiblingsContext.Before != null)
						{
							point.SiblingsContext.Before.Nearest = new List<PointContext>();
						}

						if (point.SiblingsContext.After != null)
						{
							point.SiblingsContext.After.Nearest = new List<PointContext>();
						}
					}
				}

				/// Связываем контексты-описания соседей и ближайших с контекстами точек
				foreach (var context in pointContexts)
				{
					foreach (var id in context.LinkedClosestPoints)
					{
						concernPoints[id].Context.ClosestContext.Add(context);
					}

					foreach (var id in context.LinkedAfterNeighbours)
					{
						concernPoints[id].Context.SiblingsContext.Before.Nearest.Add(context);
					}

					foreach (var id in context.LinkedBeforeNeighbours)
					{
						concernPoints[id].Context.SiblingsContext.After.Nearest.Add(context);
					}
				}

				/// Для совместимости
				if (unit.LineContexts != null)
				{
					/// Связываем контексты строк с точками привязки
					foreach (var line in unit.LineContexts)
					{
						foreach (var id in line.LinkedPoints)
						{
							concernPoints[id].LineContext = line;
						}
					}
				}

				/// Восстанавливаем обратные связи между элементами разметки
				DoWithMarkup(e =>
				{
					if (e is Concern c)
					{
						foreach (var elem in c.Elements)
						{
							elem.Parent = c;
						}
					}
				});

				/// Запоминаем external-отношения между функциональностями
				Relations.RefreshElements(Markup);

				foreach (var pair in unit.ExternalRelatons)
					Relations.AddExternalRelation(pair.RelationType, pair.Item0, pair.Item1);
			}
		}

		private void SetGrammarBasedProperties(IEnumerable<PointContext> points, Dictionary<string, Grammar> grammars)
        {
			foreach (var group in points.GroupBy(p => Path.GetExtension(p.FileName)))
			{
				/// Все контексты заголовка, для которых нужно восстановить приоритеты и режимы сравнения
				var headerContexts = group
					.SelectMany(e => e.HeaderContext.Sequence)
					.Concat(group.SelectMany(p => p.AncestorsContext.SelectMany(a => a.HeaderContext.Sequence)))
					.ToList();
				/// Все типы, для которых нам нужна информация о ядрах
				var typesOfElementsWithHeaders = group
					.Select(e => e.Type)
					.Concat(group.SelectMany(p => p.AncestorsContext.Select(a => a.Type)))
					.ToList();
				/// Информация о приоритетах, установленных в секции опций
				var priorities = grammars[group.Key].Options.GetSymbols(MarkupOption.GROUP_NAME, MarkupOption.PRIORITY)
					.ToDictionary(e => e, e => (double)grammars[group.Key].Options.GetParams(MarkupOption.GROUP_NAME, MarkupOption.PRIORITY, e).First());
				/// Информация о ядрах заголовков
				var cores = grammars[group.Key].Options.GetSymbols(MarkupOption.GROUP_NAME, MarkupOption.HEADERCORE)
					.ToDictionary(e => e, e => new HashSet<string>(grammars[group.Key].Options.GetParams(MarkupOption.GROUP_NAME, MarkupOption.HEADERCORE, e).Select(el => (string)el)));

				/// Сгруппированная по типам элементов заголовка информация
				var typeBasedHeaderElementInfo = headerContexts
					.Select(e => e.Type)
					.Distinct()
					.ToDictionary(e => e, e =>
					{
						/// Проверяем, не является ли текущий тип алиасом чего-нибудь
						var aliasOf = grammars[group.Key].Aliases.Keys
							.FirstOrDefault(k => grammars[group.Key].Aliases[k].Contains(e));

						return new
						{
							ExactMatch = grammars[group.Key].Options.IsSet(MarkupOption.GROUP_NAME, MarkupOption.EXACTMATCH, e)
								|| !String.IsNullOrEmpty(aliasOf) && grammars[group.Key].Options.IsSet(MarkupOption.GROUP_NAME, MarkupOption.EXACTMATCH, aliasOf),
							Priority = priorities.ContainsKey(e) ? priorities[e]
								: !String.IsNullOrEmpty(aliasOf) && priorities.ContainsKey(aliasOf) ? priorities[aliasOf]
									: e == Grammar.ANY_TOKEN_NAME ? 0 : CoreExtension.OptionsExtension.DEFAULT_PRIORITY
						};
					});

				/// Проставляем нужные приоритеты и режимы сравнения
				foreach (var elem in headerContexts)
				{
					elem.ExactMatch = typeBasedHeaderElementInfo[elem.Type].ExactMatch;
					elem.Priority = typeBasedHeaderElementInfo[elem.Type].Priority;
				}

				/// Функция для определения ядер у заголовков
				Action<HeaderContext, string> setCore = (header, type) =>
				{
					/// Информация о ядре связана либо с данным типом, либо с типом, алиасом которого является данный
					var typeToCheck = cores.ContainsKey(type) ? type : null;

					if(typeToCheck == null)
					{
						var typeForAlias = grammars[group.Key].Aliases.FirstOrDefault(kvp => kvp.Value.Contains(type)).Key;
						typeToCheck = !string.IsNullOrEmpty(typeForAlias) && cores.ContainsKey(typeForAlias) ? typeForAlias : null;
					}

					var grouped = header.Sequence
						.Select((e, i) => new { elem = e, idx = i })
						.GroupBy(e => typeToCheck != null && cores[typeToCheck].Contains(e.elem.Type))
						.ToDictionary(g => g.Key, g => g.ToList());

					header.NonCoreIndices = grouped.ContainsKey(false)
						? grouped[false].Select(e => e.idx).ToList()
						: new List<int>();
					header.CoreIndices = grouped.ContainsKey(true)
						? grouped[true].Select(e => e.idx).ToList()
						: new List<int>();
				};

				foreach (var elem in group)
				{
					setCore(elem.HeaderContext, elem.Type);

					foreach (var ancestorElem in elem.AncestorsContext)
					{
						setCore(ancestorElem.HeaderContext, ancestorElem.Type);
					}
				}
			}
        }

		/// <summary>
		/// Поиск узла дерева, которому соответствует заданная точка привязки
		/// </summary>
		public List<RemapCandidateInfo> Find(ConcernPoint point, ParsedFile targetInfo)
		{
			return ContextFinder.FindPoints(
				new List<ConcernPoint> { point }, 
				new List<ParsedFile> { targetInfo }, 
				ContextFinder.SearchType.Local
			)[point];
		}

		/// <summary>
		/// Получение списка файлов, в которых есть точки привязки
		/// </summary>
		public HashSet<string> GetReferencedFiles()
		{
			return new HashSet<string>(
				GroupPointsByFileVisitor.GetGroups(Markup).Select(p => p.Key)
			);
		}

		#region Перепривязка

		/// <summary>
		/// Размер топа кандидатов, ранжированных по похожести, возвращаемого при неоднозначности
		/// </summary>
		public int AmbiguityTopCount { get; set; } = 10;

		/// <summary>
		/// Похожесть, ниже которой не рассматриваем элемент как кандидата
		/// </summary>
		public double GarbageThreshold { get; set; } = 0.4;

		/// <summary>
		/// Перепривязка всех точек разметки
		/// </summary>
		/// <param name="searchArea">
		/// Множество файлов проекта
		/// </param>
		/// <param name="allowAutoDecisions">
		/// Признак того, что разрешено проводить автоматическую перепривязку
		/// </param>
		/// <param name="searchType">
		/// Поиск точки проводится по тому же файлу или по всему проекту
		/// </param>
		/// <returns></returns>
		public Dictionary<ConcernPoint, List<RemapCandidateInfo>> Remap(
			List<ParsedFile> searchArea,
			bool allowAutoDecisions,
			ContextFinder.SearchType searchType)
		{
			var ambiguous = new Dictionary<ConcernPoint, List<RemapCandidateInfo>>();
			var points = GetConcernPoints();

			/// Локальный поиск имеет смысл проводить, если только его провести и нужно,
			/// или если нужен глобальный поиск, но разрешена автоперепривязка
			var result = allowAutoDecisions || searchType == ContextFinder.SearchType.Local
				? ContextFinder.FindPoints(points, searchArea, ContextFinder.SearchType.Local)
				: new Dictionary<ConcernPoint, List<RemapCandidateInfo>>();

			/// Если требуется глобальный поиск, 
			/// выполняем его для того, что не нашли локально, 
			/// потом мёржим результаты
			if(searchType == ContextFinder.SearchType.Global)
			{
				points = points.Except(result
					.Where(e => e.Value.FirstOrDefault()?.IsAuto ?? false)
					.Select(e => e.Key)
				).ToList();

				var globalResult = ContextFinder.FindPoints(points, searchArea, ContextFinder.SearchType.Global);

				foreach(var key in globalResult.Keys)
				{
					result[key] = globalResult[key];
				}
			}

			foreach (var kvp in result)
			{
				var candidates = kvp.Value
					//.TakeWhile(c=>c.Similarity >= GarbageThreshold)
					.Take(AmbiguityTopCount).ToList();

				if (!allowAutoDecisions || 
					!ApplyCandidate(kvp.Key, candidates))
					ambiguous[kvp.Key] = candidates;
			}

			OnMarkupChanged?.Invoke();

			return ambiguous;
		}

		/// <summary>
		/// Перепривязка всех точек заданного типа в заданном файле
		/// </summary>
		public Dictionary<ConcernPoint, List<RemapCandidateInfo>> Remap(
			string pointType,
			ParsedFile file,
			bool allowAutoDecisions)
		{
			if(file == null)
			{
				return new Dictionary<ConcernPoint, List<RemapCandidateInfo>>();
			}

			var points = GetConcernPoints()
				.Where(p => p.Context.Type == pointType
					&& p.Context.FileName == file.Name)
				.ToList();

			var ambiguous = new Dictionary<ConcernPoint, List<RemapCandidateInfo>>();

			var result = ContextFinder.FindPoints(
				points,
				new List<ParsedFile> { file }, 
				ContextFinder.SearchType.Local
			);

			foreach (var key in result.Keys.ToList())
			{
				result[key] = result[key]
					.TakeWhile(c => c.Similarity >= GarbageThreshold)
					.Take(AmbiguityTopCount)
					.ToList();

				if (!allowAutoDecisions || !ApplyCandidate(key, result[key]))
				{
					ambiguous[key] = result[key];
				}
			}

			OnMarkupChanged?.Invoke();

			return ambiguous;
		}

		private bool ApplyCandidate(
			ConcernPoint point, 
			IEnumerable<RemapCandidateInfo> candidates)
		{
			var first = candidates.FirstOrDefault();

			if (first?.IsAuto ?? false)
			{
				RelinkConcernPoint(point, first);
				return true;
			}
			else
			{
				point.NodeLocation = null;
				point.LineLocation = null;

				return false;
			}
		}

		#endregion

		/// <summary>
		/// Обобщённое добавление элемента разметки
		/// </summary>
		/// <param name="elem"></param>
		private void AddElement(MarkupElement elem)
		{
			if (elem.Parent == null)
				Markup.Add(elem);
			else
				elem.Parent.Elements.Add(elem);
		}

		/// <summary>
		/// Совершение заданного действия со всеми элементами разметки
		/// </summary>
		public void DoWithMarkup(Action<MarkupElement> action)
		{
			foreach (var elem in Markup)
				DoWithMarkupSubtree(action, elem);
		}

		/// <summary>
		/// Совершение заданного действия со всеми элементами поддерева разметки
		/// </summary>
		private void DoWithMarkupSubtree(Action<MarkupElement> action, MarkupElement root)
		{
			var elements = new Queue<MarkupElement>();
			elements.Enqueue(root);

			while (elements.Count > 0)
			{
				var elem = elements.Dequeue();

				if (elem is Concern concern)
					foreach (var child in concern.Elements)
						elements.Enqueue(child);

				action(elem);
			}
		}
	}
}
