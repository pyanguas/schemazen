using System;

namespace SchemaZen.Library.Models {
	public interface INameable {
		string Name { get; set; }
	}

	public interface IHasOwner {
		string Owner { get; set; }
	}

	public interface IScriptable {
		string ScriptCreate();
	}

    public interface IDatable {
        DateTime? ModifyDate { get; set; }
    }

    public interface ICustomName
    {
        string Subdir { get; set; }
        string Filename { get; set; }
    }

}
