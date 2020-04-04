using System;
using System.Linq.Expressions;

namespace Bumper
{
    class Program
    {
        static void Main(string[] args)
        {
            Expression<Func<int, bool>> s = x => true;
        }
    }
}