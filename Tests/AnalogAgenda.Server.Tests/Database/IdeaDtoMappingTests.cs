using Configuration.Sections;
using Database.DTOs;
using Database.Entities;
using Database.Services;
using SystemConfig = Configuration.Sections.System;

namespace AnalogAgenda.Server.Tests.Database;

public class IdeaDtoMappingTests
{
    private static DtoConvertor CreateConvertor()
    {
        var systemConfig = new SystemConfig { IsDev = false };
        var storageConfig = new Storage { AccountName = "testaccount" };
        return new DtoConvertor(systemConfig, storageConfig);
    }

    [Fact]
    public void ToDTO_OrdersConnectedSessionsByIndex_InvalidIndexLast_AlignsIds()
    {
        var baseDate = DateTime.UtcNow.Date;
        var sessionBad = new SessionEntity
        {
            Id = "sbad",
            Index = 0,
            Name = null,
            Location = "Bad",
            Participants = "[]",
            SessionDate = baseDate,
            ImageId = Guid.NewGuid()
        };
        var sessionSecond = new SessionEntity
        {
            Id = "s2",
            Index = 2,
            Name = null,
            Location = "L2",
            Participants = "[]",
            SessionDate = baseDate,
            ImageId = Guid.NewGuid()
        };
        var sessionFirst = new SessionEntity
        {
            Id = "s1",
            Index = 1,
            Name = "Named",
            Location = "L1",
            Participants = "[]",
            SessionDate = baseDate,
            ImageId = Guid.NewGuid()
        };

        var idea = new IdeaEntity
        {
            Id = "idea1",
            Title = "T",
            Description = "",
            Outcome = "",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            IdeaSessions =
            [
                new IdeaSessionEntity { IdeaId = "idea1", SessionId = sessionBad.Id, Session = sessionBad },
                new IdeaSessionEntity { IdeaId = "idea1", SessionId = sessionSecond.Id, Session = sessionSecond },
                new IdeaSessionEntity { IdeaId = "idea1", SessionId = sessionFirst.Id, Session = sessionFirst }
            ]
        };

        var dto = CreateConvertor().ToDTO(idea);

        Assert.Equal(3, dto.ConnectedSessions!.Count);
        Assert.Equal("s1", dto.ConnectedSessions[0].Id);
        Assert.Equal("Named", dto.ConnectedSessions[0].DisplayLabel);
        Assert.Equal("s2", dto.ConnectedSessions[1].Id);
        Assert.Equal("Session 2", dto.ConnectedSessions[1].DisplayLabel);
        Assert.Equal("sbad", dto.ConnectedSessions[2].Id);
        Assert.Equal(string.Empty, dto.ConnectedSessions[2].DisplayLabel);

        Assert.Equal(new[] { "s1", "s2", "sbad" }, dto.ConnectedSessionIds);
    }
}
