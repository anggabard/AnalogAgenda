using Database.DBObjects.Enums;
using Database.Helpers;

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

    protected override string GetId(params string[] inputs)
    {
        return IdGenerator.Get(8, Name, Url, Type.ToString(), 
            PurchasedBy.ToString(), PurchasedOn.Ticks.ToString(), 
            MixedOn.Ticks.ToString(), ValidForWeeks.ToString(),
            ValidForFilms.ToString(), ImageId.ToString(),
            Description);
    }
}