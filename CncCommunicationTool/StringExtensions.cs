using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CncCommunicationTool
{
    public static class StringExtensions
    {
        public static int TryParseInt(this string source, int defaultValue = default(int))
        {
            if (source == null)
                throw new ArgumentNullException("source");

            int temp;

            if (int.TryParse(source, out temp))
                return temp;

            return defaultValue;
        }
    }
}
