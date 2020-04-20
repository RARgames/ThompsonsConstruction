///Thompson's NFA construction algorithm and string match checking for regex
///Implemented by Piotr Pakulski for ECOTE project

using System;
using System.Collections.Generic;

namespace ThompsonAlg
{
    public class Transition //Class for transition structure used by NFA
    {
        public int fromStateId; //Start state of tranisiton
        public int toStateId; //End state of transition
        public char symbol; //Symbol with which the transition happens - E is used for Epsilon transition

        public Transition(int from, int to, char sym) //Transition class constructor
        {
            fromStateId = from;
            toStateId = to;
            symbol = sym;
        }

        public override string ToString() //Overridden method ToString() for showing how transition looks like
        {
            return $"({fromStateId}, {symbol}, {toStateId})";
        }
    }
    public static class Regex //Class used for checking Regex input
    {
        public static bool IsValidRegex(string regex)
        {
            if (string.IsNullOrEmpty(regex)) //If regex is empty
            {
                return false;
            }

            bool invalidFlag1 = false, invalidFlag2 = false; //invalidFlag1 is a flag for 2 consecitive ** and invalidFlag2 is a flag  for 2 consecutive ||, if any of these flags is true, the regex is invalid
            int bracketCount = 0; //int for checking parenthesis count, if it's not equal to 0 at the end, the regex is invalid
            foreach (var c in regex)
            {
                if (c != '(' && c != ')' && c != '*' && c != '|' && !Char.IsDigit(c) && !Char.IsLower(c)) //All chars should be lower letters, digits or special symbols like: '(', ')', '*', '|'
                {
                    return false;
                }
                if (c == '*')
                {
                    if (invalidFlag1)
                    {
                        return false;
                    }
                    invalidFlag1 = true;
                }
                else
                {
                    invalidFlag1 = false;
                }
                if (c == '|')
                {
                    if (invalidFlag2)
                    {
                        return false;
                    }
                    invalidFlag2 = true;
                }
                else
                {
                    invalidFlag2 = false;
                }
                if (c == '(')
                {
                    bracketCount++;
                }
                else if (c == ')')
                {
                    bracketCount--;
                }
            }

            if (!Char.IsLetterOrDigit(regex.ToCharArray()[0]) && regex.ToCharArray()[0] != '(') //First char should be a letter/digit or opening bracket, otherwise regex is invalid
            {
                return false;
            }

            if (bracketCount != 0)
            {
                return false;
            }
            return true;
        }
    }

    public class NFA //Class for storing NFA information
    {
        private List<int> statesIdList; //List of int that stores all possible states of NFA
        private List<Transition> transitionsList; //List of all possible transitions of NFA
        private int finalStateId; //Id of a final state of NFA

        private NFA(int size) //Constructor for empty NFA of a given size
        {
            statesIdList = new List<int>();
            transitionsList = new List<Transition>();
            SetStateSize(size);
            finalStateId = 0;
        }

        private NFA(char c) //Constructor for complete NFA with 1
        {
            statesIdList = new List<int>();
            transitionsList = new List<Transition>();
            transitionsList.Add(new Transition(0, 1, c));
            SetStateSize(2);
            finalStateId = 1;
        }

        private void SetStateSize(int size) //BUG TUTAJ komenty
        {
            for (int i = 0; i < size; i++)
            {
                statesIdList.Add(i);
            }
        }

        public override string ToString()
        {
            string output = "\n";
            foreach (var t in transitionsList)
            {
                output += t.ToString() + "\n";
            }
            output += "Start state: 0  |  Final state: " + finalStateId;
            return output;
        }

        private static NFA Concat(NFA part1, NFA part2)
        {
            NFA result = new NFA(part1.statesIdList.Count + part2.statesIdList.Count - 2);

            result.finalStateId = part1.statesIdList.Count + part2.statesIdList.Count - 2; //Set final state id

            result.transitionsList = new List<Transition>(part1.transitionsList); //Set result transitions list to part1 transitions list 
            foreach (var t in part2.transitionsList)
            {
                result.transitionsList.Add(new Transition(t.fromStateId + part1.statesIdList.Count - 1, t.toStateId + part1.statesIdList.Count - 1, t.symbol)); //Copy part2 transitions to part1
            }

            result.statesIdList = new List<int>(part1.statesIdList); //Set states id list to part1 states id list 

            bool first = true;
            foreach (var i in part2.statesIdList)
            {
                if (first)
                {
                    first = false;
                    continue;
                }
                result.statesIdList.Add(i + part1.statesIdList.Count + 1); //Copy part2 state list into part1 (without first element)
            }

            return result;
        }

        private static NFA Star(NFA part)
        {
            NFA result = new NFA(part.statesIdList.Count + 2);

            result.finalStateId = part.statesIdList.Count + 1;

            result.transitionsList.Add(new Transition(0, 1, 'E')); //Create new start transition
            foreach (var t in part.transitionsList)
            {
                result.transitionsList.Add(new Transition(t.fromStateId + 1, t.toStateId + 1, t.symbol)); //Copy all existing transitions
            }
            result.transitionsList.Add(new Transition(part.statesIdList.Count, part.statesIdList.Count + 1, 'E')); //Add transition from n final state to new final state
            result.transitionsList.Add(new Transition(part.statesIdList.Count, 1, 'E')); //Add loop transition from last state of n to initial state of n
            result.transitionsList.Add(new Transition(0, part.statesIdList.Count + 1, 'E')); //Add transition from new inital state to new final state

            return result;
        }

        private static NFA Union(NFA part1, NFA part2)
        {
            NFA result = new NFA(part1.statesIdList.Count + part2.statesIdList.Count + 2);

            result.transitionsList.Add(new Transition(0, 1, 'E')); //Add new initial transition
            foreach (var t in part1.transitionsList)
            {
                result.transitionsList.Add(new Transition(t.fromStateId + 1, t.toStateId + 1, t.symbol)); //Copy exisiting transitions from part1
            }
            result.transitionsList.Add(new Transition(part1.statesIdList.Count, part1.statesIdList.Count + part2.statesIdList.Count + 1, 'E')); //Add transition from last state of part1 to new final state

            result.transitionsList.Add(new Transition(0, part1.statesIdList.Count + 1, 'E')); //Add transition from start to part2 start
            foreach (var t in part2.transitionsList)
            {
                result.transitionsList.Add(new Transition(t.fromStateId + part1.statesIdList.Count + 1, t.toStateId + part1.statesIdList.Count + 1, t.symbol)); //Copy exisiting transitions from part2
            }
            result.transitionsList.Add(new Transition(part2.statesIdList.Count + part1.statesIdList.Count, part1.statesIdList.Count + part2.statesIdList.Count + 1, 'E')); //Add transition from end of part2 to new final state

            result.finalStateId = part1.statesIdList.Count + part2.statesIdList.Count + 1; //Set final to the new one
            return result;
        }

        private static int OperatorPrecedence(char c)
        {
            switch (c)
            {
                case '|':
                    return 0;
                case '.':
                    return 1;
                case '*':
                    return 2;
                default:
                    return -1;
            }
        }

        private static string ToPostfix(string regex)
        {
            string addedDot = "";
            for (int i = 0; i < regex.Length; i++)
            {
                char c = regex[i];
                addedDot += c;
                if (c == '(' || c == '|')
                {
                    continue;
                }
                if (i < regex.Length - 1)
                {
                    char nextC = regex[i + 1];
                    if (nextC == '*' || nextC == '|' || nextC == ')')
                    {
                        continue;
                    }
                    addedDot += '.';
                }
            }

            string output = "";
            Stack<char> operatorStack = new Stack<char>();

            foreach (var c in addedDot)
            {
                if (c == '.' || c == '|' || c == '*')
                {
                    while (operatorStack.Count != 0 && operatorStack.Peek() != '(' && OperatorPrecedence(operatorStack.Peek()) >= OperatorPrecedence(c))
                    {
                        output += operatorStack.Pop();
                    }
                    operatorStack.Push(c);
                }
                else if (c == '(' || c == ')')
                {
                    if (c == '(')
                    {
                        operatorStack.Push(c);
                    }
                    else
                    {
                        while (operatorStack.Count > 1 && operatorStack.Peek() != '(')
                        {
                            output += operatorStack.Pop();
                        }
                        operatorStack.Pop();
                    }
                }
                else
                {
                    output += c;
                }
            }
            while (operatorStack.Count != 0)
            {
                output += operatorStack.Pop();
            }

            return output;
        }


        public static NFA Create(string regex)
        {
            if (!Regex.IsValidRegex(regex))
            {
                Console.WriteLine("Invalid regex!");
                return null; //Return null instead of error to allow automated tests
            }

            NFA nfa;
            Stack<NFA> nfaStack = new Stack<NFA>();
            string postfix = ToPostfix(regex);
            foreach (var c in postfix)
            {
                switch (c)
                {
                    case '*':
                        nfaStack.Push(Star(nfaStack.Pop()));
                        break;
                    case '|':
                        nfa = nfaStack.Pop();
                        nfaStack.Push(Union(nfaStack.Pop(), nfa));
                        break;
                    case '.':
                        nfa = nfaStack.Pop();
                        nfaStack.Push(Concat(nfaStack.Pop(), nfa));
                        break;
                    default:
                        nfaStack.Push(new NFA(c));
                        break;
                }
            }
            if (string.IsNullOrEmpty(postfix))
            {
                return new NFA('E');
            }

            return nfaStack.Pop();
        }

        private List<int>[] nextStateIdList;
        private List<Transition> usedETransitions;

        public bool CheckString(string str)
        {
            int currentStateId;

            nextStateIdList = new List<int>[str.Length + 1];

            for (int k = 0; k < str.Length + 1; k++)
            {
                if (k == 0)
                {
                    nextStateIdList[0] = new List<int> { 0 }; //Set initial state
                }
                else
                {
                    nextStateIdList[k] = new List<int>();
                }
            }
            usedETransitions = new List<Transition>();

            for (int i = 0; i < str.Length + 1; i++)
            {
                usedETransitions.Clear();

                for (int j = 0; j < nextStateIdList[i].Count; j++)
                {
                    currentStateId = nextStateIdList[i][j];

                    if (CheckOtherPos(currentStateId, str, i, false))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool CheckOtherPos(int currentStateId, string str, int i, bool usedSymbolTransition)
        {
            foreach (var t in transitionsList)
            {
                if (t.fromStateId == currentStateId)
                {
                    bool isEmptyStr = string.IsNullOrEmpty(str);
                    bool isEpsTrans = (t.symbol == 'E');
                    bool isCurrCharTrans;
                    bool isTestingLastChar;
                    if (!isEmptyStr)
                    {
                        isTestingLastChar = ((i + 1) == str.Length);
                        isCurrCharTrans = (t.symbol == str.ToCharArray()[i]);
                    }
                    else
                    {
                        isTestingLastChar = true;
                        isCurrCharTrans = isEpsTrans;
                    }
                    if (isCurrCharTrans && !isEpsTrans)
                    {
                        if (usedSymbolTransition)
                        {
                            return false;
                        }
                        usedSymbolTransition = true;
                    }

                    if (isEpsTrans || isCurrCharTrans)
                    {
                        if (t.toStateId == finalStateId && isTestingLastChar)
                        {
                            if (usedSymbolTransition || isEmptyStr)
                            {
                                return true;
                            }
                        }
                    }
                    
                    if (isCurrCharTrans)
                    {
                        if (!usedSymbolTransition)
                        {
                            if (CheckOtherPos(t.toStateId, str, i + 1, usedSymbolTransition)) { return true; }
                        }
                        else
                        {
                            if (isTestingLastChar)
                            {
                                if (CheckOtherPos(t.toStateId, str, i, usedSymbolTransition)) { return true; }
                            }
                            else
                            {
                                nextStateIdList[i + 1].Add(t.toStateId);
                            }
                        }
                    }
                    else if (isEpsTrans && usedETransitions.FindAll(e => e == t).Count < 2) //2nd part to only allow 2 reps of e transform
                    {
                        usedETransitions.Add(t);
                        if (CheckOtherPos(t.toStateId, str, i, usedSymbolTransition)) { return true; }
                    }
                }
            }
            return false;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            AutoTester at = new AutoTester();
            Console.WriteLine("****** AUTO TEST START ******");
            Console.WriteLine("regex : string : match : STATUS");
            Console.WriteLine();
            at.AutoTest("ab", "a", false);
            at.AutoTest("ab", "", false);
            at.AutoTest("ab", "aa", false);
            at.AutoTest("ab", "aaa", false);
            at.AutoTest("ab", "ab", true);
            at.AutoTest("ab", "b", false);
            at.AutoTest("ab", "bb", false);
            Console.WriteLine();
            at.AutoTest("a", "a", true);
            at.AutoTest("a", "", false);
            at.AutoTest("a", "aa", false);
            at.AutoTest("a", "aaa", false);
            at.AutoTest("a", "ab", false);
            at.AutoTest("a", "b", false);
            at.AutoTest("a", "bb", false);
            Console.WriteLine();
            at.AutoTest("a*", "", true);
            at.AutoTest("a*", "a", true);
            at.AutoTest("a*", "aa", true);
            at.AutoTest("a*", "aaa", true);
            at.AutoTest("a*", "ab", false);
            at.AutoTest("a*", "b", false);
            at.AutoTest("a*", "bb", false);
            Console.WriteLine();
            at.AutoTest("a*b", "", false);
            at.AutoTest("a*b", "a", false);
            at.AutoTest("a*b", "aa", false);
            at.AutoTest("a*b", "aaa", false);
            at.AutoTest("a*b", "ab", true);
            at.AutoTest("a*b", "b", true);
            at.AutoTest("a*b", "bb", false);
            at.AutoTest("a*b", "aaaaab", true);
            Console.WriteLine();
            at.AutoTest("a|b", "", false);
            at.AutoTest("a|b", "a", true);
            at.AutoTest("a|b", "aa", false);
            at.AutoTest("a|b", "aaa", false);
            at.AutoTest("a|b", "ab", false);
            at.AutoTest("a|b", "b", true);
            Console.WriteLine();
            at.AutoTest("a*|b", "", true);
            at.AutoTest("a*|b", "a", true);
            at.AutoTest("a*|b", "aaa", true);
            at.AutoTest("a*|b", "b", true);
            at.AutoTest("a*|b", "ba", false);
            at.AutoTest("a*|b", "ab", false);
            Console.WriteLine();
            at.AutoTest("a(ab)*c", "aababababc", true);
            at.AutoTest("a(ab)*c", "aababababcc", false);
            at.AutoTest("a(ab)*c", "ac", true);
            at.AutoTest("a(ab)*c", "", false);
            at.AutoTest("a(ab)*c", "c", false);
            Console.WriteLine();
            at.AutoTest("(a*)|b", "", true);
            at.AutoTest("(a*)|b", "a", true);
            at.AutoTest("(a*)|b", "aa", true);
            at.AutoTest("(a*)|b", "aaa", true);
            at.AutoTest("(a*)|b", "ab", false);
            at.AutoTest("(a*)|b", "b", true);
            at.AutoTest("(a*)|b", "bb", false);
            Console.WriteLine();
            at.AutoTest("((a*b)*c)|b", "aaaaabaabc", true);
            at.AutoTest("((a*b)*c)|b", "c", true);
            at.AutoTest("((a*b)*c)|b", "ca", false);
            at.AutoTest("((a*b)*c)|b", "cb", false);
            at.AutoTest("((a*b)*c)|b", "cc", false);
            at.AutoTest("((a*b)*c)|b", "", false);
            Console.WriteLine();
            at.AutoTest("a*b*c*", "abb", true);
            at.AutoTest("a*b*c*", "aaaaabb", true);
            at.AutoTest("a*b*c*", "aaaaac", true);
            at.AutoTest("a*b*c*", "aaaaabbccccc", true);
            at.AutoTest("a*b*c*", "ccc", true);
            Console.WriteLine();
            at.AutoTest("(a*)", "a", true);
            at.AutoTest("(a*)", "aaa", true);
            at.AutoTest("(a*)b", "aaab", true);
            at.AutoTest("b(a*)", "b", true);
            at.AutoTest("b(a*)", "ba", true);
            Console.WriteLine();
            at.AutoTest("()", "", true);
            at.AutoTest("()", "a", false);
            Console.WriteLine();
            at.AutoTest("(c|b(a*))*", "cccbbaaaaabab", true);
            at.AutoTest("(c|b(a*))*", "cccbbaaaaababc", true);
            at.AutoTest("(c|(a))*", "caaaaaccc", true);
            Console.WriteLine();
            at.AutoTest("(0|(1(01*(00)*0)*1)*)*", "", true);
            at.AutoTest("(0|(1(01*(00)*0)*1)*)*", "0", true);
            at.AutoTest("(0|(1(01*(00)*0)*1)*)*", "00", true);
            at.AutoTest("(0|(1(01*(00)*0)*1)*)*", "11", true);
            at.AutoTest("(0|(1(01*(00)*0)*1)*)*", "000", true);
            at.AutoTest("(0|(1(01*(00)*0)*1)*)*", "011", true);
            at.AutoTest("(0|(1(01*(00)*0)*1)*)*", "110", true);
            at.AutoTest("(0|(1(01*(00)*0)*1)*)*", "0000", true);
            at.AutoTest("(0|(1(01*(00)*0)*1)*)*", "0011", true);
            at.AutoTest("(0|(1(01*(00)*0)*1)*)*", "0110", true);
            at.AutoTest("(0|(1(01*(00)*0)*1)*)*", "1001", true);
            at.AutoTest("(0|(1(01*(00)*0)*1)*)*", "1100", true);
            at.AutoTest("(0|(1(01*(00)*0)*1)*)*", "1111", true);
            at.AutoTest("(0|(1(01*(00)*0)*1)*)*", "00000", true);
            at.AutoTest("(0|(1(01*(00)*0)*1)*)*", "00001", false);
            at.AutoTest("(0|(1(01*(00)*0)*1)*)*", "0011101", false);
            at.AutoTest("(0|(1(01*(00)*0)*1)*)*", "1011100000011100000110111000000111000001", true);
            Console.WriteLine("Invalid regex tests: ");
            at.AutoTest("((a*)", "", true);
            at.AutoTest("(a*))", "", true);
            at.AutoTest("|a", "", true);
            at.AutoTest("|(a)", "", true);
            at.AutoTest("a**", "aaaa", true);
            at.AutoTest("a**", "", true);
            at.AutoTest("a**", "b", true);
            at.AutoTest("a||b", "a", true);
            at.AutoTest("a||b", "b", true);
            Console.WriteLine();
            at.StatusFilterDisplay();
            Console.WriteLine("****** AUTO TEST END ******");
            Console.WriteLine();
            Console.WriteLine("Input regex");
            var input = Console.ReadLine();
            NFA inputNFA = NFA.Create(input);
            if (inputNFA != null)
            {
                Console.WriteLine(inputNFA.ToString());

                while (true)
                {
                    Console.WriteLine("=====================");
                    Console.WriteLine("Input string to check");
                    input = Console.ReadLine();
                    Console.WriteLine(inputNFA.CheckString(input));
                }
            }
        }
    }

    public class AutoTester
    {
        private int testCount = 0, failCount = 0;
        private bool displayNFAOverride = false;

        public AutoTester(bool displayNFA = false)
        {
            displayNFAOverride = displayNFA;
        }

        public void AutoTest(string regex, string str, bool shouldReturn, bool displayNFA = false)
        {
            NFA autoTest = NFA.Create(regex);

            testCount++;
            if (autoTest != null)
            {
                if (displayNFA || displayNFAOverride)
                {
                    Console.WriteLine(autoTest.ToString());
                }
                string status = "FAILED";
                bool testStatus = autoTest.CheckString(str);
                if (shouldReturn == testStatus)
                {
                    status = "OK";
                }
                Console.WriteLine(regex + " : " + str + " : " + testStatus + " :   " + status);
                StatusFilter(status);
            }
        }

        private void StatusFilter(string status)
        { 
            if (string.Compare(status, "OK") != 0)
            {
                failCount++;
            }
        }

        public void StatusFilterDisplay()
        {
            if (failCount > 0)
            {
                Console.WriteLine("[AutoTest] " + failCount + "/" + testCount + " FAILED");
            }
            else
            {
                Console.WriteLine("[AutoTest] ALL " + testCount + " TESTS PASSED");
            }
        }
    }
}

//TODO add comments
