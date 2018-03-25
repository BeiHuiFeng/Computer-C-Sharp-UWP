using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace computer {
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    /// 

    class Expression {
        public string ExprStr {get; private set; }
        public string ResultStr { get; private set; }
        private string CaculateStr;
        private bool haveCounted;        // initial value is false, when call func Count(), set it to true
        private bool exprLegit;          // is the expr legitimate?
        private double result;

        private double CountWithoutParen(string str) {
            /*  try to evaluate the expression without parentheses
             *  
             *  use a finite state machine to match operators and operands
             *  state = 0: expect to match a operand(include negative ones)
             *  state = 1: matching a operand with several bits
             *  state = 2: expect to match a operator, + , -, ×, ÷, ^ 
             *  
             *  use two stack to store the operators and operands
             *  for more details, please refer to Data Structure(for C)
             */
            double operand, operandA, operandB;
            String digitStr;
            char op;
            int i, state, operandSPos = 0, operandEPos = 0;     // start position & end position of an operand
            bool isDotExit = false;
            str = str + "#";                                // add a char '#' to indicate the end of the str
            char[] expr = str.ToCharArray();
            Stack<char> StackOP = new Stack<char>();        // for operators
            Stack<double> StackOPRD = new Stack<double>();  // for operands
            StackOP.Push('#');      // push a char '#' in the beginning to indicate the start of the str
            for (i = 0, state = 0; StackOP.Peek() != '#' || expr[i] != '#';) {
                switch (state) {
                    case 0:
                        // a new digit token
                        operandSPos = i;
                        isDotExit = false;
                        if (char.IsLetter(expr[i])) {
                            if (expr[i] == '-') {
                                state = 1;
                                i++;
                                break;
                            }
                            if (IsOP(expr[i])) {
                                ResultStr = "ERROR:\nNo oprend matches " + expr[i].ToString();
                                CaculateStr = "";
                                exprLegit = false;
                                return 0;
                            }
                            if (expr[i] == '.') {
                                ResultStr = "ERROR:\nNo digit befor dot.";
                                CaculateStr = "";
                                exprLegit = false;
                                return 0;
                            }
                        }
                        state = 1;
                        i++;
                        break;
                    case 1:
                        // matching a digit token
                        if (char.IsDigit(expr[i])) i++;
                        else if (expr[i] == '.' && !isDotExit) {
                            i++;
                            isDotExit = true;
                        }
                        else if(expr[i] == '.' && isDotExit) {
                            ResultStr = "ERROR:\nTwo dot exit in a number.";
                            CaculateStr = "";
                            exprLegit = false;
                            return 0;
                        }
                        else if (IsOP(expr[i])) {
                            operandEPos = i - 1;     // match a operand
                            state = 2;             // find a operator next
                            digitStr = str.Substring(operandSPos, operandEPos - operandSPos + 1);
                            Double.TryParse(digitStr, out operand);
                            StackOPRD.Push(operand);
                            // no need to i++
                        }
                        else {
                            ResultStr = "ERROR:\nUnexpected character: " + expr[i].ToString();
                            CaculateStr = "";
                            exprLegit = false;
                            return 0;
                        }
                        break;
                    case 2:
                        // expect to match an operator
                        if (IsOP(expr[i])) {
                            switch (Precede(StackOP.Peek(), expr[i])) {
                                case '<':
                                    // the op on the top of the stack has less priority
                                    StackOP.Push(expr[i]);
                                    i++;
                                    break;
                                case '=':
                                    // in fact function Precede () won't return '='
                                    // so the program won't go here
                                    i++;
                                    break;
                                case '>':
                                    operandB = StackOPRD.Pop();
                                    operandA = StackOPRD.Pop();
                                    op = StackOP.Pop();
                                    operand = Operate(operandA, op, operandB);
                                    if (!exprLegit) return 0;
                                    StackOPRD.Push(operand);
                                    break;
                                default:
                                    break;
                            }
                            state = 0;      // expect to match a new digit
                        }
                        else {
                            // actually the program won't go here
                        }
                        break;
                    default:
                        // actually the program won't go here
                        break;
                }   // switch(state)
            }   // for

            // this is the end of CountWithoutParen
            // the result is stored in the top of StackOPRD
            return StackOPRD.Pop();
        }

        private double Operate(double operandA, char op, double operandB) {
            switch(op) {
                case '+': return operandA + operandB;
                case '-': return operandA - operandB;
                case '×': return operandA * operandB;
                case '^': return Math.Round(Math.Pow(operandA, operandB), 5);
                case '÷':
                    if (operandB == 0) {
                        ResultStr = "ERROR:\n0 can't be used as divisor.";
                        CaculateStr = "";
                        exprLegit = false;
                        return 0;
                    }
                    else return operandA / operandB;
                default: return 0;
            }
        }

        private char Precede(char op1, char op2) {
            /*  given two operator: op1 & op2, return the priority
             *      +   -   ×   ÷   ^   #   op2
             *  +   >   >   <   <   <   >
             *  -   >   >   <   <   <   >
             *  ×   >   >   >   >   <   >
             *  ÷   >   >   >   >   <   >
             *  ^   >   >   >   >   >   >
             *  #   <   <   <   <   <   =
             *  op1
             */
            switch (op1) {
                case '+':
                    switch(op2) {
                        case '+': return '>';
                        case '-': return '>';
                        case '×': return '<';
                        case '÷': return '<';
                        case '^': return '<';
                        case '#': return '>';
                        default: return '\0';
                    }
                case '-':
                    switch (op2) {
                        case '+': return '>';
                        case '-': return '>';
                        case '×': return '<';
                        case '÷': return '<';
                        case '^': return '<';
                        case '#': return '>';
                        default: return '\0';
                    }
                case '×':
                    switch (op2) {
                        case '+': return '>';
                        case '-': return '>';
                        case '×': return '>';
                        case '÷': return '>';
                        case '^': return '<';
                        case '#': return '>';
                        default: return '\0';
                    }
                case '÷':
                    switch (op2) {
                        case '+': return '>';
                        case '-': return '>';
                        case '×': return '>';
                        case '÷': return '>';
                        case '^': return '<';
                        case '#': return '>';
                        default: return '\0';
                    }
                case '^':
                    switch (op2) {
                        case '+': return '>';
                        case '-': return '>';
                        case '×': return '>';
                        case '÷': return '>';
                        case '^': return '>';
                        case '#': return '>';
                        default: return '\0';
                    }
                case '#':
                    switch (op2) {
                        case '+': return '<';
                        case '-': return '<';
                        case '×': return '<';
                        case '÷': return '<';
                        case '^': return '<';
                        case '#': return '=';
                        default: return '\0';
                    }
                default: return '\0';
             }
        }

        private string FuncRecognition(string str) {
            // find all the funcion name in the expresison and replace them with a single char
            /* squa     -> @
               cube     -> #
               ln       -> l
               fact     -> f
               sqrt     -> $
               sin      -> s
               cos      -> c
               tan      -> t
               arcsin   -> x
               arccos   -> y
               arctan   -> z
            */
            str = str.Replace("squa", "@").Replace("cube", "#");
            str = str.Replace("ln", "l").Replace("fact", "f").Replace("sqrt", "$");
            str = str.Replace("sin", "s").Replace("cos", "c");
            str = str.Replace("tan", "t").Replace("arcsin", "x");
            str = str.Replace("arccos", "y").Replace("arctan", "z");
            return str;
        }

        private int Fact(int n) {
            if (n <= 0) return 1;
            if (n == 1) return 1;
            else return n * Fact(n - 1);
        }

        private bool IsOP(char op) {
            switch (op) {
                case '+':
                case '-':
                case '×':
                case '÷':
                case '^':
                case '#': return true;
                default: return false;
            }
        }
        
        private bool IfAnyOP(string str) {
            char[] ops = { '+', '-', '×', '÷', '^' };
            if (str.IndexOfAny(ops) < 0) return false;
            else return true;
        }

        public Expression() {
            ExprStr = "";
            ResultStr = "";
            CaculateStr = "";
            exprLegit = true;
            haveCounted = false;
            result = 0;
        }

        public void Add(string BtnStr) {
            exprLegit = true;
            if(haveCounted) {
                haveCounted = false;
                ExprStr = "";
            }
            char []op = BtnStr.ToCharArray();
            if(IsOP(op[0])) {
                if (ExprStr.EndsWith("+") || ExprStr.EndsWith("-") || ExprStr.EndsWith("×") || ExprStr.EndsWith("÷"))
                    ExprStr = ExprStr.Remove(ExprStr.Length - 1);
            }
            ExprStr = ExprStr + BtnStr;
        }

        public bool IsExprLegit() {
            return exprLegit;
        }
        
        public void Clear() {
            ExprStr = "";
            ResultStr = "";
            CaculateStr = "";
            exprLegit = true;
            haveCounted = false;
            result = 0;
        }

        public void BackSpace() {
            int pos = ExprStr.Length - 1;
            if (pos >= 0) ExprStr = ExprStr.Remove(pos);
        }

        public void Count() {
            CaculateStr = ExprStr;
            CaculateStr = FuncRecognition(CaculateStr);
            haveCounted = true;
            double tempResult;
            string tempStr;
            char []c;
            bool isFuncExit;
            int LPIndex, RPIndex;
            while (true) {
                // find the innermost parentheses
                LPIndex = CaculateStr.LastIndexOf('(');
                RPIndex = CaculateStr.IndexOf(')', LPIndex < 0? 0 : LPIndex);
                if (LPIndex < 0 && RPIndex < 0) {
                    // no parenthese in the expression
                    if(!IfAnyOP(CaculateStr)) {
                        Double.TryParse(CaculateStr, out result);
                        ResultStr = CaculateStr;
                        CaculateStr = "";
                        return;
                    }
                    result = CountWithoutParen(CaculateStr);
                    if (!exprLegit) {
                        // more code here


                        return;
                    }
                    ResultStr = result.ToString();
                    CaculateStr = "";
                    return;
                }
                else if ((LPIndex < 0 && RPIndex >= 0) || (LPIndex >= 0) && (RPIndex < 0)) {
                    // only find left or right parenthese
                    ResultStr = "ERROR:\nCannot match parentheses.\nPlease check your input again.";
                    CaculateStr = "";
                    result = 0;
                    exprLegit = false;
                    return;
                }
                else if ((RPIndex - LPIndex) <= 1) {
                    // no expression in the couple of parentheses or wrong position of the two parenthese
                    ResultStr = "ERROR:\nNo expr in innermost parentheses.";
                    CaculateStr = "";
                    result = 0;
                    exprLegit = false;
                    return;
                }
                else {
                    // there IS a couple of parentheses and there IS a expression in them

                    // get the expression in the parentheses
                    tempStr = CaculateStr.Substring(LPIndex + 1, RPIndex - LPIndex - 1);
                    if (IfAnyOP(tempStr)) {
                        // count the expression in the parentheses
                        tempResult = CountWithoutParen(tempStr);
                        if (!exprLegit) {
                            // maybe more code here

                            return;
                        }
                    }
                    else {
                        // no operator exits in tempStr
                        Double.TryParse(tempStr, out tempResult);
                    }
                    if (LPIndex == 0) c = "a".ToCharArray();
                    else c = CaculateStr.Substring(LPIndex - 1, 1).ToCharArray();        // find the funcion left to lparen
                    isFuncExit = true;
                    switch(c[0]) {
                        case '@':
                            // square 
                            tempResult = tempResult * tempResult;      
                            break;
                        case '#':
                            // cube
                            tempResult = tempResult * tempResult * tempResult;
                            break;
                        case 'l':
                            // ln
                            if(tempResult <= 0) {
                                ResultStr = "ERROR:\nIn function ln(x), x can't be negative or 0.";
                                CaculateStr = "";
                                result = 0;
                                exprLegit = false;
                                return;
                            }
                            tempResult = Math.Round(Math.Log(tempResult), 5);
                            break;
                        case 'f':
                            // factorial
                            if(tempResult < 0) {
                                ResultStr = "ERROR:\nIn function fact(n), n can't be negative.";
                                CaculateStr = "";
                                result = 0;
                                exprLegit = false;
                                return;
                            }
                            tempResult = Fact((int)tempResult);
                            break;
                        case 's':
                            // sin
                            tempResult = Math.Round(Math.Sin(tempResult), 5);
                            break;
                        case 'c':
                            // cos 
                            tempResult = Math.Round(Math.Cos(tempResult), 5);
                            break;
                        case 't':
                            // tan
                            tempResult = Math.Round(Math.Tan(tempResult), 5);
                            break;
                        case 'x':
                            // arcsin
                            if(tempResult < -1 || tempResult > 1) {
                                ResultStr = "ERROR:\nIn function arcsin(x), x must between -1 and 1.";
                                CaculateStr = "";
                                result = 0;
                                exprLegit = false;
                                return;
                            }
                            tempResult = Math.Round(Math.Asin(tempResult), 5);
                            break;
                        case 'y':
                            // arccos
                            if (tempResult < -1 || tempResult > 1) {
                                ResultStr = "ERROR:\nIn function arccos(x), x must between -1 and 1.";
                                CaculateStr = "";
                                result = 0;
                                exprLegit = false;
                                return;
                            }
                            tempResult = Math.Round(Math.Acos(tempResult), 5);
                            break;
                        case 'z':
                            // arctan
                            tempResult = Math.Round(Math.Atan(tempResult), 5);
                            break;
                        default:
                            // no funcion
                            isFuncExit = false;
                            break;
                    }   // switch(c[0])
                    if(isFuncExit) {
                        CaculateStr = CaculateStr.Remove(LPIndex - 1, RPIndex - LPIndex + 2);
                        CaculateStr = CaculateStr.Insert(LPIndex - 1, tempResult.ToString());
                    }
                    else {
                        CaculateStr = CaculateStr.Remove(LPIndex, RPIndex - LPIndex + 1);
                        CaculateStr = CaculateStr.Insert(LPIndex, tempResult.ToString());
                    }
                }
            }   // while(true)
        }   // public void Count()

    }   // class Expression

    public sealed partial class MainPage : Page {


        public MainPage() {
            this.InitializeComponent();
        }

        Expression expr = new Expression();

        private void BtnClickEual(object sender, RoutedEventArgs e) {
            expr.Count();
            TxtExpr.Text = expr.ExprStr;
            TxtResult.Text = expr.ResultStr;
        }

        private void BtnClickSquare(object sender, RoutedEventArgs e) {
            expr.Add("squa");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickCube(object sender, RoutedEventArgs e) {
            expr.Add("cube");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickLn(object sender, RoutedEventArgs e) {
            expr.Add("ln");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickFactorial(object sender, RoutedEventArgs e) {
            expr.Add("fact");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickConstE(object sender, RoutedEventArgs e) {
            expr.Add("2.71828");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickConstPAI(object sender, RoutedEventArgs e) {
            expr.Add("3.14159");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickLParen(object sender, RoutedEventArgs e) {
            expr.Add("(");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickPower(object sender, RoutedEventArgs e) {
            expr.Add("^");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickSqrt(object sender, RoutedEventArgs e) {
            expr.Add("sqrt");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickClearAll(object sender, RoutedEventArgs e) {
            expr.Clear();
            TxtExpr.Text = expr.ExprStr;
            TxtResult.Text = expr.ResultStr;
        }

        private void BtnClickNum7(object sender, RoutedEventArgs e) {
            expr.Add("7");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickNum4(object sender, RoutedEventArgs e) {
            expr.Add("4");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickNum1(object sender, RoutedEventArgs e) {
            expr.Add("1");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickRParen(object sender, RoutedEventArgs e) {
            expr.Add(")");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickSin(object sender, RoutedEventArgs e) {
            expr.Add("sin");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickArcsin(object sender, RoutedEventArgs e) {
            expr.Add("arcsin");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickNum8(object sender, RoutedEventArgs e) {
            expr.Add("8");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickNum5(object sender, RoutedEventArgs e) {
            expr.Add("5");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickNum2(object sender, RoutedEventArgs e) {
            expr.Add("2");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickNum0(object sender, RoutedEventArgs e) {
            expr.Add("0");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickCos(object sender, RoutedEventArgs e) {
            expr.Add("cos");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickArccos(object sender, RoutedEventArgs e) {
            expr.Add("arccos");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickBack(object sender, RoutedEventArgs e) {
            expr.BackSpace();
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickNum9(object sender, RoutedEventArgs e) {
            expr.Add("9");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickNum6(object sender, RoutedEventArgs e) {
            expr.Add("6");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickNum3(object sender, RoutedEventArgs e) {
            expr.Add("3");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickDot(object sender, RoutedEventArgs e) {
            expr.Add(".");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickTan(object sender, RoutedEventArgs e) {
            expr.Add("tan");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickArctan(object sender, RoutedEventArgs e) {
            expr.Add("arctan");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickDiv(object sender, RoutedEventArgs e) {
            expr.Add("÷");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickMul(object sender, RoutedEventArgs e) {
            expr.Add("×");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickMinus(object sender, RoutedEventArgs e) {
            expr.Add("-");
            TxtExpr.Text = expr.ExprStr;
        }

        private void BtnClickPlus(object sender, RoutedEventArgs e) {
            expr.Add("+");
            TxtExpr.Text = expr.ExprStr;
        }

    }
}
