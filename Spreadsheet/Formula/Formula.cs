﻿// Skeleton written by Joe Zachary for CS 3500, September 2013
// Read the entire skeleton carefully and completely before you
// do anything else!

// Version 1.1 (9/22/13 11:45 a.m.)

// Change log:
//  (Version 1.1) Repaired mistake in GetTokens
//  (Version 1.1) Changed specification of second constructor to
//                clarify description of how validation works

// (Daniel Kopta) 
// Version 1.2 (9/10/17) 

// Change log:
//  (Version 1.2) Changed the definition of equality with regards
//                to numeric tokens
//@author Camille van Ginkel wrote and implemented assigned methods for PS3



using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SpreadsheetUtilities
{
    /// <summary>
    /// Represents formulas written in standard infix notation using standard precedence
    /// rules.  The allowed symbols are non-negative numbers written using double-precision 
    /// floating-point syntax (without unary preceeding '-' or '+'); 
    /// variables that consist of a letter or underscore followed by 
    /// zero or more letters, underscores, or digits; parentheses; and the four operator 
    /// symbols +, -, *, and /.  
    /// 
    /// Spaces are significant only insofar that they delimit tokens.  For example, "xy" is
    /// a single variable, "x y" consists of two variables "x" and y; "x23" is a single variable; 
    /// and "x 23" consists of a variable "x" and a number "23".
    /// 
    /// Associated with every formula are two delegates:  a normalizer and a validator.  The
    /// normalizer is used to convert variables into a canonical form, and the validator is used
    /// to add extra restrictions on the validity of a variable (beyond the standard requirement 
    /// that it consist of a letter or underscore followed by zero or more letters, underscores,
    /// or digits.)  Their use is described in detail in the constructor and method comments.
    /// </summary>
    public class Formula
    {
        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntactically invalid,
        /// throws a FormulaFormatException with an explanatory Message.
        /// 
        /// The associated normalizer is the identity function, and the associated validator
        /// maps every string to true.  
        /// </summary>
        /// 

        private string theFormula;  //holds passed in formula from  constructor
        private string FormulaToString;  //holds the string with normalized tokens
        private Func<string, string> normalizer; //holds passed in normalize method
        private Func<string, bool> validator; //holds passed in IsValid method

        IList<string> normalizedTokens; // list of normalized token in Evaluate method
        SortedSet<string> variables; //set of all normalized variables

        int numOfLeftParen;
        int numOfRightParen;
        bool checkNext;
        bool extraCheckNext;

        public Formula(String formula) :
            this(formula, s => s, s => true)
        {
        }

        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntactically incorrect,
        /// throws a FormulaFormatException with an explanatory Message.
        /// 
        /// The associated normalizer and validator are the second and third parameters,
        /// respectively.  
        /// 
        /// If the formula contains a variable v such that normalize(v) is not a legal variable, 
        /// throws a FormulaFormatException with an explanatory message. 
        /// 
        /// If the formula contains a variable v such that isValid(normalize(v)) is false,
        /// throws a FormulaFormatException with an explanatory message.
        /// 
        /// Suppose that N is a method that converts all the letters in a string to upper case, and
        /// that V is a method that returns true only if a string consists of one letter followed
        /// by one digit.  Then:
        /// 
        /// new Formula("x2+y3", N, V) should succeed
        /// new Formula("x+y3", N, V) should throw an exception, since V(N("x")) is false
        /// new Formula("2x+y3", N, V) should throw an exception, since "2x+y3" is syntactically incorrect.
        /// </summary>
        public Formula(String formula, Func<string, string> normalize, Func<string, bool> isValid)
        {
            this.theFormula = formula;
            this.normalizer = normalize;
            this.validator = isValid;

            /// One Token Rule:  There must be at least one token
            if (string.IsNullOrWhiteSpace(formula))
            {
                throw new FormulaFormatException("Formula is empty.  Input valid formula.");
            }

            variables = new SortedSet<string>();
            normalizedTokens = new List<string>();  // for Evaluate method

            numOfLeftParen = 0;
            numOfRightParen = 0;
            checkNext = false;
            extraCheckNext = false;
            StringBuilder temp = new StringBuilder();
            foreach (string token in GetTokens(theFormula))
            {
                ParsingVariables(variables, token);

                /// Parsing:  after splitting formula into tokens, valid tokens are only (, ), +, -, *, /, and decimal real numbers (including scientific notation)
                ParsingTokens(normalizedTokens, variables, token, temp);

                /// Right Parentheses Rule:  When reading the tokens from left to right, at no point should the number of closing parentheses seen so far be greater than the number on opening parentheses seen so far.
                /// Balanced Parentheses Rule:  The total number of opening parentheses must be equal to the total number of closing parentheses
                RightParenthesisRule(token);

                /// Parenthesis/ Operator Following Rule:  Any token immediately following an open parenthesis or an operator must be either a number, a variable, or an opening parenthesis
                FollowingRule(token);

                /// Extra Following Rule:  Any token that immediately follows a number a variable or a closing parenthesis must be either an operator or a closing parenthesis
                ExtraFollowingRule(token);
            }

            /// Starting Token Rule: The first token of an expression must be a number, a variable, or an opening parenthesis
            /// Ending Token Rule: The first token of an expression must be a number, a variable, or a clossing parenthesis
            TokenRules();
            BalancedParenthesisRule();
            FormulaToString = temp.ToString();
        }

        private bool IsVariable(string token)
        {
            String varPattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";  //determines whether a token is a variable
            return Regex.IsMatch(token, varPattern);
        }
        private bool IsLeftParenthesis(String token)
        {
            String lpPattern = @"\("; //determines whether a token is a '('
            bool result = Regex.IsMatch(token, lpPattern);
            return result;
        }
        private bool IsRightParenthesis(String token)
        {
            String rpPattern = @"\)"; //determines whether a token is a ')'
            bool result = Regex.IsMatch(token, rpPattern);
            return result;
        }

        private bool IsOperator(String token)
        {

            String opPattern = @"[\+\-*/]"; //determines whether a token is +, -, *, or \
            bool result = Regex.IsMatch(token, opPattern);
            return result;
        }

        private bool IsRealNumber(String token)
        {
            double result; //determines whether a token is a real number or number in scientfic notation
            bool isNum = Double.TryParse(token, out result);
            return isNum;
        }

        /// <sumary>
        /// Parsing:  after splitting formula into tokens, valid tokens are 
        /// only (, ), +, -, *, /, variables, and decimal real numbers (including scientific notation)
        /// 
        /// Also builds a string while enumerating throw tokens if no exceptions are thrown.
        /// Returns string with concacted tokens and normalized variables.  The resulting string is given to the ToString method.
        /// </sumary>
        private void ParsingVariables(SortedSet<string> variables, string token)
        {
            if (IsVariable(token) || IsVariable(normalizer(token)))
            {
                if (!variables.Contains(token))
                {
                    variables.Add(normalizer(token));

                    if (!IsVariable(normalizer(token)))
                    {
                        throw new FormulaFormatException("The normalized version of the variable " + token +
                    " is not a valid standard variable.  Check that normalizer does not convert valid variables to invalid variables.");
                    }
                    if (!validator(normalizer(token)))
                    {
                        throw new FormulaFormatException("The normalized version of the variable " + token +
                            " does not meet the validator restrictions.  Check variable and validator function.");
                    }
                }
            }

        }

        /// <sumary>
        /// Parsing:  after splitting formula into tokens, valid tokens are 
        /// only (, ), +, -, *, /, variables, and decimal real numbers (including scientific notation)
        /// 
        /// Also builds a string while enumerating throw tokens if no exceptions are thrown.
        /// Returns string with concacted tokens and normalized variables.  The resulting string is given to the ToString method.
        /// </sumary>
        private void ParsingTokens(IList<string> tokens, SortedSet<string> variables, string token, StringBuilder temp)
        {
            if (IsVariable(normalizer(token)))
            {
                temp.Append(normalizer(token));
                tokens.Add(normalizer(token));
            }
            else if (!variables.Contains(token))  //if the token is not a variable
            {
                //Check if it is (, ), +, -, *, /, or a real number
                if (!(IsLeftParenthesis(token) || IsRightParenthesis(token) || IsLeftParenthesis(token) || IsOperator(token) || IsRealNumber(token)))
                {
                    throw new FormulaFormatException("The token " + token + " is not a valid input in formula.  Check input.");
                }
                temp.Append(token);
                tokens.Add(token);
            }
        }


        /// <sumary>
        /// Right Parentheses Rule:  When reading the tokens from left to right,
        /// at no point should the number of closing parentheses seen so far be 
        /// greater than the number on opening parentheses seen so far.
        /// </sumary>
        private void RightParenthesisRule(string token)
        {
            if (IsLeftParenthesis(token))
            {
                numOfLeftParen++;
            }
            if (IsRightParenthesis(token))
            {
                numOfRightParen++;
                if (numOfRightParen > numOfLeftParen)
                {
                    throw new FormulaFormatException("A right parenthesis was not preceded by a left parenthesis in formula.  Check parentheses in formula.");
                }
            }
        }

        /// <sumary>
        /// Balanced Parentheses Rule:  The total number of opening parentheses must
        /// be equal to the total number of closing parentheses
        /// </sumary>
        private void BalancedParenthesisRule()
        {
            /// Balanced Parentheses Rule:
            if (!(numOfLeftParen == numOfRightParen))
            {
                throw new FormulaFormatException("Right and left parentheses are not balanced.  Check that all parentheses are closed.");
            }
        }


        /// <sumary>
        /// Starting Token Rule: The first token of an expression must be a number, 
        /// a variable, or an opening parenthesis
        /// 
        /// Ending Token Rule: The last token of an expression must be a number, 
        /// a variable, or a closing parenthesis
        /// </sumary>
        private void TokenRules()
        {
            //Starting Token Rule:
            string firstToken = normalizedTokens.First<string>();
            if (!(IsVariable(normalizer(firstToken)) || IsRealNumber(firstToken) || IsLeftParenthesis(firstToken)))
            {
                throw new FormulaFormatException("Formula started with " + firstToken + ". Formula should start with a variable, number, or open parenthesis.  Check formula input.");
            }

            //Ending Token Rule:
            string lastToken = normalizedTokens.Last<string>();
            if (!(IsVariable(normalizer(lastToken)) || IsRealNumber(lastToken) || IsRightParenthesis(lastToken)))
            {
                throw new FormulaFormatException("Formula ended with " + lastToken + ".  Forumula should end with a variable, number, or closing parenthesis.  Check formula input.");
            }
        }

        /// <sumary>
        /// Parenthesis/ Operator Following Rule:  Any token immediately following an open
        /// parenthesis or an operator must be either a number, a variable, or an opening parenthesis
        /// </sumary>
        private void FollowingRule(string token)
        {

            if (checkNext)
            {
                if (!(IsRealNumber(token) || IsVariable(normalizer(token)) || IsLeftParenthesis(token)))
                {
                    throw new FormulaFormatException("The token " + token + " followed an open parenthesis or operator.  Token should be either a number, variable, or another open parenthesis.  Check syntax of formula.");
                }
                checkNext = false;
            }
            if (IsLeftParenthesis(token) || IsOperator(token))
            {
                checkNext = true;
            }
        }


        /// <sumary>
        /// Extra Following Rule:  Any token that immediately follows a number a variable 
        /// or a closing parenthesis must be either an operator or a closing parenthesis
        /// </sumary>
        private void ExtraFollowingRule( string token)
        {
            if (extraCheckNext)
            {
                if (!(IsRightParenthesis(token) || IsOperator(token)))
                {
                    throw new FormulaFormatException("The token " + token + " followed a number, variable or right parenthesis.  The token should be an operator or anouther right parenthesis.  Check syntax of formula.");
                }
                extraCheckNext = false;
            }
            if (IsRealNumber(token) || IsVariable(normalizer(token)) || IsRightParenthesis(token))
            {
                extraCheckNext = true;
            }
        }


        /// <summary>
        /// Evaluates this Formula, using the lookup delegate to determine the values of
        /// variables.  When a variable symbol v needs to be determined, it should be looked up
        /// via lookup(normalize(v)). (Here, normalize is the normalizer that was passed to 
        /// the constructor.)
        /// 
        /// For example, if L("x") is 2, L("X") is 4, and N is a method that converts all the letters 
        /// in a string to upper case:
        /// 
        /// new Formula("x+7", N, s => true).Evaluate(L) is 11
        /// new Formula("x+7").Evaluate(L) is 9
        /// 
        /// Given a variable symbol as its parameter, lookup returns the variable's value 
        /// (if it has one) or throws an ArgumentException (otherwise).
        /// 
        /// If no undefined variables or divisions by zero are encountered when evaluating 
        /// this Formula, the value is returned.  Otherwise, a FormulaError is returned.  
        /// The Reason property of the FormulaError should have a meaningful explanation.
        ///
        /// This method should never throw an exception.
        /// </summary>

        public object Evaluate(Func<string, double> lookup)
        {
            try
            {
                //Instance variables copied from Evaluator class
                Func<string, double> findVarValue = lookup;
                string exp = this.ToString();
                if (exp.Equals(""))
                {
                    throw new ArgumentException("String argument is empty.");
                }

                Stack<char> operatorsStack = new Stack<char>();  //holds operators of expression
                Stack<Double> valuesStack = new Stack<Double>();  //holds values of expression

                bool checkIfNextTokenIsRightParen = false;
                foreach (string temp in normalizedTokens)
                {
                    string token = temp;
                    if (token.Equals("")) //ignore empty strings
                    {
                        continue;
                    }
                    if (IsVariable(token)) //if token is a variable
                    {
                        //proceed as above using the lookup value of token
                        double varToken = findVarValue(token);
                        token = varToken.ToString();
                    }

                    double t;
                    bool isRealNumber = Double.TryParse(token, out t);  //determines if token is an integer
                    if (!(token.Equals("(") || token.Equals(")") || token.Equals("*") || token.Equals("/") || token.Equals("+") || token.Equals("-") || isRealNumber || IsVariable(token)))//if token does not equals (,),+,-,*,/, non-negative integer, or variable 
                    {
                        throw new ArgumentException("Error: Unexpected symbol");
                    }

                    if (isRealNumber) //if token is a integer
                    {
                        double numToken = Double.Parse(token);
                        if ((operatorsStack.IsOnTop('*') || operatorsStack.IsOnTop('/'))) //if * or / is on top of the operator stack
                        {
                            //pop the value stack and operator stack and apply the operator to the token and popped number

                            char op = operatorsStack.Pop();
                            if (valuesStack.Count == 0)
                            {
                                throw new ArgumentException("Syntax Error: No more values to apply operator");
                            }
                            double firstVal = valuesStack.Pop();
                            double result;
                            if (op == '*')
                            {
                                result = firstVal * numToken;
                            }
                            else // if (op == '/')
                            {
                                if (numToken == 0)
                                {
                                    throw new ArgumentException("Error: Divsion by 0");
                                }
                                result = firstVal / numToken;
                            }
                            valuesStack.Push(result);
                            if (operatorsStack.IsOnTop('('))
                            {
                                checkIfNextTokenIsRightParen = true;
                            }
                        }
                        else  //Otherwise, push token onto value stack
                        {
                            valuesStack.Push(numToken);
                        }

                    }


                    if (token.Equals("+") || token.Equals("-")) //if token is a + or - 
                    {
                        char op = char.Parse(token);
                        if (operatorsStack.IsOnTop('+') || operatorsStack.IsOnTop('-')) //if + or - is already on top of the operators stack
                        {
                            //pop the value stack twice and the operator stack once
                            //apply the operator to the numbers, and push result onto value stack
                            if (valuesStack.Count < 2)
                            {
                                throw new ArgumentException("Syntax error.");
                            }
                            else
                            {
                                PopPopPopEvalPush(operatorsStack, valuesStack);
                            }

                        }
                        //push token onto the operators stack
                        operatorsStack.Push(op);
                    }

                    if (token.Equals("*") || token.Equals("/")) //if token it a * or /
                    {
                        // push token onto the operator stack
                        char op = char.Parse(token);
                        operatorsStack.Push(op);
                    }

                    if (token.Equals("(")) //if  token is a '(' left parenthesis
                    {
                        //push token onto operator stack
                        char parenthesis = '(';
                        operatorsStack.Push(parenthesis);
                    }

                    if (token.Equals(")")) //if token is a ')' right parenthesis
                    {

                        if (operatorsStack.IsOnTop('+') || operatorsStack.IsOnTop('-')) //if * or / is on top of the operator stack
                        {
                            //pop the value stack twice and the operator stack once
                            //apply the popped operator to the popped numbers
                            //push the value on to the the value stack
                            PopPopPopEvalPush(operatorsStack, valuesStack);
                            if (operatorsStack.Count == 0 || !operatorsStack.Pop().Equals('('))
                            {
                                throw new ArgumentException("Syntax error");
                            }
                            if (operatorsStack.IsOnTop('*') || operatorsStack.IsOnTop('/'))
                            {
                                PopPopPopEvalPush(operatorsStack, valuesStack);
                            }
                        }
                        if (operatorsStack.IsOnTop('(') && checkIfNextTokenIsRightParen)
                        {
                            operatorsStack.Pop();
                        }
                    }

                }
                //When the last token has been processed
                if (operatorsStack.Count == 0) //if operator stack is empty
                {
                    //value stack should contain a single number 
                    //pop it and report as value of the expression 
                    return valuesStack.Pop();
                }
                else //if operator stack is not empty
                {
                    //There should be one operator on operator stack which is either + or -
                    //there should be two value on value stack 
                    //Apply the operator to the two values and return as value of expression
                    if (valuesStack.Count < 2)
                    {
                        throw new ArgumentException("Syntax Error");
                    }
                    else
                    {
                        PopPopPopEvalPush(operatorsStack, valuesStack);
                    }

                    return valuesStack.Pop();
                }
            }
            catch (ArgumentException e)
            {
                return new FormulaError(e.Message);
            }
        }



        /*
         * Helper Method:
         * Pops the value stack twice and the operator stack once. Then applies the operator to the numbers, 
         * and pushes result onto value stack.
         */
        static void PopPopPopEvalPush(Stack<char> operators, Stack<Double> values)
        {
            double num2 = values.Pop();
            double num1 = values.Pop();

            char op = operators.Pop();
            double result;
            if (op == '*')
                result = num1 * num2;
            else if (op == '/')
                result = num1 / num2;
            else if (op == '+')
                result = num1 + num2;
            else //if (op == '-')
                result = num1 - num2;

            values.Push(result);

            if (operators.IsOnTop('*') || operators.IsOnTop('/'))
            {
                PopPopPopEvalPush(operators, values);
            }
        }


        /// <summary>
        /// Enumerates the normalized versions of all of the variables that occur in this 
        /// formula.  No normalization may appear more than once in the enumeration, even 
        /// if it appears more than once in this Formula.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        /// 
        /// new Formula("x+y*z", N, s => true).GetVariables() should enumerate "X", "Y", and "Z"
        /// new Formula("x+X*z", N, s => true).GetVariables() should enumerate "X" and "Z".
        /// new Formula("x+X*z").GetVariables() should enumerate "x", "X", and "z".
        /// </summary>
        public IEnumerable<String> GetVariables()
        {
            foreach (string var in variables)
            {
                yield return var;
            }
        }

        /// <summary>
        /// Returns a string containing no spaces which, if passed to the Formula
        /// constructor, will produce a Formula f such that this.Equals(f).  All of the
        /// variables in the string should be normalized.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        /// 
        /// new Formula("x + y", N, s => true).ToString() should return "X+Y"
        /// new Formula("x + Y").ToString() should return "x+Y"
        /// </summary>
        public override string ToString()
        {
            //Parsing() builds string while enumerating through GetTokens and normalizes variables
            string Result = FormulaToString;
            return Result;
        }

        /// <summary>
        /// If obj is null or obj is not a Formula, returns false.  Otherwise, reports
        /// whether or not this Formula and obj are equal.
        /// 
        /// Two Formulae are considered equal if they consist of the same tokens in the
        /// same order.  To determine token equality, all tokens are compared as strings 
        /// except for numeric tokens and variable tokens.
        /// Numeric tokens are considered equal if they are equal after being "normalized" 
        /// by C#'s standard conversion from string to double, then back to string. This 
        /// eliminates any inconsistencies due to limited floating point precision.
        /// Variable tokens are considered equal if their normalized forms are equal, as 
        /// defined by the provided normalizer.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        ///  
        /// new Formula("x1+y2", N, s => true).Equals(new Formula("X1  +  Y2")) is true
        /// new Formula("x1+y2").Equals(new Formula("X1+Y2")) is false
        /// new Formula("x1+y2").Equals(new Formula("y2+x1")) is false
        /// new Formula("2.0 + x7").Equals(new Formula("2.000 + x7")) is true
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }
            if (!(obj is Formula))
            {
                return false;
            }
            string thisString = this.ToString();

            Formula passedIn = (Formula)obj;
            string passedInString = passedIn.ToString();

            foreach (string passedInToken in GetTokens(passedIn.ToString()))
            {
                if (IsRealNumber(passedInToken))
                {
                    double dPassedIn = Double.Parse(passedInToken);
                    string backToStrPassedIn = dPassedIn.ToString();
                    passedInString = passedInString.Replace(passedInToken, backToStrPassedIn);
                }
            }

            foreach (string thisToken in GetTokens(this.ToString()))
            {
                if (IsRealNumber(thisToken))
                {
                    double dThis = Double.Parse(thisToken);
                    string backToStrThis = dThis.ToString();
                    thisString = thisString.Replace(thisToken, backToStrThis);
                }
            }
            return thisString.Equals(passedInString);
        }

        /// <summary>
        /// Reports whether f1 == f2, using the notion of equality from the Equals method.
        /// Note that if both f1 and f2 are null, this method should return true.  If one is
        /// null and one is not, this method should return false.
        /// </summary>
        public static bool operator ==(Formula f1, Formula f2)
        {
            if (f1 is null && f2 is null)
            {
                return true;
            }
            if (f1 is null || f2 is null)
            {
                return false;
            }
            return f1.Equals(f2);
        }

        /// <summary>
        /// Reports whether f1 != f2, using the notion of equality from the Equals method.
        /// Note that if both f1 and f2 are null, this method should return false.  If one is
        /// null and one is not, this method should return true.
        /// </summary>
        public static bool operator !=(Formula f1, Formula f2)
        {
            if (f1 is null && f2 is null)
            {
                return false;
            }
            if (f1 is null || f2 is null)
            {
                return true;
            }
            return !f1.Equals(f2);
        }

        /// <summary>
        /// Returns a hash code for this Formula.  If f1.Equals(f2), then it must be the
        /// case that f1.GetHashCode() == f2.GetHashCode().  Ideally, the probability that two 
        /// randomly-generated unequal Formulae have the same hash code should be extremely small.
        /// </summary>
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        /// <summary>
        /// Given an expression, enumerates the tokens that compose it.  Tokens are left paren;
        /// right paren; one of the four operator symbols; a string consisting of a letter or underscore
        /// followed by zero or more letters, digits, or underscores; a double literal; and anything that doesn't
        /// match one of those patterns.  There are no empty tokens, and no token contains white space.
        /// </summary>
        private static IEnumerable<string> GetTokens(String formula)
        {
            // Patterns for individual tokens
            String lpPattern = @"\(";
            String rpPattern = @"\)";
            String opPattern = @"[\+\-*/]";
            String varPattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";
            String doublePattern = @"(?: \d+\.\d* | \d*\.\d+ | \d+ ) (?: [eE][\+-]?\d+)?";
            String spacePattern = @"\s+";

            // Overall pattern
            String pattern = String.Format("({0}) | ({1}) | ({2}) | ({3}) | ({4}) | ({5})",
                                            lpPattern, rpPattern, opPattern, varPattern, doublePattern, spacePattern);

            // Enumerate matching tokens that don't consist solely of white space.
            foreach (String s in Regex.Split(formula, pattern, RegexOptions.IgnorePatternWhitespace))
            {
                if (!Regex.IsMatch(s, @"^\s*$", RegexOptions.Singleline))
                {
                    yield return s;
                }
            }

        }
    }

    /// <summary>
    /// Used to report syntactic errors in the argument to the Formula constructor.
    /// </summary>
    public class FormulaFormatException : Exception
    {
        /// <summary>
        /// Constructs a FormulaFormatException containing the explanatory message.
        /// </summary>
        public FormulaFormatException(String message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Used as a possible return value of the Formula.Evaluate method.
    /// </summary>
    public struct FormulaError
    {
        /// <summary>
        /// Constructs a FormulaError containing the explanatory reason.
        /// </summary>
        /// <param name="reason"></param>
        public FormulaError(String reason)
            : this()
        {
            Reason = reason;
        }

        /// <summary>
        ///  The reason why this FormulaError was created.
        /// </summary>
        public string Reason { get; private set; }
    }
}
static class StackExtensions
{
    public static bool IsOnTop(this Stack<char> s, char C)
    {
        if (s.Count < 1)
            return false;
        return s.Peek().Equals(C);

    }

}
