using System.Text.Json;
using Database.DTOs;
using Database.Entities;
using Database.DBObjects.Enums;

namespace Database.Tests.DTOs;

public class SessionDtoTests
{
    [Fact]
    public void ParticipantsList_ShouldDeserializeJsonString_WhenParticipantsIsValidJson()
    {
        // Arrange
        var sessionDto = new SessionDto
        {
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
            Location = "Test Location",
            Participants = "[]"
        };
        
        var expectedSubstances = new List<string> { "devkit1", "devkit2", "devkit3" };

        // Act
        sessionDto.UsedSubstancesList = expectedSubstances;
        var serializedJson = sessionDto.UsedSubstances;
        var deserializedList = sessionDto.UsedSubstancesList;

        // Assert
        Assert.Equal("[\"devkit1\",\"devkit2\",\"devkit3\"]", serializedJson);
        Assert.Equal(expectedSubstances, deserializedList);
    }

    [Fact]
    public void DevelopedFilmsList_ShouldHandleEmptyList_Correctly()
    {
        // Arrange
        var sessionDto = new SessionDto
        {
            Location = "Test Location",
            Participants = "[]"
        };

        // Act
        sessionDto.DevelopedFilmsList = new List<string>();
        var serializedJson = sessionDto.DevelopedFilms;
        var deserializedList = sessionDto.DevelopedFilmsList;

        // Assert
        Assert.Equal("[]", serializedJson);
        Assert.Empty(deserializedList);
    }

    [Fact]
    public void ToEntity_ShouldConvertCorrectly_WithAllProperties()
    {
        // Arrange
        var sessionDto = new SessionDto
        {
            RowKey = "test-session",
            SessionDate = new DateOnly(2025, 10, 2),
            Location = "Angel's Home",
            Participants = "[\"Angel\", \"Tudor\"]",
            Description = "Test session description",
            UsedSubstances = "[\"devkit1\"]",
            DevelopedFilms = "[\"film1\", \"film2\"]",
            ImageUrl = ""
        };

        // Act
        var entity = sessionDto.ToEntity();

        // Assert
        Assert.Equal("test-session", entity.RowKey);
        Assert.Equal(new DateTime(2025, 10, 2, 0, 0, 0, DateTimeKind.Utc), entity.SessionDate);
        Assert.Equal("Angel's Home", entity.Location);
        Assert.Equal("[\"Angel\", \"Tudor\"]", entity.Participants);
        Assert.Equal("Test session description", entity.Description);
        Assert.Equal("[\"devkit1\"]", entity.UsedSubstances);
        Assert.Equal("[\"film1\", \"film2\"]", entity.DevelopedFilms);
        Assert.Equal(Guid.Empty, entity.ImageId);
    }
}

public class SessionEntityTests
{
    [Fact]
    public void ToDTO_ShouldConvertCorrectly_WithAllProperties()
    {
        // Arrange
        var entity = new SessionEntity
        {
            RowKey = "test-session",
            SessionDate = new DateTime(2025, 10, 2, 0, 0, 0, DateTimeKind.Utc),
            Location = "Angel's Home",
            Participants = "[\"Angel\", \"Tudor\"]",
            Description = "Test session description",
            UsedSubstances = "[\"devkit1\"]",
            DevelopedFilms = "[\"film1\", \"film2\"]",
            ImageId = Guid.Empty
        };

        // Act
        var dto = entity.ToDTO("testaccount");

        // Assert
        Assert.Equal("test-session", dto.RowKey);
        Assert.Equal(new DateOnly(2025, 10, 2), dto.SessionDate);
        Assert.Equal("Angel's Home", dto.Location);
        Assert.Equal("[\"Angel\", \"Tudor\"]", dto.Participants);
        Assert.Equal("Test session description", dto.Description);
        Assert.Equal("[\"devkit1\"]", dto.UsedSubstances);
        Assert.Equal("[\"film1\", \"film2\"]", dto.DevelopedFilms);
        Assert.Equal(string.Empty, dto.ImageUrl);
    }

    [Fact]
    public void Constructor_ShouldSetCorrectTableName()
    {
        // Arrange & Act
        var entity = new SessionEntity();

        // Assert
        Assert.Equal(TableName.Sessions, entity.TableName);
    }
}
