﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LandParserGenerator.Lexing;
using LandParserGenerator.Parsing.Tree;

namespace LandParserGenerator.Parsing.LR
{
	public class Parser: BaseParser
	{
		private TableLR1 Table { get; set; }

		private ParsingStack Stack { get; set; }
		private TokenStream LexingStream { get; set; }

		public Parser(Grammar g, ILexer lexer): base(g, lexer)
		{
			Table = new TableLR1(g);
		}

		public override Node Parse(string text)
		{
			Log = new List<Message>();
			Statistics = new Statistics();

			Node root = null;	

			/// Готовим лексер
			LexingStream = new TokenStream(Lexer, text);
			/// Читаем первую лексему из входного потока
			var token = LexingStream.NextToken();
			/// Создаём стек
			Stack = new ParsingStack(LexingStream);
			Stack.Push(0);

			while (true)
			{
				var currentState = Stack.PeekState();

				if(token.Name != Grammar.ERROR_TOKEN_NAME && token.Name != Grammar.ANY_TOKEN_NAME)
					Log.Add(Message.Trace(
						$"Текущий токен: {GetTokenInfoForMessage(token)} | Стек: {Stack.ToString(grammar)}",
						token.Line,
						token.Column
					));

				/// Знаем, что предпринять, если действие однозначно
				/// или существует выбор между shift и reduce (тогда выбираем shift)
				if (Table[currentState, token.Name].Count == 1
					|| Table[currentState, token.Name].Count == 2 && Table[currentState, token.Name].Any(a => a is ShiftAction))
				{
					if (token.Name == Grammar.ANY_TOKEN_NAME)
					{
						token = SkipAny(Stack.PeekSymbol());

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
						var tokenNode = new Node(token.Name);
						tokenNode.SetAnchor(token.StartOffset, token.EndOffset);

						var shift = (ShiftAction)action;
						/// Вносим в стек новое состояние
						Stack.Push(tokenNode, shift.TargetItemIndex);

						Log.Add(Message.Trace(
							$"Перенос",
							token.Line,
							token.Column
						));

						token = LexingStream.NextToken();
					}
					/// Если нужно произвести свёртку
					else if (action is ReduceAction)
					{
						var reduce = (ReduceAction)action;
						var parentNode = new Node(reduce.ReductionAlternative.NonterminalSymbolName);

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

						Log.Add(Message.Trace(
							$"Свёртка по правилу {grammar.Userify(reduce.ReductionAlternative)} -> {grammar.Userify(reduce.ReductionAlternative.NonterminalSymbolName)}",
							token.Line,
							token.Column
						));

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
					var errorToken = LexingStream.CurrentToken();

					Log.Add(Message.Error(
						$"Неожиданный символ {GetTokenInfoForMessage(errorToken)} для состояния{Environment.NewLine}\t\t" + Table.ToString(Stack.PeekState(), null, "\t\t"),
						errorToken.Line,
						errorToken.Column
					));

					break;
				}
				else
				{
					/// Если встретился неожиданный токен, но он в списке пропускаемых
					if (grammar.Options.IsSet(ParsingOption.SKIP, token.Name))
					{
						token = LexingStream.NextToken();
					}
					else
					{
						Log.Add(Message.Trace(
							$"Попытка подобрать токены как Any для состояния {Environment.NewLine}\t\t" + Table.ToString(Stack.PeekState(), null, "\t\t"),
							token.Line,
							token.Column
						));

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
			var token = LexingStream.CurrentToken();
			var rawAction = Table[Stack.PeekState(), Grammar.ANY_TOKEN_NAME].Single();

			/// Пока по Any нужно производить свёртки
			while (rawAction is ReduceAction)
			{
				var action = (ReduceAction)rawAction;
				var parentNode = new Node(action.ReductionAlternative.NonterminalSymbolName);

				/// Снимаем со стека символы ветки, по которой нужно произвести свёртку
				for (var i = 0; i < action.ReductionAlternative.Count; ++i)
				{
					parentNode.AddFirstChild(Stack.PeekSymbol());
					Stack.Pop();
				}
				var currentState = Stack.PeekState();

				/// Кладём на стек состояние, в которое нужно произвести переход
				Stack.Push(
					parentNode,
					Table.Transitions[currentState][action.ReductionAlternative.NonterminalSymbolName]
				);

				rawAction = Table[Stack.PeekState(), Grammar.ANY_TOKEN_NAME].FirstOrDefault();
			}

			/// Если нужно произвести перенос
			if (rawAction is ShiftAction)
			{
				var action = (ShiftAction)rawAction;
				/// Вносим в стек новое состояние
				Stack.Push(anyNode, action.TargetItemIndex);
			}

			int startOffset = token.StartOffset;
			int endOffset = token.EndOffset;

			/// Пропускаем токены, пока не найдём тот, для которого
			/// в текущем состоянии нужно выполнить перенос или свёртку
			while (Table[Stack.PeekState(), token.Name].Count == 0
				&& token.Name != Grammar.EOF_TOKEN_NAME)
			{
				endOffset = token.EndOffset;
				token = LexingStream.NextToken();
			}

			anyNode.SetAnchor(startOffset, endOffset);

			/// Если дошли до конца входной строки, и это было не по плану
			if (token.Name == Grammar.EOF_TOKEN_NAME
				&& Table[Stack.PeekState(), token.Name].Count == 0)
			{
				Log.Add(Message.Error(
					$"Ошибка при пропуске токенов: неожиданный конец файла",
					null
				));

				return Lexer.CreateToken(Grammar.ERROR_TOKEN_NAME);
			}

			return token;
		}
	}
}
