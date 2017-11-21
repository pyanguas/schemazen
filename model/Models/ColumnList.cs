using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace SchemaZen.Library.Models {
	public class ColumnList {
		private readonly List<Column> _mItems = new List<Column>();

		public ReadOnlyCollection<Column> Items => _mItems.AsReadOnly();

        private int _maxLength = 0;

        public void Add(Column c) {
            _mItems.Add(c);
            if (c.Name.Length > _maxLength)
            {
                _maxLength = c.Name.Length;
            }
		}

		public void Remove(Column c) {
			_mItems.Remove(c);
		}

		public Column Find(string name) {
			return _mItems.FirstOrDefault(c => c.Name == name);
		}
        
        // Comma after line
        /*
		public string Script() {
			var text = new StringBuilder();
			foreach (var c in _mItems) {
				text.Append("   " + c.ScriptCreate());
				if (_mItems.IndexOf(c) < _mItems.Count - 1) {
					text.AppendLine(",");
				} else {
					text.AppendLine();
				}
			}
			return text.ToString();
		}
        */

        // Comma before line
        public string Script()
        {
            var text = new StringBuilder();
            foreach (var c in _mItems)
            {
                if (_mItems.IndexOf(c) > 0)
                {
                    text.Append("  , ");
                }
                else
                {
                    text.Append("    ");
                }
                text.AppendLine(c.ScriptCreate().TrimEnd());
            }
            return text.ToString();
        }

        public string DescriptionsScript()
        {
            var text = new StringBuilder();
            foreach (var c in _mItems)
            {
                text.AppendLine(c.DescriptionScriptCreate(_maxLength));
            }
            return text.ToString();
        }
    }
}
