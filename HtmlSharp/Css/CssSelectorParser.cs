﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlSharp.Elements;
using HtmlSharp.Extensions;
using System.Collections.Specialized;

namespace HtmlSharp.Css
{
    public class SelectorParser
    {
        SelectorTokenizer tokenizer = new SelectorTokenizer();
        List<SelectorToken> tokens = new List<SelectorToken>();
        int currentPosition = 0;

        SelectorToken CurrentToken { get { return tokens.ElementAtOrDefault(currentPosition); } }
        bool End { get { return currentPosition >= tokens.Count; } }

        public SelectorsGroup Parse(string selector)
        {
            tokens = tokenizer.Tokenize(selector).ToList();

            List<Selector> selectors = new List<Selector>();
            while (!End)
            {
                selectors.Add(ParseSelector());
                if (!End)
                {
                    Consume(SelectorTokenType.Comma);
                }
            }

            return new SelectorsGroup(selectors);
        }

        void Consume(SelectorTokenType tokenType)
        {
            Expect(tokenType);
            currentPosition++;
        }

        void Consume(string tokenText)
        {
            Expect(tokenText);
            currentPosition++;
        }

        private void Expect(string tokenText)
        {
            if (CurrentToken == null)
            {
                ParseErrorOnMissingToken();
            }
            else if (CurrentToken.Text != tokenText)
            {
                ParseError(string.Format(
                    "Expected token {0} but found {1}", tokenText, CurrentToken.TokenType));
            }
        }

        void Expect(SelectorTokenType tokenType)
        {
            if (CurrentToken == null)
            {
                ParseErrorOnMissingToken();
            }
            else if (CurrentToken.TokenType != tokenType)
            {
                ParseError(string.Format(
                    "Expected token {0} but found {1}", tokenType, CurrentToken.TokenType));
            }
        }

        void ParseError(string message)
        {
            throw new FormatException(message);
        }

        void ParseErrorOnMissingToken()
        {
            if (CurrentToken == null)
            {
                ParseError("Unexpected end of css selector");
            }
        }

        private Selector ParseSelector()
        {
            List<SimpleSelectorSequence> simpleSelectorSequences = new List<SimpleSelectorSequence>();
            List<Combinator> combinators = new List<Combinator>();

            Combinator combinator = null;
            do
            {
                simpleSelectorSequences.Add(ParseSimpleSelectorSequence());

                combinator = ParseCombinator();
                if (combinator != null)
                {
                    combinators.Add(combinator);
                }
            }
            while (combinator != null);

            return new Selector(simpleSelectorSequences, combinators);
        }

        private Combinator ParseCombinator()
        {
            Combinator combinator = null;
            if (CurrentToken != null)
            {
                Dictionary<SelectorTokenType, Combinator> lookup = new Dictionary<SelectorTokenType, Combinator>()
                {
                    { SelectorTokenType.Plus, new AdjacentSiblingCombinator() },
                    { SelectorTokenType.Greater, new ChildCombinator() },
                    { SelectorTokenType.Tilde, new GeneralSiblingCombinator() },
                    { SelectorTokenType.WhiteSpace, new DescendantCombinator() }
                };

                if (lookup.TryGetValue(CurrentToken.TokenType, out combinator))
                {
                    currentPosition++;
                    SkipWhiteSpace();
                }
            }
            return combinator;
        }

        private SimpleSelectorSequence ParseSimpleSelectorSequence()
        {
            ParseErrorOnMissingToken();

            TypeSelector typeSelector = ParseTypeSelector() ?? ParseUniversalSelector();

            List<SelectorFilter> filters = new List<SelectorFilter>();
            while (true)
            {
                if (CurrentToken == null)
                {
                    break;
                }
                SelectorFilter filterSelector = ParseIDSelector() ?? ParseClassSelector() ??
                    ParseAttributeSelector() ?? ParsePseudoSelector() ?? ParseNegation();
                if (filterSelector != null)
                {
                    filters.Add(filterSelector);
                }
                else
                {
                    break;
                }
            }
            if (typeSelector == null)
            {
                if (filters.Count == 0)
                {
                    ParseError("Expected hash, class, attrib, pseudo, or negation");
                }
                typeSelector = new UniversalSelector();
            }
            return new SimpleSelectorSequence(typeSelector, filters);
        }

        private SelectorFilter ParseNegation()
        {
            return null;
            throw new NotImplementedException();
        }

        private SelectorFilter ParsePseudoSelector()
        {
            SelectorFilter selector = null;
            if (CurrentToken.Text == ":")
            {
                currentPosition++;
                ParseErrorOnMissingToken();
                if (CurrentToken.Text == ":")
                {
                    ParseError(":: selectors do not apply to static html, so were not implemented.");
                }
                else if (CurrentToken.TokenType == SelectorTokenType.Ident)
                {
                    var filterLookup = new Dictionary<string, SelectorFilter>()
                    {
                        { "root", new RootFilter() },
                        { "first-child", new FirstChildFilter() },
                        { "last-child", new LastChildFilter() },
                        { "first-of-type", new FirstOfTypeFilter() },
                        { "last-of-type", new LastOfTypeFilter() },
                        { "only-child", new OnlyChildFilter() },
                        { "only-of-type", new OnlyOfTypeFilter() },
                        { "empty", new EmptyFilter() },
                        { "enabled", new EnabledFilter() },
                        { "disabled", new DisabledFilter() },
                        { "checked", new CheckedFilter() }
                    };

                    if (filterLookup.ContainsKey(CurrentToken.Text))
                    {
                        selector = filterLookup[CurrentToken.Text];
                        currentPosition++;
                    }
                    else
                    {
                       ParseError("Unsupported or invalid pseudo selector :" + filterLookup);
                    }
                }
                else if (CurrentToken.TokenType == SelectorTokenType.Function)
                {
                    var filterLookup = new Dictionary<string, Func<Expression, SelectorFilter>>()
                    {
                        { "nth-child(", e => new NthChildFilter(e) },
                        { "nth-last-child(", e => new NthLastChildFilter(e) },
                        { "nth-of-type(", e => new NthOfTypeFilter(e) },
                        { "nth-last-of-type(", e => new NthLastOfTypeFilter(e) }
                    };

                    if (filterLookup.ContainsKey(CurrentToken.Text))
                    {
                        string text = CurrentToken.Text;
                        currentPosition++;
                        ParseErrorOnMissingToken();
                        SkipWhiteSpace();
                        ParseErrorOnMissingToken();
                        Expression expression = ParseExpression();
                        if (expression == null)
                        {
                            ParseError("Unable to parse expression");
                        }
                        selector = filterLookup[text](expression);
                    }
                    else if (CurrentToken.Text == "lang(")
                    {
                        currentPosition++;
                        ParseErrorOnMissingToken();
                        SkipWhiteSpace();
                        ParseErrorOnMissingToken();
                        if (CurrentToken.TokenType == SelectorTokenType.Ident)
                        {
                            selector = new LangFilter(CurrentToken.Text);
                        }
                        else if (CurrentToken.TokenType == SelectorTokenType.String)
                        {
                            selector = new LangFilter(CurrentToken.Text.Substring(1, CurrentToken.Text.Length - 2));
                        }

                    }
                    else
                    {
                        ParseError("Unrecognized pseudo function");
                    }
                    currentPosition++;
                    Consume(")");
                }
            }
            return selector;
        }

        /* In CSS3, the expressions are identifiers, strings, */
        /* or of the form "an+b" */
        private Expression ParseExpression()
        {
            bool negative = false;
            Expression expression = null;
            if (CurrentToken.Text == "-")
            {
                negative = true;
                currentPosition++;
            }
            if (CurrentToken.TokenType == SelectorTokenType.Dimension || CurrentToken.Text == "n" || CurrentToken.Text == "-n")
            {
                string dimension = CurrentToken.Text;
                int dim;
                string negativeSecond = null;

                Match m = Regex.Match(CurrentToken.Text, @"(\d+n)-(\d+)");
                if (m.Success)
                {
                    dimension = m.Groups[1].Value;
                    negativeSecond = m.Groups[2].Value;
                }

                if (int.TryParse(dimension.Substring(0, dimension.Length - 1), out dim) || CurrentToken.Text == "n" || CurrentToken.Text == "-n")
                {
                    if (CurrentToken.Text == "n")
                    {
                        dim = 1;
                    }
                    else if (CurrentToken.Text == "-n")
                    {
                        dim = -1;
                    }
                    dim = negative ? dim * -1 : dim;
                    currentPosition++;
                    if (CurrentToken.TokenType == SelectorTokenType.Plus)
                    {
                        currentPosition++;
                        if (CurrentToken.TokenType == SelectorTokenType.Number)
                        {
                            //xn+b
                            int num;
                            if (int.TryParse(CurrentToken.Text, out num))
                            {
                                expression = new NumericExpression(dim, num);
                            }
                            else
                            {
                                //parse error
                            }
                        }
                    }
                    else if (negativeSecond != null)
                    {
                        //xn-b
                        int num;
                        if (int.TryParse(negativeSecond, out num))
                        {
                            expression = new NumericExpression(dim, -num);
                            currentPosition--;
                        }
                        else
                        {
                            //parse error
                        }
                    }
                    else if (CurrentToken.Text == ")")
                    {
                        expression = new NumericExpression(dim, 0);
                        currentPosition--;
                    }
                }
                else
                {
                    //parse error
                }
            }
            else if (CurrentToken.TokenType == SelectorTokenType.Number)
            {
                //b
                int num;
                if (int.TryParse(CurrentToken.Text, out num))
                {
                    expression = new NumericExpression(0, num * (negative ? -1 : 1));
                }
                else
                {
                    //parse error
                }
            }
            else if (CurrentToken.TokenType == SelectorTokenType.Ident)
            {
                //odd or even
                if (CurrentToken.Text == "odd")
                {
                    expression = new OddExpression();
                }
                else if (CurrentToken.Text == "even")
                {
                    expression = new OddExpression();
                }
                else
                {
                    //parse error
                }
            }
            else if (CurrentToken.TokenType == SelectorTokenType.String)
            {
                string withoutQuotes = CurrentToken.Text.Substring(1, CurrentToken.Text.Length - 2);
                if (CurrentToken.Text == "odd")
                {
                    expression = new OddExpression();
                }
                else if (CurrentToken.Text == "even")
                {
                    expression = new OddExpression();
                }
                else
                {
                    //parse error
                }
            }

            return expression;
        }

        private SelectorFilter ParseAttributeSelector()
        {
            SelectorFilter selector = null;

            if (CurrentToken.Text == "[")
            {
                currentPosition++;
                SkipWhiteSpace();
                SelectorNamespacePrefix ns = ParseCssNamespacePrefix();
                string attributeType = null;
                if (CurrentToken.TokenType == SelectorTokenType.Ident)
                {
                    attributeType = CurrentToken.Text;
                    currentPosition++;
                }
                else
                {
                    // parse error
                }
                SkipWhiteSpace();
                if (CurrentToken.TokenType == SelectorTokenType.PrefixMatch)
                {
                    currentPosition++;
                    SkipWhiteSpace();
                    if (CurrentToken.TokenType == SelectorTokenType.Ident)
                    {
                        selector = new AttributePrefixFilter(attributeType, CurrentToken.Text);
                    }
                    else if (CurrentToken.TokenType == SelectorTokenType.String)
                    {
                        selector = new AttributePrefixFilter(attributeType, CurrentToken.Text);
                    }
                    else
                    {
                        //parse error lol
                    }
                    currentPosition++;
                    SkipWhiteSpace();
                }
                else if (CurrentToken.TokenType == SelectorTokenType.SuffixMatch)
                {
                    currentPosition++;
                    SkipWhiteSpace();
                    if (CurrentToken.TokenType == SelectorTokenType.Ident)
                    {
                        selector = new AttributeSuffixFilter(attributeType, CurrentToken.Text);
                    }
                    else if (CurrentToken.TokenType == SelectorTokenType.String)
                    {
                        selector = new AttributeSuffixFilter(attributeType, CurrentToken.Text);
                    }
                    else
                    {
                        //parse error lol
                    }
                    currentPosition++;
                    SkipWhiteSpace();
                }
                else if (CurrentToken.TokenType == SelectorTokenType.SubstringMatch)
                {
                    currentPosition++;
                    SkipWhiteSpace();
                    if (CurrentToken.TokenType == SelectorTokenType.Ident)
                    {
                        selector = new AttributeSubstringFilter(attributeType, CurrentToken.Text);
                    }
                    else if (CurrentToken.TokenType == SelectorTokenType.String)
                    {
                        selector = new AttributeSubstringFilter(attributeType, CurrentToken.Text);
                    }
                    else
                    {
                        //parse error lol
                    }
                    currentPosition++;
                    SkipWhiteSpace();
                }
                else if (CurrentToken.Text == "=")
                {
                    currentPosition++;
                    SkipWhiteSpace();
                    if (CurrentToken.TokenType == SelectorTokenType.Ident)
                    {
                        selector = new AttributeExactFilter(attributeType, CurrentToken.Text);
                    }
                    else if (CurrentToken.TokenType == SelectorTokenType.String)
                    {
                        selector = new AttributeExactFilter(attributeType, CurrentToken.Text);
                    }
                    else
                    {
                        //parse error lol
                    }
                    currentPosition++;
                    SkipWhiteSpace();
                }
                else if (CurrentToken.TokenType == SelectorTokenType.Includes)
                {
                    currentPosition++;
                    SkipWhiteSpace();
                    if (CurrentToken.TokenType == SelectorTokenType.Ident)
                    {
                        selector = new AttributeIncludesFilter(attributeType, CurrentToken.Text);
                    }
                    else if (CurrentToken.TokenType == SelectorTokenType.String)
                    {
                        selector = new AttributeIncludesFilter(attributeType, CurrentToken.Text);
                    }
                    else
                    {
                        //parse error lol
                    }
                    currentPosition++;
                    SkipWhiteSpace();
                }
                else if (CurrentToken.TokenType == SelectorTokenType.DashMatch)
                {
                    currentPosition++;
                    SkipWhiteSpace();
                    if (CurrentToken.TokenType == SelectorTokenType.Ident)
                    {
                        selector = new AttributeDashFilter(attributeType, CurrentToken.Text);
                    }
                    else if (CurrentToken.TokenType == SelectorTokenType.String)
                    {
                        selector = new AttributeDashFilter(attributeType, CurrentToken.Text);
                    }
                    else
                    {
                        //parse error lol
                    }
                    currentPosition++;
                    SkipWhiteSpace();
                }
                else if (CurrentToken.Text != "]")
                {
                    //parse error lolz
                }

                if (CurrentToken.Text == "]" && selector == null)
                {
                    selector = new AttributeFilter(attributeType);
                    currentPosition++;
                }
                else if (CurrentToken.Text == "]" && selector != null)
                {
                    currentPosition++;
                }
                else
                {
                    //parse error lolz
                }
            }

            return selector;
        }

        private void SkipWhiteSpace()
        {
            if (CurrentToken.TokenType == SelectorTokenType.WhiteSpace)
            {
                currentPosition++;
            }
        }

        private SelectorFilter ParseClassSelector()
        {
            SelectorFilter selector = null;
            if (CurrentToken.Text == ".")
            {
                currentPosition++;
                if (CurrentToken == null)
                {
                    //parse error
                }
                if (CurrentToken.TokenType == SelectorTokenType.Ident)
                {
                    selector = new ClassFilter("." + CurrentToken.Text);
                }

                currentPosition++;
            }
            return selector;
        }

        private SelectorFilter ParseIDSelector()
        {
            SelectorFilter selector = null;
            if (CurrentToken.TokenType == SelectorTokenType.Hash)
            {
                selector = new IDFilter(CurrentToken.Text);
                currentPosition++;
            }
            return selector;
        }

        private TypeSelector ParseUniversalSelector()
        {
            TypeSelector selector = null;

            int tempPos = currentPosition;
            SelectorNamespacePrefix prefix = ParseCssNamespacePrefix();

            if (CurrentToken != null)
            {
                if (CurrentToken.Text == "*")
                {
                    selector = new UniversalSelector(prefix);
                    currentPosition++;
                }
            }

            return selector;
        }

        private TypeSelector ParseTypeSelector()
        {
            TypeSelector selector = null;

            int tempPos = currentPosition;
            SelectorNamespacePrefix prefix = ParseCssNamespacePrefix();

            if (CurrentToken != null)
            {
                if (CurrentToken.TokenType == SelectorTokenType.Ident)
                {
                    selector = new TypeSelector(CurrentToken.Text, prefix);
                    currentPosition++;
                }
            }

            return selector;
        }

        private SelectorNamespacePrefix ParseCssNamespacePrefix()
        {
            SelectorNamespacePrefix prefix = null;

            if (CurrentToken != null)
            {
                if (CurrentToken.TokenType == SelectorTokenType.Ident)
                {
                    string ident = CurrentToken.Text;
                    currentPosition++;
                    if (CurrentToken == null)
                    {
                        currentPosition--;
                    }
                    else if (CurrentToken.Text == "|")
                    {
                        prefix = new SelectorNamespacePrefix(ident);
                        currentPosition++;
                    }
                    else
                    {
                        currentPosition--;
                    }
                }
                else if (CurrentToken.Text == "*")
                {
                    currentPosition++;
                    if (CurrentToken == null)
                    {
                        currentPosition--;
                    }
                    else if (CurrentToken.Text == "|")
                    {
                        prefix = new SelectorNamespacePrefix("*");
                        currentPosition++;
                    }
                    else
                    {
                        currentPosition--;
                    }
                }
                else if (CurrentToken.Text == "|")
                {
                    //TODO: is this supposed to mean universal selector?
                    prefix = new SelectorNamespacePrefix("");
                    currentPosition++;
                }
            }
            return prefix;
        }
    }


}