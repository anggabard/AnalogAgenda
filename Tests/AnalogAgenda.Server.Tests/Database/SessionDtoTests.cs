using System.Text.Json;
using Configuration.Sections;
using Database.DTOs;
using Database.Entities;
using Database.Services;
using Moq;

namespace AnalogAgenda.Server.Tests.Database;

public class SessionDtoTests
{
    [Fact]
    public void ParticipantsList_ShouldDeserializeJsonString_WhenParticipantsIsValidJson()
    {
        // Arrange
        var sessionDto = new SessionDto
        {
            Id = "test-id",
            Location = "Test Location",
            Participants = "[\"Angel\", \"Tudor\", \"Cristiana\"]"
        };

        // Act
        var participants = sessionDto.ParticipantsList;

        // Assert
        Assert.NotNull(participants);
        Assert.Equal(3, participants.Count);
        Assert.Contains("Angel", participants);
        Assert.Contains("Tudor", participants);
        Assert.Contains("Cristiana", participants);
    }

    [Fact]
    public void ParticipantsList_ShouldReturnEmptyList_WhenParticipantsIsEmpty()
    {
        // Arrange
        var sessionDto = new SessionDto
        {
            Id = "test-id",
            Location = "Test Location",
            Participants = ""
        };

        // Act
        var participants = sessionDto.ParticipantsList;

        // Assert
        Assert.NotNull(participants);
        Assert.Empty(participants);
    }

    [Fact]
    public void UsedSubstancesList_ShouldSerializeAndDeserialize_Correctly()
    {
        // Arrange
        var sessionDto = new SessionDto
        {
            Id = "test-id",
            Location = "Test Location",
            Participants = "[]"
        };
        
        var expectedSubstances = new List<string> { "devkit1", "devkit2", "devkit3" };

        // Act
        sessionDto.UsedSubstancesList = expectedSubstances;
        var serializedJson = sessionDto.UsedSubstances;
        var deserializedList = sessionDto.UsedSubstancesList;

        // Assert
        // Format changed from JSON to comma-separated
        Assert.Equal("devkit1,devkit2,devkit3", serializedJson);
        Assert.Equal(expectedSubstances, deserializedList);
    }

    [Fact]
    public void DevelopedFilmsList_ShouldHandleEmptyList_Correctly()
    {
        // Arrange
        var sessionDto = new SessionDto
        {
            Id = "test-id",
            Location = "Test Location",
            Participants = "[]"
        };

        // Act
        sessionDto.DevelopedFilmsList = new List<string>();
        var serializedJson = sessionDto.DevelopedFilms;
        var deserializedList = sessionDto.DevelopedFilmsList;

        // Assert
        // Empty list returns empty string, not "[]"
        Assert.Equal("", serializedJson);
        Assert.Empty(deserializedList);
    }

    [Fact]
    public void ToEntity_ShouldConvertCorrectly_WithAllProperties()
    {
        // Arrange
        var sessionDto = new SessionDto
        {
            Id = "test-session",
            SessionDate = new DateOnly(2025, 10, 2),
            Location = "Angel's Home",
            Participants = "[\"Angel\", \"Tudor\"]",
            Description = "Test session description",
            UsedSubstances = "devkit1,devkit2", // Now comma-separated instead of JSON
            DevelopedFilms = "film1,film2",     // Now comma-separated instead of JSON
            ImageUrl = ""
        };

        var entityConvertor = new EntityConvertor();

        // Act
        var entity = entityConvertor.ToEntity(sessionDto);

        // Assert
        Assert.Equal("test-session", entity.Id);
        Assert.Equal(new DateTime(2025, 10, 2, 0, 0, 0, DateTimeKind.Utc), entity.SessionDate);
        Assert.Equal("Angel's Home", entity.Location);
        Assert.Equal("[\"Angel\", \"Tudor\"]", entity.Participants);
        Assert.Equal("Test session description", entity.Description);
        Assert.Equal(Guid.Empty, entity.ImageId);
    }
}

public class SessionEntityTests
{
    [Fact]
    public void ToDTO_ShouldConvertCorrectly_WithAllProperties()
    {
        // Arrange
        var devKit1 = new DevKitEntity { Id = "devkit1", Name = "DevKit 1", Url = "http://example.com" };
        var devKit2 = new DevKitEntity { Id = "devkit2", Name = "DevKit 2", Url = "http://example.com" };
        var film1 = new FilmEntity { Id = "film1", Brand = "Film 1", Iso = "400" };
        var film2 = new FilmEntity { Id = "film2", Brand = "Film 2", Iso = "200" };
        
        var entity = new SessionEntity
        {
            Id = "test-session",
            SessionDate = new DateTime(2025, 10, 2, 0, 0, 0, DateTimeKind.Utc),
            Location = "Angel's Home",
            Participants = "[\"Angel\", \"Tudor\"]",
            Description = "Test session description",
            ImageId = Guid.Empty,
            UsedDevKits = new List<DevKitEntity> { devKit1, devKit2 },
            DevelopedFilms = new List<FilmEntity> { film1, film2 }
        };

        var systemConfig = new Configuration.Sections.System { IsDev = false };
        var storageConfig = new Configuration.Sections.Storage { AccountName = "testaccount" };
        var dtoConvertor = new DtoConvertor(systemConfig, storageConfig);

        // Act
        var dto = dtoConvertor.ToDTO(entity);

        // Assert
        Assert.Equal("test-session", dto.Id);
        Assert.Equal(new DateOnly(2025, 10, 2), dto.SessionDate);
        Assert.Equal("Angel's Home", dto.Location);
        Assert.Equal("[\"Angel\", \"Tudor\"]", dto.Participants);
        Assert.Equal("Test session description", dto.Description);
        Assert.Equal("devkit1,devkit2", dto.UsedSubstances);
        Assert.Equal("film1,film2", dto.DevelopedFilms);
        Assert.Equal(string.Empty, dto.ImageUrl);
    }
}

