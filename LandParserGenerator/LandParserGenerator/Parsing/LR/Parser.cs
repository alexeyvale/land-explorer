﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Land.Core.Lexing;
using Land.Core.Parsing.Tree;

namespace Land.Core.Parsing.LR
{
	public class Parser: BaseParser
	{
		private TableLR1 Table { get; set; }

		private ParsingStack Stack { get; set; }
		private TokenStream LexingStream { get; set; }

		public Parser(Grammar g, ILexer lexer, BaseNodeGenerator nodeGen = null) : base(g, lexer, nodeGen)
		{
			Table = new TableLR1(g);
		}

		protected override Node ParsingAlgorithm(string text, bool enableTracing)
		{
			Node root = null;	

			/// Готовим лексер
			LexingStream = new TokenStream(Lexer, text);
			/// Читаем первую лексему из входного потока
			var token = LexingStream.GetNextToken();
			/// Создаём стек
			Stack = new ParsingStack();
			Stack.Push(0);

			while (true)
			{
				if (token.Name == Grammar.ERROR_TOKEN_NAME)
					break;

				var currentState = Stack.PeekState();

				if(enableTracing && token.Name != Grammar.ERROR_TOKEN_NAME && token.Name != Grammar.ANY_TOKEN_NAME)
					Log.Add(Message.Trace(
						$"Текущий токен: {this.GetTokenInfoForMessage(token)} | Стек: {Stack.ToString(GrammarObject)}",
						token.Location.Start
					));

				/// Знаем, что предпринять, если действие однозначно
				/// или существует выбор между shift и reduce (тогда выбираем shift)
				if (Table[currentState, token.Name].Count == 1
					|| Table[currentState, token.Name].Count == 2 && Table[currentState, token.Name].Any(a => a is ShiftAction))
				{
					if (token.Name == Grammar.ANY_TOKEN_NAME)
					{
						token = SkipAny(NodeGenerator.Generate(Grammar.ANY_TOKEN_NAME));

						/// Если при пропуске текста произошла ошибка, прерываем разбор
						if (token.Name == Grammar.ERROR_TOKEN_NAME)
							break;
						else
							continue;
					}

					var action = GetAction(currentState, token.Name);

					/// Если нужно произвести перенос
					if (action is ShiftAction)
					{
						var tokenNode = NodeGenerator.Generate(token.Name);
						tokenNode.SetValue(token.Text);
						tokenNode.SetAnchor(token.Location.Start, token.Location.End);

						var shift = (ShiftAction)action;
						/// Вносим в стек новое состояние
						Stack.Push(tokenNode, shift.TargetItemIndex);

						if (enableTracing)
						{
							Log.Add(Message.Trace(
								$"Перенос",
								token.Location.Start
							));
						}

						token = LexingStream.GetNextToken();
					}
					/// Если нужно произвести свёртку
					else if (action is ReduceAction reduce)
					{
						var parentNode = NodeGenerator.Generate(reduce.ReductionAlternative.NonterminalSymbolName);

						/// Снимаем со стека символы ветки, по которой нужно произвести свёртку
						for (var i = 0; i < reduce.ReductionAlternative.Count; ++i)
						{
							parentNode.AddFirstChild(Stack.PeekSymbol());
							Stack.Pop();
						}
						currentState = Stack.PeekState();

						/// Кладём на стек состояние, в которое нужно произвести переход
						Stack.Push(
							parentNode,
							Table.Transitions[currentState][reduce.ReductionAlternative.NonterminalSymbolName]
						);

						if (enableTracing)
						{
							Log.Add(Message.Trace(
								$"Свёртка по правилу {GrammarObject.Userify(reduce.ReductionAlternative)} -> {GrammarObject.Userify(reduce.ReductionAlternative.NonterminalSymbolName)}",
								token.Location.Start
							));
						}

						continue;
					}
					else if (action is AcceptAction)
					{
						root = Stack.PeekSymbol();
						break;
					}
				}
				else if (token.Name == Grammar.ANY_TOKEN_NAME)
				{
					Log.Add(Message.Warning(
						$"Неожиданный символ {this.GetTokenInfoForMessage(LexingStream.CurrentToken)} для состояния{Environment.NewLine}\t\t" + Table.ToString(Stack.PeekState(), null, "\t\t"),
						LexingStream.CurrentToken.Location.Start
					));

					token = ErrorRecovery();
				}
				else
				{
					/// Если встретился неожиданный токен, но он в списке пропускаемых
					if (GrammarObject.Options.IsSet(ParsingOption.SKIP, token.Name))
					{
						token = LexingStream.GetNextToken();
					}
					else
					{
						if (enableTracing)
						{
							Log.Add(Message.Trace(
								$"Попытка подобрать токены как Any для состояния {Environment.NewLine}\t\t" + Table.ToString(Stack.PeekState(), null, "\t\t"),
								token.Location.Start
							));
						}

						token = Lexer.CreateToken(Grammar.ANY_TOKEN_NAME);
					}
				}
			}

			if(root != null)
				TreePostProcessing(root);

			return root;
		}

		private Action GetAction(int currentState, string token)
		{
			if (Table[currentState, token].Count == 0)
				return null;

			return Table[currentState, token].Count == 1
				? Table[currentState, token].Single()
				: Table[currentState, token].Single(a => a is ShiftAction);
		}

		private IToken SkipAny(Node anyNode)
		{
			var stackActions = new LinkedList<Tuple<Node, int?>>();

			var token = LexingStream.CurrentToken;
			var tokenIndex = LexingStream.CurrentIndex;

			var rawActions = Table[Stack.PeekState(), Grammar.ANY_TOKEN_NAME].ToList();

			/// Пока по Any нужно производить свёртки (ячейка таблицы непуста и нет конфликтов)
			while (rawActions.Count == 1 && rawActions[0] is ReduceAction)
			{
				var reduce = (ReduceAction)rawActions[0];
				var parentNode = NodeGenerator.Generate(reduce.ReductionAlternative.NonterminalSymbolName);

				/// Снимаем со стека символы ветки, по которой нужно произвести свёртку
				for (var i = 0; i < reduce.ReductionAlternative.Count; ++i)
				{
					stackActions.AddFirst(new Tuple<Node, int?>(
						Stack.PeekSymbol(),
						Stack.PeekState()
					));

					parentNode.AddFirstChild(Stack.PeekSymbol());
					Stack.Pop();
				}

				/// Кладём на стек состояние, в которое нужно произвести переход
				Stack.Push(
					parentNode,
					Table.Transitions[Stack.PeekState()][reduce.ReductionAlternative.NonterminalSymbolName]
				);
				stackActions.AddFirst(new Tuple<Node, int?>(null, null));

				rawActions = Table[Stack.PeekState(), Grammar.ANY_TOKEN_NAME].ToList();
			}

			/// Берём опции из нужного вхождения Any
			var marker = Table.Items[Stack.PeekState()].First(i => i.Next == Grammar.ANY_TOKEN_NAME);
			anyNode.Options = marker.Alternative[marker.Position].Options;

			/// Производим перенос
			var shift = (ShiftAction)rawActions.Where(a => a is ShiftAction).Single();
			/// Вносим в стек новое состояние
			Stack.Push(anyNode, shift.TargetItemIndex);
			stackActions.AddFirst(new Tuple<Node, int?>(null, null));

			var stopTokens = anyNode.Options.AnyOptions.ContainsKey(AnyOption.Except)
				? anyNode.Options.AnyOptions[AnyOption.Except]
				: Table.GetExpectedTokens(Stack.PeekState()).Except(
					anyNode.Options.AnyOptions.ContainsKey(AnyOption.Include) 
						? anyNode.Options.AnyOptions[AnyOption.Include] : new HashSet<string>()
				);

			var startLocation = anyNode.Anchor?.Start 
				?? token.Location.Start;
			var endLocation = anyNode.Anchor?.End;

			/// Пропускаем токены, пока не найдём тот, для которого
			/// в текущем состоянии нужно выполнить перенос или свёртку
			while (!stopTokens.Contains(token.Name)
				&& !anyNode.Options.Contains(AnyOption.Avoid, token.Name)
				&& token.Name != Grammar.EOF_TOKEN_NAME)
			{
				anyNode.Value.Add(token.Text);
				endLocation = token.Location.End;

				token = LexingStream.GetNextToken();
			}

			if(endLocation != null)
				anyNode.SetAnchor(startLocation, endLocation);

			/// Если дошли до конца входной строки, и это было не по плану
			if (!stopTokens.Contains(token.Name))
			{
				var message = Message.Trace(
					$"Ошибка при пропуске {Grammar.ANY_TOKEN_NAME}: неожиданный токен {GrammarObject.Userify(token.Name)}, ожидался один из токенов {String.Join(", ", stopTokens.Select(t => GrammarObject.Userify(t)))}",
					token.Location.Start
				);

				if (GrammarObject.Options.IsSet(ParsingOption.RECOVERY))
				{
					message.Type = MessageType.Warning;
					Log.Add(message);

					LexingStream.MoveTo(tokenIndex);

					/// Приводим стек в состояние, 
					/// в котором он был до захода в этот метод
					foreach(var action in stackActions)
					{
						if (action.Item1 == null)
							Stack.Pop();
						else
							Stack.Push(action.Item1, action.Item2);
					}

					return ErrorRecovery();
				}
				else
				{
					message.Type = MessageType.Error;
					Log.Add(message);
					return Lexer.CreateToken(Grammar.ERROR_TOKEN_NAME);
				}
			}

			return token;
		}

		private IToken SkipAnyInRecovery(Node anyNode)
		{
			/// Берём опции из нужного вхождения Any
			var marker = Table.Items[Stack.PeekState()].First(i => i.Next == Grammar.ANY_TOKEN_NAME);
			anyNode.Options = marker.Alternative[marker.Position].Options;
			/// Производим перенос Any
			var shift = (ShiftAction)Table[Stack.PeekState(), Grammar.ANY_TOKEN_NAME]
				.Where(a => a is ShiftAction).Single();
			/// Вносим в стек новое состояние
			Stack.Push(anyNode, shift.TargetItemIndex);

			var stopTokens = anyNode.Options.AnyOptions.ContainsKey(AnyOption.Except)
				? anyNode.Options.AnyOptions[AnyOption.Except]
				: Table.GetExpectedTokens(Stack.PeekState()).Except(
					anyNode.Options.AnyOptions.ContainsKey(AnyOption.Include)
						? anyNode.Options.AnyOptions[AnyOption.Include] : new HashSet<string>()
				);

			var token = LexingStream.CurrentToken;
			var startLocation = anyNode.Anchor?.Start
				?? token.Location.Start;
			var endLocation = anyNode.Anchor?.End;

			/// Пропускаем токены, пока не найдём тот, для которого
			/// в текущем состоянии нужно выполнить перенос или свёртку
			while (!stopTokens.Contains(token.Name)
				&& !anyNode.Options.Contains(AnyOption.Avoid, token.Name)
				&& token.Name != Grammar.EOF_TOKEN_NAME)
			{
				anyNode.Value.Add(token.Text);
				endLocation = token.Location.End;

				token = LexingStream.GetNextToken();
			}

			if (endLocation != null)
				anyNode.SetAnchor(startLocation, endLocation);

			if (!stopTokens.Contains(token.Name))
				{
				Log.Add(Message.Error(
					$"Ошибка при восстановлении: неожиданный токен {GrammarObject.Userify(token.Name)}, ожидался один из токенов {String.Join(", ", stopTokens.Select(t=>GrammarObject.Userify(t)))}",
					token.Location.Start
				));

				return Lexer.CreateToken(Grammar.ERROR_TOKEN_NAME);
			}

			return token;
		}

		private IToken ErrorRecovery()
		{
			if (!GrammarObject.Options.IsSet(ParsingOption.RECOVERY))
			{
				Log.Add(Message.Error(
					$"Возобновление разбора в случае ошибки отключено",
					LexingStream.CurrentToken.Location.Start
				));

				return null;
			}

			PointLocation startLocation = null;
			PointLocation endLocation = null;
			IEnumerable<string> value = new List<string>();

			/// Снимаем со стека состояния до тех пор, пока не находим состояние,
			/// в котором есть пункт A -> * Any ...
			do
			{
				if (Stack.CountSymbols > 0)
				{
					if (Stack.PeekSymbol().Anchor != null)
					{
						startLocation = Stack.PeekSymbol().Anchor.Start;
						if (endLocation == null)
						{
							endLocation = Stack.PeekSymbol().Anchor.End;
						}
					}

					value = Stack.PeekSymbol().GetValue().Concat(value);
				}

				Stack.Pop();
			}
			/// Снимаем состояния, пока не встретим небазисный пункт с альтернативой, начинающейся с Any;
			/// в этом же множестве будет и базисный пункт с указателем перед нетерминалом, на котором возможно восстановление
			while (Stack.CountStates > 0 && !Table.Items[Stack.PeekState()].Any(m => m.Position == 0
				&& m.Next == Grammar.ANY_TOKEN_NAME));

			if (Stack.CountStates > 0)
			{
				/// Пытаемся пропустить Any в этом месте,
				/// Any захватывает участок с начала последнего 
				/// снятого со стека символа до места восстановления
				var anyNode = NodeGenerator.Generate(Grammar.ANY_TOKEN_NAME);
				if(startLocation != null)
					anyNode.SetAnchor(startLocation, startLocation);
				anyNode.Value = value.ToList();

				var token = SkipAnyInRecovery(anyNode);

				/// Если Any успешно пропустили и возобновили разбор,
				/// возвращаем токен, с которого разбор продолжается

				if (token.Name != Grammar.ERROR_TOKEN_NAME)
				{
					Statistics.RecoveryTimes += 1;
					return token;
				}
			}

			Log.Add(Message.Error(
				$"Не удалось продолжить разбор",
				null
			));

			return Lexer.CreateToken(Grammar.ERROR_TOKEN_NAME);
		}
	}
}
