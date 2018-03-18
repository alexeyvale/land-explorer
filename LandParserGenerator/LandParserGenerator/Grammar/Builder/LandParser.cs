// This code was generated by the Gardens Point Parser Generator
// Copyright (c) Wayne Kelly, John Gough, QUT 2005-2014
// (see accompanying GPPGcopyright.rtf)

// GPPG version 1.5.2
// Machine:  DESKTOP-QMIGNCH
// DateTime: 18.03.2018 20:11:18
// UserName: Алексей
// Input file <./Land.y - 18.03.2018 20:11:18>

// options: no-lines gplex

using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Text;
using QUT.Gppg;
using System.Linq;
using LandParserGenerator;

namespace LandParserGenerator.Builder
{
public enum Tokens {error=2,EOF=3,OR=4,COLON=5,OPT_LPAR=6,
    ELEM_LPAR=7,LPAR=8,RPAR=9,COMMA=10,PROC=11,EQUALS=12,
    MINUS=13,PLUS=14,EXCLAMATION=15,ADD_CHILD=16,DOT=17,REGEX=18,
    NAMED=19,STRING=20,ID=21,ENTITY_NAME=22,OPTION_NAME=23,CATEGORY_NAME=24,
    POSITION=25,RNUM=26,OPTIONAL=27,ZERO_OR_MORE=28,ONE_OR_MORE=29,IS_LIST_NODE=30,
    PREC_NONEMPTY=31};

public struct ValueType
{ 
	public int intVal; 
	public double doubleVal;
	public Quantifier quantVal;
	public bool boolVal;
	public string strVal;
	public Entry entryVal;
	
	public Tuple<string, double> strDoublePair;
	
	public List<dynamic> dynamicList;
	public List<Tuple<string, List<dynamic>>> optionParamsList;
	public List<string> strList;	
	public List<Alternative> altList;
	
	// Информация о количестве повторений
	public Nullable<Quantifier> optQuantVal;
	public Nullable<double> optDoubleVal;
}
// Abstract base class for GPLEX scanners
[GeneratedCodeAttribute( "Gardens Point Parser Generator", "1.5.2")]
public abstract class ScanBase : AbstractScanner<ValueType,LexLocation> {
  private LexLocation __yylloc = new LexLocation();
  public override LexLocation yylloc { get { return __yylloc; } set { __yylloc = value; } }
  protected virtual bool yywrap() { return true; }
}

// Utility class for encapsulating token information
[GeneratedCodeAttribute( "Gardens Point Parser Generator", "1.5.2")]
public class ScanObj {
  public int token;
  public ValueType yylval;
  public LexLocation yylloc;
  public ScanObj( int t, ValueType val, LexLocation loc ) {
    this.token = t; this.yylval = val; this.yylloc = loc;
  }
}

[GeneratedCodeAttribute( "Gardens Point Parser Generator", "1.5.2")]
public class Parser: ShiftReduceParser<ValueType, LexLocation>
{
  // Verbatim content from ./Land.y - 18.03.2018 20:11:18
    public Parser(AbstractScanner<LandParserGenerator.Builder.ValueType, LexLocation> scanner) : base(scanner) { }
    
    public Grammar ConstructedGrammar;
    public List<Message> Errors = new List<Message>();
  // End verbatim content from ./Land.y - 18.03.2018 20:11:18

#pragma warning disable 649
  private static Dictionary<int, string> aliases;
#pragma warning restore 649
  private static Rule[] rules = new Rule[43];
  private static State[] states = new State[57];
  private static string[] nonTerms = new string[] {
      "lp_description", "quantifier", "body_element_core", "body_element_atom", 
      "group", "body_element", "identifiers", "body", "prec_nonempty", "opt_args", 
      "args", "context_opt_args", "body_element_args", "context_options", "$accept", 
      "structure", "options", "element", "terminal", "nonterminal", "option", 
      };

  static Parser() {
    states[0] = new State(new int[]{22,24},new int[]{-1,1,-16,3,-18,56,-19,23,-20,55});
    states[1] = new State(new int[]{3,2});
    states[2] = new State(-1);
    states[3] = new State(new int[]{11,4,22,24},new int[]{-18,22,-19,23,-20,55});
    states[4] = new State(-30,new int[]{-17,5});
    states[5] = new State(new int[]{24,7,3,-2},new int[]{-21,6});
    states[6] = new State(-31);
    states[7] = new State(new int[]{21,8});
    states[8] = new State(new int[]{8,12,21,-34,24,-34,3,-34},new int[]{-10,9});
    states[9] = new State(-42,new int[]{-7,10});
    states[10] = new State(new int[]{21,11,24,-32,3,-32});
    states[11] = new State(-41);
    states[12] = new State(new int[]{26,19,20,20,21,21},new int[]{-11,13});
    states[13] = new State(new int[]{9,14,10,15});
    states[14] = new State(-33);
    states[15] = new State(new int[]{26,16,20,17,21,18});
    states[16] = new State(-35);
    states[17] = new State(-36);
    states[18] = new State(-37);
    states[19] = new State(-38);
    states[20] = new State(-39);
    states[21] = new State(-40);
    states[22] = new State(-3);
    states[23] = new State(-5);
    states[24] = new State(new int[]{5,25,12,27});
    states[25] = new State(new int[]{18,26});
    states[26] = new State(-7);
    states[27] = new State(-11,new int[]{-8,28});
    states[28] = new State(new int[]{4,30,11,-8,22,-8,23,-16,20,-16,21,-16,8,-16},new int[]{-6,29,-14,31});
    states[29] = new State(-9);
    states[30] = new State(-10);
    states[31] = new State(new int[]{23,43,20,49,21,50,8,52},new int[]{-3,32,-4,48,-5,51});
    states[32] = new State(new int[]{7,40,27,-14,28,-14,29,-14,31,-14,4,-14,23,-14,20,-14,21,-14,8,-14,11,-14,22,-14,9,-14},new int[]{-13,33});
    states[33] = new State(new int[]{27,37,28,38,29,39,31,-24,4,-24,23,-24,20,-24,21,-24,8,-24,11,-24,22,-24,9,-24},new int[]{-2,34});
    states[34] = new State(new int[]{31,36,4,-20,23,-20,20,-20,21,-20,8,-20,11,-20,22,-20,9,-20},new int[]{-9,35});
    states[35] = new State(-12);
    states[36] = new State(-19);
    states[37] = new State(-21);
    states[38] = new State(-22);
    states[39] = new State(-23);
    states[40] = new State(new int[]{26,19,20,20,21,21},new int[]{-11,41});
    states[41] = new State(new int[]{9,42,10,15});
    states[42] = new State(-13);
    states[43] = new State(new int[]{6,45,23,-18,20,-18,21,-18,8,-18},new int[]{-12,44});
    states[44] = new State(-15);
    states[45] = new State(new int[]{26,19,20,20,21,21},new int[]{-11,46});
    states[46] = new State(new int[]{9,47,10,15});
    states[47] = new State(-17);
    states[48] = new State(-25);
    states[49] = new State(-27);
    states[50] = new State(-28);
    states[51] = new State(-26);
    states[52] = new State(-11,new int[]{-8,53});
    states[53] = new State(new int[]{9,54,4,30,23,-16,20,-16,21,-16,8,-16},new int[]{-6,29,-14,31});
    states[54] = new State(-29);
    states[55] = new State(-6);
    states[56] = new State(-4);

    for (int sNo = 0; sNo < states.Length; sNo++) states[sNo].number = sNo;

    rules[1] = new Rule(-15, new int[]{-1,3});
    rules[2] = new Rule(-1, new int[]{-16,11,-17});
    rules[3] = new Rule(-16, new int[]{-16,-18});
    rules[4] = new Rule(-16, new int[]{-18});
    rules[5] = new Rule(-18, new int[]{-19});
    rules[6] = new Rule(-18, new int[]{-20});
    rules[7] = new Rule(-19, new int[]{22,5,18});
    rules[8] = new Rule(-20, new int[]{22,12,-8});
    rules[9] = new Rule(-8, new int[]{-8,-6});
    rules[10] = new Rule(-8, new int[]{-8,4});
    rules[11] = new Rule(-8, new int[]{});
    rules[12] = new Rule(-6, new int[]{-14,-3,-13,-2,-9});
    rules[13] = new Rule(-13, new int[]{7,-11,9});
    rules[14] = new Rule(-13, new int[]{});
    rules[15] = new Rule(-14, new int[]{-14,23,-12});
    rules[16] = new Rule(-14, new int[]{});
    rules[17] = new Rule(-12, new int[]{6,-11,9});
    rules[18] = new Rule(-12, new int[]{});
    rules[19] = new Rule(-9, new int[]{31});
    rules[20] = new Rule(-9, new int[]{});
    rules[21] = new Rule(-2, new int[]{27});
    rules[22] = new Rule(-2, new int[]{28});
    rules[23] = new Rule(-2, new int[]{29});
    rules[24] = new Rule(-2, new int[]{});
    rules[25] = new Rule(-3, new int[]{-4});
    rules[26] = new Rule(-3, new int[]{-5});
    rules[27] = new Rule(-4, new int[]{20});
    rules[28] = new Rule(-4, new int[]{21});
    rules[29] = new Rule(-5, new int[]{8,-8,9});
    rules[30] = new Rule(-17, new int[]{});
    rules[31] = new Rule(-17, new int[]{-17,-21});
    rules[32] = new Rule(-21, new int[]{24,21,-10,-7});
    rules[33] = new Rule(-10, new int[]{8,-11,9});
    rules[34] = new Rule(-10, new int[]{});
    rules[35] = new Rule(-11, new int[]{-11,10,26});
    rules[36] = new Rule(-11, new int[]{-11,10,20});
    rules[37] = new Rule(-11, new int[]{-11,10,21});
    rules[38] = new Rule(-11, new int[]{26});
    rules[39] = new Rule(-11, new int[]{20});
    rules[40] = new Rule(-11, new int[]{21});
    rules[41] = new Rule(-7, new int[]{-7,21});
    rules[42] = new Rule(-7, new int[]{});
  }

  protected override void Initialize() {
    this.InitSpecialTokens((int)Tokens.error, (int)Tokens.EOF);
    this.InitStates(states);
    this.InitRules(rules);
    this.InitNonTerminals(nonTerms);
  }

  protected override void DoAction(int action)
  {
#pragma warning disable 162, 1522
    switch (action)
    {
      case 2: // lp_description -> structure, PROC, options
{ 
			ConstructedGrammar.PostProcessing();
			Errors.AddRange(ConstructedGrammar.CheckValidity()); 
		}
        break;
      case 7: // terminal -> ENTITY_NAME, COLON, REGEX
{ 
			SafeGrammarAction(() => { 
				ConstructedGrammar.DeclareTerminal(ValueStack[ValueStack.Depth-3].strVal, ValueStack[ValueStack.Depth-1].strVal);
				ConstructedGrammar.AddAnchor(ValueStack[ValueStack.Depth-3].strVal, LocationStack[LocationStack.Depth-3]);
			}, LocationStack[LocationStack.Depth-3]);
		}
        break;
      case 8: // nonterminal -> ENTITY_NAME, EQUALS, body
{ 
			SafeGrammarAction(() => { 
				ConstructedGrammar.DeclareNonterminal(ValueStack[ValueStack.Depth-3].strVal, ValueStack[ValueStack.Depth-1].altList);
				ConstructedGrammar.AddAnchor(ValueStack[ValueStack.Depth-3].strVal, LocationStack[LocationStack.Depth-3]);
			}, LocationStack[LocationStack.Depth-3]);
		}
        break;
      case 9: // body -> body, body_element
{ 
			CurrentSemanticValue.altList = ValueStack[ValueStack.Depth-2].altList; 
			CurrentSemanticValue.altList[CurrentSemanticValue.altList.Count-1].Add(ValueStack[ValueStack.Depth-1].entryVal); 	
		}
        break;
      case 10: // body -> body, OR
{ 
			CurrentSemanticValue.altList = ValueStack[ValueStack.Depth-2].altList;
			CurrentSemanticValue.altList.Add(new Alternative());		
		}
        break;
      case 11: // body -> /* empty */
{ 
			CurrentSemanticValue.altList = new List<Alternative>(); 
			CurrentSemanticValue.altList.Add(new Alternative()); 
		}
        break;
      case 12: // body_element -> context_options, body_element_core, body_element_args, 
               //                 quantifier, prec_nonempty
{ 		
			var opts = new LocalOptions();
			
			foreach(var opt in ValueStack[ValueStack.Depth-5].optionParamsList)
			{
				NodeOption nodeOpt;		
				if(!Enum.TryParse<NodeOption>(opt.Item1.ToUpper(), out nodeOpt))
				{
					MappingOption mapOpt;
					if(!Enum.TryParse<MappingOption>(opt.Item1.ToUpper(), out mapOpt))
					{
						Errors.Add(Message.Error(
							"Неизвестная опция '" + opt.Item1 + "'",
							LocationStack[LocationStack.Depth-5].StartLine, LocationStack[LocationStack.Depth-5].StartColumn,
							"LanD"
						));
					}
					else
						opts.Set(mapOpt, opt.Item2.ToArray());				
				}
				else
					opts.Set(nodeOpt);	
			}
			
			if(ValueStack[ValueStack.Depth-2].optQuantVal.HasValue)
			{
				var generated = ConstructedGrammar.GenerateNonterminal(ValueStack[ValueStack.Depth-4].strVal, ValueStack[ValueStack.Depth-2].optQuantVal.Value, ValueStack[ValueStack.Depth-1].boolVal);
				ConstructedGrammar.AddAnchor(generated, CurrentLocationSpan);
				
				CurrentSemanticValue.entryVal = new Entry(generated, opts);
			}
			else
			{
				if(ValueStack[ValueStack.Depth-4].strVal == Grammar.TEXT_TOKEN_NAME && ValueStack[ValueStack.Depth-3].dynamicList.Count > 0)
				{
					opts.AnySyncTokens = new HashSet<string>(ValueStack[ValueStack.Depth-3].dynamicList.Select(e=>(string)e));
				}
				
				CurrentSemanticValue.entryVal = new Entry(ValueStack[ValueStack.Depth-4].strVal, opts);
			}
		}
        break;
      case 13: // body_element_args -> ELEM_LPAR, args, RPAR
{ CurrentSemanticValue.dynamicList = ValueStack[ValueStack.Depth-2].dynamicList; }
        break;
      case 14: // body_element_args -> /* empty */
{ CurrentSemanticValue.dynamicList = new List<dynamic>(); }
        break;
      case 15: // context_options -> context_options, OPTION_NAME, context_opt_args
{ 
			CurrentSemanticValue.optionParamsList = ValueStack[ValueStack.Depth-3].optionParamsList; 
			CurrentSemanticValue.optionParamsList.Add(new Tuple<string, List<dynamic>>(ValueStack[ValueStack.Depth-2].strVal, ValueStack[ValueStack.Depth-1].dynamicList)); 
		}
        break;
      case 16: // context_options -> /* empty */
{ CurrentSemanticValue.optionParamsList = new List<Tuple<string, List<dynamic>>>(); }
        break;
      case 17: // context_opt_args -> OPT_LPAR, args, RPAR
{ CurrentSemanticValue.dynamicList = ValueStack[ValueStack.Depth-2].dynamicList; }
        break;
      case 18: // context_opt_args -> /* empty */
{ CurrentSemanticValue.dynamicList = new List<dynamic>(); }
        break;
      case 19: // prec_nonempty -> PREC_NONEMPTY
{ CurrentSemanticValue.boolVal = true; }
        break;
      case 20: // prec_nonempty -> /* empty */
{ CurrentSemanticValue.boolVal = false; }
        break;
      case 21: // quantifier -> OPTIONAL
{ CurrentSemanticValue.optQuantVal = ValueStack[ValueStack.Depth-1].quantVal; }
        break;
      case 22: // quantifier -> ZERO_OR_MORE
{ CurrentSemanticValue.optQuantVal = ValueStack[ValueStack.Depth-1].quantVal; }
        break;
      case 23: // quantifier -> ONE_OR_MORE
{ CurrentSemanticValue.optQuantVal = ValueStack[ValueStack.Depth-1].quantVal; }
        break;
      case 24: // quantifier -> /* empty */
{ CurrentSemanticValue.optQuantVal = null; }
        break;
      case 25: // body_element_core -> body_element_atom
{ CurrentSemanticValue.strVal = ValueStack[ValueStack.Depth-1].strVal; }
        break;
      case 26: // body_element_core -> group
{ CurrentSemanticValue.strVal = ValueStack[ValueStack.Depth-1].strVal; }
        break;
      case 27: // body_element_atom -> STRING
{ 
			CurrentSemanticValue.strVal = ConstructedGrammar.GenerateTerminal(ValueStack[ValueStack.Depth-1].strVal);
			ConstructedGrammar.AddAnchor(CurrentSemanticValue.strVal, CurrentLocationSpan);
		}
        break;
      case 28: // body_element_atom -> ID
{ CurrentSemanticValue.strVal = ValueStack[ValueStack.Depth-1].strVal; }
        break;
      case 29: // group -> LPAR, body, RPAR
{ 
			CurrentSemanticValue.strVal = ConstructedGrammar.GenerateNonterminal(ValueStack[ValueStack.Depth-2].altList);
			ConstructedGrammar.AddAnchor(CurrentSemanticValue.strVal, CurrentLocationSpan);
		}
        break;
      case 32: // option -> CATEGORY_NAME, ID, opt_args, identifiers
{
			OptionCategory optCategory;
			if(!Enum.TryParse(ValueStack[ValueStack.Depth-4].strVal.ToUpper(), out optCategory))
			{
				Errors.Add(Message.Error(
					"Неизвестная категория опций '" + ValueStack[ValueStack.Depth-4].strVal + "'",
					LocationStack[LocationStack.Depth-4].StartLine, LocationStack[LocationStack.Depth-4].StartColumn,
					"LanD"
				));
			}

			bool goodOption = true;
			switch (optCategory)
			{
				case OptionCategory.PARSING:
					ParsingOption parsingOpt;
					goodOption = Enum.TryParse(ValueStack[ValueStack.Depth-3].strVal.ToUpper(), out parsingOpt);
					if(goodOption) 
						SafeGrammarAction(() => { 
					 		ConstructedGrammar.SetOption(parsingOpt, ValueStack[ValueStack.Depth-1].strList.ToArray());
					 	}, LocationStack[LocationStack.Depth-4]);
					break;
				case OptionCategory.NODES:
					NodeOption nodeOpt;
					goodOption = Enum.TryParse(ValueStack[ValueStack.Depth-3].strVal.ToUpper(), out nodeOpt);
					if(goodOption)
						SafeGrammarAction(() => { 					
							ConstructedGrammar.SetOption(nodeOpt, ValueStack[ValueStack.Depth-1].strList.ToArray());
						}, LocationStack[LocationStack.Depth-4]);
					break;
				case OptionCategory.MAPPING:
					MappingOption mappingOpt;
					goodOption = Enum.TryParse(ValueStack[ValueStack.Depth-3].strVal.ToUpper(), out mappingOpt);
					if(goodOption)
						SafeGrammarAction(() => { 			
							ConstructedGrammar.SetOption(mappingOpt, ValueStack[ValueStack.Depth-1].strList.ToArray(), ValueStack[ValueStack.Depth-2].dynamicList.ToArray());
						}, LocationStack[LocationStack.Depth-4]);
					break;
				default:
					break;
			}
			
			if(!goodOption)
			{
				Errors.Add(Message.Error(
					"Опция '" + ValueStack[ValueStack.Depth-3].strVal + "' не определена для категории '" + ValueStack[ValueStack.Depth-4].strVal + "'",
					LocationStack[LocationStack.Depth-3].StartLine, LocationStack[LocationStack.Depth-3].StartColumn,
					"LanD"
				));
			}
		}
        break;
      case 33: // opt_args -> LPAR, args, RPAR
{ CurrentSemanticValue.dynamicList = ValueStack[ValueStack.Depth-2].dynamicList; }
        break;
      case 34: // opt_args -> /* empty */
{ CurrentSemanticValue.dynamicList = new List<dynamic>(); }
        break;
      case 35: // args -> args, COMMA, RNUM
{ CurrentSemanticValue.dynamicList = ValueStack[ValueStack.Depth-3].dynamicList; CurrentSemanticValue.dynamicList.Add(ValueStack[ValueStack.Depth-1].doubleVal); }
        break;
      case 36: // args -> args, COMMA, STRING
{ CurrentSemanticValue.dynamicList = ValueStack[ValueStack.Depth-3].dynamicList; CurrentSemanticValue.dynamicList.Add(ValueStack[ValueStack.Depth-1].strVal); }
        break;
      case 37: // args -> args, COMMA, ID
{ CurrentSemanticValue.dynamicList = ValueStack[ValueStack.Depth-3].dynamicList; CurrentSemanticValue.dynamicList.Add(ValueStack[ValueStack.Depth-1].strVal); }
        break;
      case 38: // args -> RNUM
{ CurrentSemanticValue.dynamicList = new List<dynamic>(){ ValueStack[ValueStack.Depth-1].doubleVal }; }
        break;
      case 39: // args -> STRING
{ CurrentSemanticValue.dynamicList = new List<dynamic>(){ ValueStack[ValueStack.Depth-1].strVal }; }
        break;
      case 40: // args -> ID
{ CurrentSemanticValue.dynamicList = new List<dynamic>(){ ValueStack[ValueStack.Depth-1].strVal }; }
        break;
      case 41: // identifiers -> identifiers, ID
{ CurrentSemanticValue.strList = ValueStack[ValueStack.Depth-2].strList; CurrentSemanticValue.strList.Add(ValueStack[ValueStack.Depth-1].strVal); }
        break;
      case 42: // identifiers -> /* empty */
{ CurrentSemanticValue.strList = new List<string>(); }
        break;
    }
#pragma warning restore 162, 1522
  }

  protected override string TerminalToString(int terminal)
  {
    if (aliases != null && aliases.ContainsKey(terminal))
        return aliases[terminal];
    else if (((Tokens)terminal).ToString() != terminal.ToString(CultureInfo.InvariantCulture))
        return ((Tokens)terminal).ToString();
    else
        return CharToString((char)terminal);
  }


private void SafeGrammarAction(Action action, LexLocation loc)
{
	try
	{
		action();
	}
	catch(IncorrectGrammarException ex)
	{
		Errors.Add(Message.Error(
			ex.Message,
			loc.StartLine, loc.StartColumn,
			"LanD"
		));
	}
}

}
}
