using Database.DBObjects.Enums;

namespace Database.Entities;

public class DevKitEntity : BaseEntity
{
    public DevKitEntity() : base(TableName.DevKits) { }

    public required string Name { get; set; }

    public required string Url { get; set; }

    public EDevKitType Type { get; set; }

    public EUsernameType PurchasedBy { get; set; }

    public DateTime PurchasedOn { get; set; }

    public DateTime MixedOn { get; set; }

    public int ValidForWeeks { get; set; }

    public int ValidForFilms { get; set; }

    public int FilmsDeveloped { get; set; }

    public Guid ImageId { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool Expired { get; set; }

    protected override int RowKeyLenght() => 8;
}