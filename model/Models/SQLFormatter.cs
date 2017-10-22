using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SchemaZen.Library.Models
{

     
     

    public class SQLFormatter
    {
        private const string SQL_NOFMT_REGEX =@"(@NO_FMT@)";

        private static PoorMansTSqlFormatterLib.Tokenizers.TSqlStandardTokenizer tkn =
            new PoorMansTSqlFormatterLib.Tokenizers.TSqlStandardTokenizer();
        private static PoorMansTSqlFormatterLib.Parsers.TSqlStandardParser parser =
            new PoorMansTSqlFormatterLib.Parsers.TSqlStandardParser();
        private static PoorMansTSqlFormatterLib.Formatters.TSqlStandardFormatter fmt =
            new PoorMansTSqlFormatterLib.Formatters.TSqlStandardFormatter();

        /*
        private static PoorMansTSqlFormatterLib.Formatters.TSqlStandardFormatterOptions opt =
            new PoorMansTSqlFormatterLib.Formatters.TSqlStandardFormatterOptions();
        */


        public static void setTrailingCommas(bool value)
        {
            fmt.Options.TrailingCommas = value;
            fmt.Options.ExpandBetweenConditions = false;
            fmt.Options.ExpandInLists = false;
        }

        public static String formatSQL(String source)
        {
            try
            {
                var regex = new Regex(SQL_NOFMT_REGEX);
                var match = regex.Match(source);
                var group = match.Groups[1];

                if (! group.Success)
                {

                    PoorMansTSqlFormatterLib.Interfaces.ITokenList lstToken = tkn.TokenizeSQL(source);
                    System.Xml.XmlDocument xmlDoc = parser.ParseSQL(lstToken);
                    String definitionFmt = fmt.FormatSQLTree(xmlDoc);
                    return definitionFmt;

                }
                else
                {
                    return source;
                }

            }
            catch (Exception e)
            {
                return source;
            }
        }
    }
}
