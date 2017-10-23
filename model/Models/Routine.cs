using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SchemaZen.Library.Models {
	public class Routine : INameable, IHasOwner, IScriptable, IDatable {
		public enum RoutineKind {
			Procedure,
			Function,
			Trigger,
			View,
			XmlSchemaCollection,
            TVFunction
		}

        public bool AnsiNull { get; set; }
		public string Name { get; set; }
		public bool QuotedId { get; set; }
		public RoutineKind RoutineType { get; set; }
        public string Owner { get; set; }
		public string Text { get; set; }
		public bool Disabled { get; set; }
		public string RelatedTableSchema { get; set; }
		public string RelatedTableName { get; set; }
		public Database Db { get; set; }
        public DateTime? ModifyDate { get; set; }
        public int ParamCount { get; set; }

        private const string _sqlCreateRegex =
			@"\A" + Database.SqlWhitespaceOrCommentRegex + @"*?(CREATE)" + Database.SqlWhitespaceOrCommentRegex;

		private const string _sqlCreateWithNameRegex =
			_sqlCreateRegex + @"+{0}" + Database.SqlWhitespaceOrCommentRegex + @"+?(?:(?:(" + Database.SqlEnclosedIdentifierRegex +
			@"|" + Database.SqlRegularIdentifierRegex + @")\.)?(" + Database.SqlEnclosedIdentifierRegex + @"|" +
			Database.SqlRegularIdentifierRegex + @"))(?:\(|" + Database.SqlWhitespaceOrCommentRegex + @")";

        private const string _sqlCreateRegex2 =
            // @"\A"
            Database.SqlWhitespaceOrCommentRegex + @"*?(CREATE)" + Database.SqlWhitespaceOrCommentRegex;

        public Routine(string owner, string name, Database db) {
			Owner = owner;
			Name = name;
			Db = db;
		}

		private string ScriptQuotedIdAndAnsiNulls(Database db, bool databaseDefaults) {
			var script = "";
			var defaultQuotedId = !QuotedId;
			if (db?.FindProp("QUOTED_IDENTIFIER") != null) {
				defaultQuotedId = db.FindProp("QUOTED_IDENTIFIER").Value == "ON";
			}
			if (defaultQuotedId != QuotedId) {
				script += $"SET QUOTED_IDENTIFIER {((databaseDefaults ? defaultQuotedId : QuotedId) ? "ON" : "OFF")} {Environment.NewLine}GO{Environment.NewLine}";
			}
			var defaultAnsiNulls = !AnsiNull;
			if (db?.FindProp("ANSI_NULLS") != null) {
				defaultAnsiNulls = db.FindProp("ANSI_NULLS").Value == "ON";
			}
			if (defaultAnsiNulls != AnsiNull) {
				script += $"SET ANSI_NULLS {((databaseDefaults ? defaultAnsiNulls : AnsiNull) ? "ON" : "OFF")} {Environment.NewLine}GO{Environment.NewLine}";
			}
			return script;
		}

		private string ScriptBase(Database db, string definition) {
            var check = ScriptCheckObjectExists();
            var before = ScriptQuotedIdAndAnsiNulls(db, false);
			var after = ScriptQuotedIdAndAnsiNulls(db, true);
			if (!string.IsNullOrEmpty(after))
				after = Environment.NewLine + "GO" + Environment.NewLine + after;

			if (RoutineType == RoutineKind.Trigger)
				after +=
						$"{Environment.NewLine}{(Disabled ? "DISABLE" : "ENABLE")} TRIGGER [{Owner}].[{Name}] ON [{RelatedTableSchema}].[{RelatedTableName}]{Environment.NewLine}GO{Environment.NewLine}";

            if (string.IsNullOrEmpty(definition))
                definition = $"/* missing definition for {RoutineType} [{Owner}].[{Name}] */";
            else
            {
                definition = scriptAlterDefinition(definition);

                if (RoutineType == RoutineKind.View)
                {
                    SQLFormatter.setTrailingCommas(true);
                    definition = SQLFormatter.formatSQL(definition);
                }

                definition = RemoveExtraNewLines(definition);

            }

			return check + before + definition + after;
		}

		private static string RemoveExtraNewLines(string definition) {
			return definition.Trim('\r', '\n');
		}

		public string ScriptCreate() {
			return ScriptBase(Db, Text);
		}

		public string GetSQLTypeForRegEx() {
			var text = GetSQLType();
			if (RoutineType == RoutineKind.Procedure) // support shorthand - PROC
				return "(?:" + text + "|" + text.Substring(0, 4) + ")";
			return text;
		}

		public string GetSQLType() {
			var text = RoutineType.ToString();
			return string.Join(string.Empty, text.AsEnumerable().Select(
				(c, i) => ((char.IsUpper(c) || i == 0) ? " " + char.ToUpper(c).ToString() : c.ToString())
				).ToArray()).Trim();
		}

		public string ScriptDrop() {
			return $"DROP {GetSQLType()} [{Owner}].[{Name}]";
		}


		public string ScriptAlter(Database db) {
			if (RoutineType != RoutineKind.XmlSchemaCollection) {
				var regex = new Regex(_sqlCreateRegex, RegexOptions.IgnoreCase);
				var match = regex.Match(Text);
				var group = match.Groups[1];
				if (group.Success) {
					return ScriptBase(db, Text.Substring(0, group.Index) + "ALTER" + Text.Substring(group.Index + group.Length));
				}
			}
			throw new Exception($"Unable to script routine {RoutineType} {Owner}.{Name} as ALTER");
		}

		public IEnumerable<string> Warnings() {
			if (string.IsNullOrEmpty(Text)) {
				yield return "Script definition could not be retrieved.";
			} else {
				// check if the name is correct
				var regex = new Regex(string.Format(_sqlCreateWithNameRegex, GetSQLTypeForRegEx()),
					RegexOptions.IgnoreCase | RegexOptions.Singleline);
				var match = regex.Match(Text);

				// the schema is captured in group index 2, and the name in 3

				var nameGroup = match.Groups[3];
				if (!nameGroup.Success)
					yield break;

				var name = nameGroup.Value;
				if (name.StartsWith("[") && name.EndsWith("]"))
					name = name.Substring(1, name.Length - 2);

				if (string.Compare(Name, name, StringComparison.InvariantCultureIgnoreCase) != 0) {
					yield return $"Name from script definition '{name}' does not match expected value from sys.objects.name '{Name}'. This can be corrected by dropping and recreating the object.";
				}
			}
		}

        private string ScriptCheckObjectExists()
        {
            var script = "";

            string schemaQualifier = "";
            if (!string.IsNullOrEmpty(this.Owner) && this.Owner.ToLower() != "dbo")
            {
                schemaQualifier = "[" + this.Owner + "].";
            }

            if (RoutineType == RoutineKind.View)
            {

                script =
                    $"IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'{schemaQualifier}{this.Name}')){Environment.NewLine}" +
                    $"\tEXEC sp_executesql N'CREATE VIEW {schemaQualifier}{this.Name} AS SELECT 1 AS STUB'{Environment.NewLine}GO{Environment.NewLine}{Environment.NewLine}"
                ;
            }
            else if (RoutineType == RoutineKind.Trigger)
            {

                script =
                    $"IF NOT EXISTS (SELECT * FROM sys.triggers WHERE object_id = OBJECT_ID(N'{this.Name}')){Environment.NewLine}" +
                    $"\tEXEC sp_executesql N'CREATE TRIGGER [{this.Owner}].[{this.Name}] ON [{this.RelatedTableSchema}].[{this.RelatedTableName}] FOR UPDATE AS SELECT 1 AS STUB'{Environment.NewLine}GO{Environment.NewLine}{Environment.NewLine}"
                ;

            }

            else if (RoutineType == RoutineKind.Procedure)
            {
                script =
                    $"IF NOT EXISTS (SELECT * FROM sys.procedures WHERE object_id = OBJECT_ID(N'{this.Name}')){Environment.NewLine}" +
                    $"\tEXEC sp_executesql N'CREATE PROCEDURE [{this.Owner}].[{this.Name}] AS SELECT 1 AS STUB'{Environment.NewLine}GO{Environment.NewLine}{Environment.NewLine}"
                ;
            }
            else if (RoutineType == RoutineKind.Function)
            {
                string sStubParamList = "";
                for (int i = 0; i < this.ParamCount; i++)
                {
                    if (i > 0)
                    {
                        sStubParamList += ", ";
                    }
                    sStubParamList += "@p" + i.ToString() + " int";
                }

                script =
                   $"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{this.Name}') AND type IN ('FN', 'IF')){Environment.NewLine}" +
                   $"\tEXEC sp_executesql N'CREATE FUNCTION [{this.Owner}].[{this.Name}]({sStubParamList}) RETURNS INT AS BEGIN RETURN 1 END'{Environment.NewLine}GO{Environment.NewLine}{Environment.NewLine}"
                ;
            }
            else if (RoutineType == RoutineKind.TVFunction)
            {
                script =
                    $"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{this.Name}') AND type IN ('TF')){Environment.NewLine}" +
                    $"\tEXEC sp_executesql N'CREATE FUNCTION [{this.Owner}].[{this.Name}]() RETURNS @t TABLE(v int) BEGIN RETURN END'{Environment.NewLine}GO{Environment.NewLine}{Environment.NewLine}"
                ;
                script = String.Format(
                    "IF NOT EXISTS (SELECT * FROM {0} WHERE object_id = OBJECT_ID(N'{3}') AND type IN ('TF')){5}" +
                    "\tEXEC sp_executesql N'CREATE {1} [{2}].[{3}]() RETURNS @t TABLE(v int) {4}'{5}GO{5}{5}",
                    "sys.objects",             // 0
                    "FUNCTION",                // 1
                    this.Owner,                // 2 
                    this.Name,                 // 3
                    "BEGIN RETURN END",        // 4
                    Environment.NewLine        // 5
                );
            }
            return script;
        }


        private string scriptAlterDefinition(string definition)
        {
            var script = definition;
            var regex = new Regex(_sqlCreateRegex2, RegexOptions.IgnoreCase);
            var match = regex.Match(definition);
            var group = match.Groups[1];
            if (group.Success)
            {
                script = definition.Substring(0, group.Index) + "ALTER" + definition.Substring(group.Index + group.Length);
            }
            return script;
        }
    }
}
