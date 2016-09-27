using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Console;

class Program
{
    static void Main(string[] args)
    {
        object[] numbers =
            { 0b1, 0b10, new object[] { 0b100, 0b1000 },   // binary literals
             0b1_0000, 0b10_0000 };                        // digit separators

        var (sum, count) = Tally(numbers);                 // deconstruction
        WriteLine($"Sum: {sum}, Count: {count}");
    }

    static (int sum, int count) Tally(object[] values)     // tuple types
    {
        var r = (s: 0, c: 0);                              // tuple literals

        void Add(int s, int c) { r = (r.s + s, r.c + c); } // local functions

        foreach (var v in values)
        {
            switch (v)                                     // switch on any value
            {
                case int i:                                // type patterns
                    Add(i, 1);
                    break;
                case object[] a when a.Length > 0:         // case conditions
                    var t = Tally(a);
                    Add(t.sum, t.count);
                    break;
            }
        }
        return r;
    }
}
