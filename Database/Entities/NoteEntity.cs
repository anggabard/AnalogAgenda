using Database.DBObjects.Enums;

namespace Database.Entities;

public class NoteEntity : BaseEntity
{
    public NoteEntity() : base(TableName.Notes) { }

    public required string Name { get; set; }

    protected override ushort RowKeyLenght() => 4;
}
