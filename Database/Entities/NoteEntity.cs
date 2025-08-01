using Database.DBObjects.Enums;
using Database.Helpers;

namespace Database.Entities;

public class NoteEntity : BaseEntity
{
    public NoteEntity() : base(TableName.Notes) { }

    public required string Name { get; set; }
    public DateTime CreatedDate { get; set; }

    protected override string GetId(params string[] inputs)
    {
        return IdGenerator.Get(4, Name, CreatedDate.Ticks.ToString());
    }
}
