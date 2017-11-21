using System;
using System.Text;

namespace SchemaZen.Library.Models {
	public class Column {

		public Default Default { get; set; }
		public Identity Identity { get; set; }
		public bool IsNullable { get; set; }
		public int Length { get; set; }
		public string Name { get; set; }
		public int Position { get; set; }
		public byte Precision { get; set; }
		public int Scale { get; set; }
		public string Type { get; set; }
		public string ComputedDefinition { get; set; }
		public bool IsRowGuidCol { get; set; }

		public Column() { }

		public Column(string name, string type, bool nullable, Default defaultValue) {
			Name = name;
			Type = type;
			Default = defaultValue;
			IsNullable = nullable;
		}

		public Column(string name, string type, int length, bool nullable, Default defaultValue)
			: this(name, type, nullable, defaultValue) {
			Length = length;
		}

		public Column(string name, string type, byte precision, int scale, bool nullable, Default defaultValue)
			: this(name, type, nullable, defaultValue) {
			Precision = precision;
			Scale = scale;
		}

		private string IsNullableText {
			get {
				if (IsNullable || !string.IsNullOrEmpty(ComputedDefinition)) return "NULL";
				return "NOT NULL";
			}
		}

		public string DefaultText {
			get {
				if (Default == null || !string.IsNullOrEmpty(ComputedDefinition)) return "";
                // Default text without CRLF
				//return "\r\n      " + Default.ScriptAsPartOfColumnDefinition();
				return Default.ScriptAsPartOfColumnDefinition();
			}
		}

		public string IdentityText {
			get {
				if (Identity == null) return "";
                // Identity text without CRLF
                //return "\r\n      " + Identity.Script();
                return Identity.Script();
			}
		}

		public string RowGuidColText => IsRowGuidCol ? " ROWGUIDCOL " : string.Empty;

		public bool Persisted { get; set; }
        public string Description { get; set; }
        public string Table { get; set; }
        public string Owner { get; set; }

        public ColumnDiff Compare(Column c) {
			return new ColumnDiff(this, c);
		}

		private string ScriptBase(bool includeDefaultConstraint) {
			if (!string.IsNullOrEmpty(ComputedDefinition)) {
				var persistedSetting = Persisted ? " PERSISTED" : string.Empty;
				return $"[{Name}] AS {ComputedDefinition}{persistedSetting}";
			}

            // Column indent
            //var val = new StringBuilder($"[{Name}] [{Type}]");
            var val = new StringBuilder($"[{Name}]");
            val.Append(new string(' ', 35 - Name.Length));
            val.Append($"[{Type}]");

            switch (Type) {
				case "bigint":
				case "bit":
				case "date":
				case "datetime":
				case "datetime2":
				case "datetimeoffset":
				case "float":
				case "hierarchyid":
				case "image":
				case "int":
				case "money":
				case "ntext":
				case "real":
				case "smalldatetime":
				case "smallint":
				case "smallmoney":
				case "sql_variant":
				case "text":
				case "time":
				case "timestamp":
				case "tinyint":
				case "uniqueidentifier":
				case "geography":
				case "xml":
				case "sysname":
                    // Column indent
                    val.Append(new string(' ', 19 - Type.Length));
                    val.Append($" {IsNullableText}");
                    val.Append(new string(' ', 10 - IsNullableText.Length));
                    if (includeDefaultConstraint) val.Append(DefaultText);
					if (Identity != null) val.Append(IdentityText);
					if (IsRowGuidCol) val.Append(RowGuidColText);
					return val.ToString();

				case "binary":
				case "char":
				case "nchar":
				case "nvarchar":
				case "varbinary":
				case "varchar":
                    // Column indent
                    var lengthString = Length.ToString();
					if (lengthString == "-1") lengthString = "max";
                    //val.Append($"({lengthString}) {IsNullableText}");
                    val.Append($"({lengthString})");
                    val.Append(new string(' ', 20 - (Type.Length + lengthString.Length + 2)));
                    val.Append($"{IsNullableText}");
                    val.Append(new string(' ', 10 - IsNullableText.Length));
                    if (includeDefaultConstraint) val.Append(DefaultText);
					return val.ToString();

				case "decimal":
				case "numeric":
                    // Column indent
                    //val.Append($"({Precision},{Scale}) {IsNullableText}");
                    val.Append($"({Precision},{Scale})");
                    val.Append(new String(' ', 20 - (Type.Length + Precision.ToString().Length + Scale.ToString().Length + 3)));
                    val.Append($"{IsNullableText}");
                    val.Append(new string(' ', 10 - IsNullableText.Length));
                    if (includeDefaultConstraint) val.Append(DefaultText);
					if (Identity != null) val.Append(IdentityText);
					return val.ToString();

				default:
					throw new NotSupportedException("Error scripting column " + Name + ". SQL data type " + Type + " is not supported.");
			}
		}

		public string ScriptCreate() {
			return ScriptBase(true);
		}

		public string ScriptAlter() {
			return ScriptBase(false);
		}

		internal static Type SqlTypeToNativeType(string sqlType) {
			switch (sqlType.ToLower()) {
				case "bit":
					return typeof(bool);
				case "datetime":
				case "smalldatetime":
					return typeof(DateTime);
				case "int":
					return typeof(int);
				case "uniqueidentifier":
					return typeof(Guid);
				case "binary":
				case "varbinary":
				case "image":
					return typeof(byte[]);
				default:
					return typeof(string);
			}
		}

		public Type SqlTypeToNativeType() {
			return SqlTypeToNativeType(Type);
		}

        public string DescriptionScriptCreate(int _maxColLength)
        {
            StringBuilder val = new StringBuilder("\tEXEC _spDescripcionColumna");
            val.Append($" '{this.Owner}'");
            val.Append($", '{this.Table}'");
            val.Append($", '{this.Name}'");
            val.Append(new string(' ', _maxColLength + 2 - this.Name.Length));
            val.Append($", '{this.Description.Replace("'", "''")}'");

            return val.ToString();
        }
    }

	public class ColumnDiff {
		public Column Source { get; set; }
		public Column Target { get; set; }

		public ColumnDiff(Column target, Column source) {
			Source = source;
			Target = target;
		}

		public bool IsDiff => IsDiffBase || DefaultIsDiff;

		private bool IsDiffBase => Source.IsNullable != Target.IsNullable || Source.Length != Target.Length ||
								   Source.Position != Target.Position || Source.Type != Target.Type || Source.Precision != Target.Precision ||
								   Source.Scale != Target.Scale || Source.ComputedDefinition != Target.ComputedDefinition || Source.Persisted != Target.Persisted;

		public bool DefaultIsDiff => Source.DefaultText != Target.DefaultText;

		public bool OnlyDefaultIsDiff => DefaultIsDiff && !IsDiffBase;

		/*public string Script() {
			return Target.Script();
		}*/
	}
}
