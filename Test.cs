using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;

namespace csharp_natural_sort
{
    [MemoryDiagnoser]
    [RankColumn]
    public class ComparerBenchmark
    {
        const string STR1 = "asrgfsadf12421";
        const string STR2 = "asrgfsadf12321";

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int StrCmpLogicalW(string s1, string s2);

        [Benchmark(Description = "WinAPI")]
        public void TestWinAPI() => StrCmpLogicalW(STR1, STR2);


        [Benchmark(Description = "Old")]
        public void test1Old() => CompareOld(STR1, STR2);

        [Benchmark(Description = "Span")]
        public void testSpan() => CompareSpan(STR1, STR2);


        [Benchmark(Description = "New")]
        public void testNew() => CompareNew(STR1, STR2);


        [Benchmark(Description = "Unsefe")]
        public void testUnsafe() => CompareUnsafe(STR1, STR2);


        [Benchmark(Description = "UnsefeIsDigit")]
        public void testUnsafeIsDigit() => CompareUnsafeIsDigit(STR1, STR2);


        [Benchmark(Description = "Regexp")]
        public void testRegexp() => CompareRegex(STR1, STR2);

        
        /// <summary>
        /// Мой самый первый реализованный алгоритм. Тут числа сравниваются как строка, 
        /// но условиям натуральной сортировки удовлетворяет
        /// </summary>        
        public static int CompareOld(string s1, string s2)
        {
            (var ne1, var ne2) = (string.IsNullOrEmpty(s1), string.IsNullOrEmpty(s2));

            if (ne1 && ne2) return 0;
            if (ne1) return -1;
            if (ne2) return 1;

            (var i1, var i2) = (0, 0);
            (var len1, var len2) = (s1.Length, s2.Length);

            while (i1 < len1)
            {
                if (i2 == len2) return 1;

                (var a, var b) = (s1[i1], s2[i2]);
                (var d1, var d2) = (char.IsDigit(a), char.IsDigit(b));

                if (d1 && d2)
                {
                    while (i1 < len1 && s1[i1] == '0') i1++;
                    while (i2 < len2 && s2[i2] == '0') i2++;

                    (var m, var n) = (i1, i2);

                    while (m < len1 && char.IsDigit(s1[m])) m++;
                    while (n < len2 && char.IsDigit(s2[n])) n++;

                    (var l1, var l2) = (m - i1, n - i2);

                    if (l1 != l2) return l1 > l2 ? 1 : -1;

                    while (i1 < m)
                    {
                        if (s1[i1] != s2[i2]) return (s1[i1] > s2[i2]) ? 1 : -1;

                        i1++; i2++;
                    }
                }
                else
                {
                    if (a != b) return (a > b) ? 1 : -1;

                    i1++; i2++;
                }
            }

            return (i2 == len2) ? 0 : -1;
        }

        /// <summary>
        /// Сравнение с использованием Span
        /// </summary>        
        public static int CompareSpan(string s1, string s2)
        {
            (var ne1, var ne2) = (string.IsNullOrEmpty(s1), string.IsNullOrEmpty(s2));

            if (ne1 && ne2) return 0;
            if (ne1) return -1;
            if (ne2) return 1;


            (var i1, var i2) = (0, 0);

            var span1 = s1.AsSpan();
            var span2 = s2.AsSpan();

            (var len1, var len2) = (s1.Length, s2.Length);


            while (i1 < len1)
            {
                if (i2 == len2) return 1;
                if (char.IsDigit(span1[i1]) && char.IsDigit(span2[i2]))
                {
                    while (i1 < len1 && span1[i1] == '0') i1++;
                    while (i2 < len2 && span2[i2] == '0') i2++;

                    (var j1, var j2) = (i1, i2);

                    while (j1 < len1 && char.IsDigit(span1[j1])) j1++;
                    while (j2 < len2 && char.IsDigit(span2[j2])) j2++;

                    (var l1, var l2) = (j1 - i1, j2 - i2);

                    if (l1 != l2) return (l1 > l2) ? 1 : -1;

                    var cmp = span1.Slice(i1, l1).SequenceCompareTo(span2.Slice(i2, l2));
                    if (cmp != 0) return cmp;

                    i1 = j1; i2 = j1;
                }
                else
                {
                    if (span1[i1] != span2[i2]) return span1[i1] > span2[i2] ? 1 : -1;

                    i1++; i2++;
                }
            }

            return (i2 == len2) ? 0 : -1;
        }

        /// <summary>
        /// Алгоритм с преобразованием строкового числа в число
        /// </summary>        
        public static int CompareNew(string str1, string str2)
        {
            (var ne1, var ne2) = (string.IsNullOrEmpty(str1), string.IsNullOrEmpty(str2));

            if (ne1 && ne2) return 0;
            if (ne1) return -1;
            if (ne2) return 1;

            (var len1, var len2) = (str1.Length, str2.Length);
            (var i1, var i2) = (0, 0);

            while (i1 < len1)
            {
                if (i2 == len2) return 1;
                if (char.IsDigit(str1[i1]) && char.IsDigit(str2[i2]))
                {
                    (var num1, var num2) = (str1[i1++] - '0', str2[i2++] - '0');

                    while (i1 < len1 && char.IsDigit(str1[i1]))
                        num1 = num1 * 10 + str1[i1++] - '0';

                    while (i2 < len2 && char.IsDigit(str2[i2]))
                        num2 = num2 * 10 + str2[i2++] - '0';

                    if (num1 != num2) return num1 > num2 ? 1 : -1;
                }
                else
                {
                    if (str1[i1] != str2[i2]) return str1[i1] > str2[i2] ? 1 : -1;

                    i1++; i2++;
                }
            }

            return (i2 == len2) ? 0 : -1;
        }

        /// <summary>
        /// более низкоуровневое программирование
        /// </summary>        
        public unsafe static int CompareUnsafe(string s1, string s2)
        {
            (var ne1, var ne2) = (string.IsNullOrEmpty(s1), string.IsNullOrEmpty(s2));

            if (ne1 && ne2) return 0;
            if (ne1) return -1;
            if (ne2) return 1;

            fixed (char* pointer1 = s1, pointer2 = s2)
            {
                var p1 = pointer1;
                var p2 = pointer2;

                while (*p1 != 0)
                {
                    if (*p2 == 0) return 1;

                    if (*p1 >= '0' && *p1 <= '9' && *p2 >= '0' && *p2 <= '9')
                    {
                        (var num1, var num2) = (*p1 - '0', *p2 - '0');
                        p1++; p2++;

                        while (*p1 >= '0' && *p1 <= '9')
                        {
                            num1 = 10 * num1 + *p1 - '0';
                            p1++;
                        }

                        while (*p2 >= '0' && *p2 <= '9')
                        {
                            num2 = 10 * num2 + *p2 - '0';
                            p2++;
                        }

                        if (num1 != num2) return num1 > num2 ? 1 : -1;
                    }
                    else
                    {
                        if (*p1 != *p2) return (*p1 > *p2) ? 1 : -1;

                        p1++; p2++;
                    }
                }

                return *p2 == 0 ? 0 : -1;
            }
        }

        /// <summary>
        /// Это Usafe вариант с использованием стандартной функции char.IsDigit
        /// </summary>        
        public unsafe static int CompareUnsafeIsDigit(string s1, string s2)
        {
            (var ne1, var ne2) = (string.IsNullOrEmpty(s1), string.IsNullOrEmpty(s2));

            if (ne1 && ne2) return 0;
            if (ne1) return -1;
            if (ne2) return 1;

            fixed (char* pointer1 = s1, pointer2 = s2)
            {
                var p1 = pointer1;
                var p2 = pointer2;

                while (*p1 != 0)
                {
                    if (*p2 == 0) return 1;

                    if (char.IsDigit(*p1) && char.IsDigit(*p2))
                    {
                        (var num1, var num2) = (*p1 - '0', *p2 - '0');
                        p1++; p2++;

                        while (char.IsDigit(*p1))
                        {
                            num1 = 10 * num1 + *p1 - '0';
                            p1++;
                        }

                        while (char.IsDigit(*p2))
                        {
                            num2 = 10 * num2 + *p2 - '0';
                            p2++;
                        }

                        if (num1 != num2) return num1 > num2 ? 1 : -1;
                    }
                    else
                    {
                        if (*p1 != *p2) return (*p1 > *p2) ? 1 : -1;

                        p1++; p2++;
                    }
                }

                return *p2 == 0 ? 0 : -1;
            }
        }


        #region Regexp
        /// <summary>
        /// Это самый плохой с моей точки зрения вариант
        /// </summary>
        private static Dictionary<string, string[]> table = new Dictionary<string, string[]>();
        private const string RegexString = @"([0-9]+(\.[0-9]+)?([Ee][+-]?[0-9]+)?)";
        private const StringComparison StrComparison = StringComparison.Ordinal;
        private static readonly bool isAscending = true;
        public static int CompareRegex(string x, string y)
        {
            if (x == y)
                return 0;

            string[] x1, y1;

            if (!table.TryGetValue(x, out x1))
            {
                x1 = Regex.Split(x.Replace(" ", ""), RegexString);
                table.Add(x, x1);
            }

            if (!table.TryGetValue(y, out y1))
            {
                y1 = Regex.Split(y.Replace(" ", ""), RegexString);
                table.Add(y, y1);
            }

            int returnVal;

            for (int i = 0; i < x1.Length && i < y1.Length; i++)
            {
                if (x1[i] != y1[i])
                {
                    returnVal = PartCompare(x1[i], y1[i]);
                    return isAscending ? returnVal : -returnVal;
                }
            }

            if (y1.Length > x1.Length)
            {
                returnVal = 1;
            }
            else if (x1.Length > y1.Length)
            {
                returnVal = -1;
            }
            else
            {
                returnVal = 0;
            }

            return isAscending ? returnVal : -returnVal;
        }

        private static int PartCompare(string left, string right)
        {
            decimal x, y;

            if (!decimal.TryParse(left, NumberStyles.Any, CultureInfo.InvariantCulture, out x))
                return String.Compare(left, right, StrComparison);

            if (!decimal.TryParse(right, NumberStyles.Any, CultureInfo.InvariantCulture, out y))
                return String.Compare(left, right, StrComparison);

            return x.CompareTo(y);
        } 
        #endregion



    }
}
