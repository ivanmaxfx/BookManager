using FluentAssertions;
using MyWebApiProject.Dtos;
using MyWebApiProject.Exceptions;
using MyWebApiProject.Models;
using MyWebApiProject.Services;

namespace EventService.Tests
{
    public class EventServiceTests
    {
        private static MyWebApiProject.Services.EventService CreateService() => new();

        [Fact]
        public void Create_Should_Add_Event()
        {
            // Arrange
            var service = CreateService();

            var eventItem = new Event
            {
                Id = Guid.NewGuid(),
                Title = "Meeting",
                Description = "Sprint planning",
                StartAt = new DateTime(2026, 5, 1, 10, 0, 0),
                EndAt = new DateTime(2026, 5, 1, 11, 0, 0)
            };

            // Act
            var result = service.Create(eventItem);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(eventItem.Id, result.Id);
            Assert.Equal(eventItem.Title, result.Title);
            Assert.Equal(eventItem.Description, result.Description);
            Assert.Equal(eventItem.StartAt, result.StartAt);
            Assert.Equal(eventItem.EndAt, result.EndAt);

            var savedEvent = service.GetById(eventItem.Id);
            Assert.NotNull(savedEvent);
            Assert.Equal(eventItem.Id, savedEvent.Id);
            Assert.Equal(eventItem.Title, savedEvent.Title);
            Assert.Equal(eventItem.Description, savedEvent.Description);
            Assert.Equal(eventItem.StartAt, savedEvent.StartAt);
            Assert.Equal(eventItem.EndAt, savedEvent.EndAt);
        }

        [Fact]
        public void GetById_Should_Return_Event_When_Exists()
        {
            // Arrange
            var service = CreateService();

            var eventItem = new Event
            {
                Id = Guid.NewGuid(),
                Title = "Workshop",
                StartAt = new DateTime(2026, 5, 2, 10, 0, 0),
                EndAt = new DateTime(2026, 5, 2, 12, 0, 0)
            };

            service.Create(eventItem);

            // Act
            var result = service.GetById(eventItem.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(eventItem.Id, result.Id);
            Assert.Equal(eventItem.Title, result.Title);
            Assert.Equal(eventItem.Description, result.Description);
            Assert.Equal(eventItem.StartAt, result.StartAt);
            Assert.Equal(eventItem.EndAt, result.EndAt);
        }
        

        [Fact]
        public void GetById_Should_Throw_NotFoundException_When_Id_Does_Not_Exist()
        {
            // Arrange
            var service = CreateService();
            var id = Guid.NewGuid();

            // Act
            Action act = () => service.GetById(id);

            // Assert
            act.Should().Throw<NotFoundException>()
                .WithMessage($"Event with id '{id}' was not found.");
        }

        [Fact]
        public void Update_Should_Modify_Existing_Event()
        {
            // Arrange
            var service = CreateService();
            var id = Guid.NewGuid();

            service.Create(new Event
            {
                Id = id,
                Title = "Old title",
                Description = "Old description",
                StartAt = new DateTime(2026, 5, 3, 9, 0, 0),
                EndAt = new DateTime(2026, 5, 3, 10, 0, 0)
            });

            var updatedEvent = new Event
            {
                Id = id,
                Title = "New title",
                Description = "New description",
                StartAt = new DateTime(2026, 5, 3, 11, 0, 0),
                EndAt = new DateTime(2026, 5, 3, 12, 0, 0)
            };

            // Act
            service.Update(id, updatedEvent);
            var result = service.GetById(id);

            // Assert
            result.Title.Should().Be("New title");
            result.Description.Should().Be("New description");
            result.StartAt.Should().Be(new DateTime(2026, 5, 3, 11, 0, 0));
            result.EndAt.Should().Be(new DateTime(2026, 5, 3, 12, 0, 0));
        }

        [Fact]
        public void Update_Should_Throw_NotFoundException_When_Id_Does_Not_Exist()
        {
            // Arrange
            var service = CreateService();
            var id = Guid.NewGuid();

            var updatedEvent = new Event
            {
                Id = id,
                Title = "Updated title",
                StartAt = new DateTime(2026, 5, 3, 11, 0, 0),
                EndAt = new DateTime(2026, 5, 3, 12, 0, 0)
            };

            // Act
            Action act = () => service.Update(id, updatedEvent);

            // Assert
            act.Should().Throw<NotFoundException>()
                .WithMessage($"Event with id '{id}' was not found.");
        }

        [Fact]
        public void Delete_Should_Remove_Existing_Event()
        {
            // Arrange
            var service = CreateService();
            var id = Guid.NewGuid();

            service.Create(new Event
            {
                Id = id,
                Title = "To delete",
                StartAt = new DateTime(2026, 5, 4, 9, 0, 0),
                EndAt = new DateTime(2026, 5, 4, 10, 0, 0)
            });

            // Act
            service.Delete(id);

            // Assert
            Action act = () => service.GetById(id);
            act.Should().Throw<NotFoundException>();
        }

        [Fact]
        public void GetAll_Should_Filter_By_Title()
        {
            // Arrange
            var service = CreateService();

            service.Create(new Event
            {
                Id = Guid.NewGuid(),
                Title = "Team meeting",
                StartAt = new DateTime(2026, 5, 1, 10, 0, 0),
                EndAt = new DateTime(2026, 5, 1, 11, 0, 0)
            });

            service.Create(new Event
            {
                Id = Guid.NewGuid(),
                Title = "Workshop",
                StartAt = new DateTime(2026, 5, 2, 10, 0, 0),
                EndAt = new DateTime(2026, 5, 2, 11, 0, 0)
            });

            // Act
            var result = service.GetAll(new EventQueryParameters
            {
                Title = "meeting"
            });

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.Should().HaveCount(1);
            result.Items.First().Title.Should().Be("Team meeting");
        }

        [Fact]
        public void GetAll_Should_Filter_By_Date_Range()
        {
            // Arrange
            var service = CreateService();

            service.Create(new Event
            {
                Id = Guid.NewGuid(),
                Title = "Early event",
                StartAt = new DateTime(2026, 5, 1, 10, 0, 0),
                EndAt = new DateTime(2026, 5, 1, 11, 0, 0)
            });

            service.Create(new Event
            {
                Id = Guid.NewGuid(),
                Title = "Late event",
                StartAt = new DateTime(2026, 5, 10, 10, 0, 0),
                EndAt = new DateTime(2026, 5, 10, 11, 0, 0)
            });

            // Act
            var result = service.GetAll(new EventQueryParameters
            {
                From = new DateTime(2026, 5, 5),
                To = new DateTime(2026, 5, 15)
            });

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.Should().HaveCount(1);
            result.Items.First().Title.Should().Be("Late event");
        }

        [Fact]
        public void GetAll_Should_Return_Paginated_Result()
        {
            // Arrange
            var service = CreateService();

            for (int i = 1; i <= 15; i++)
            {
                service.Create(new Event
                {
                    Id = Guid.NewGuid(),
                    Title = $"Event {i}",
                    StartAt = new DateTime(2026, 5, i, 10, 0, 0),
                    EndAt = new DateTime(2026, 5, i, 11, 0, 0)
                });
            }

            // Act
            var result = service.GetAll(new EventQueryParameters
            {
                Page = 2,
                PageSize = 5
            });

            // Assert
            result.TotalCount.Should().Be(15);
            result.Page.Should().Be(2);
            result.PageSize.Should().Be(5);
            result.Items.Should().HaveCount(5);
            result.Items.First().Title.Should().Be("Event 6");
        }

        [Fact]
        public void GetAll_Should_Apply_Combined_Filtering()
        {
            // Arrange
            var service = CreateService();

            service.Create(new Event
            {
                Id = Guid.NewGuid(),
                Title = "Team meeting",
                StartAt = new DateTime(2026, 6, 1, 10, 0, 0),
                EndAt = new DateTime(2026, 6, 1, 11, 0, 0)
            });

            service.Create(new Event
            {
                Id = Guid.NewGuid(),
                Title = "Team meeting old",
                StartAt = new DateTime(2026, 4, 1, 10, 0, 0),
                EndAt = new DateTime(2026, 4, 1, 11, 0, 0)
            });

            // Act
            var result = service.GetAll(new EventQueryParameters
            {
                Title = "team",
                From = new DateTime(2026, 5, 1),
                To = new DateTime(2026, 6, 30)
            });

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.Should().HaveCount(1);
            result.Items.First().Title.Should().Be("Team meeting");
        }

        [Fact]
        public void Create_Should_Throw_ValidationException_When_Title_Is_Invalid()
        {
            // Arrange
            var service = CreateService();

            var eventItem = new Event
            {
                Id = Guid.NewGuid(),
                Title = "   ",
                StartAt = new DateTime(2026, 5, 1, 10, 0, 0),
                EndAt = new DateTime(2026, 5, 1, 11, 0, 0)
            };

            // Act
            Action act = () => service.Create(eventItem);

            // Assert
            act.Should().Throw<ValidationException>()
                .WithMessage("Title is required.");
        }

        [Fact]
        public void Update_Should_Throw_ValidationException_When_EndAt_Is_Earlier_Than_StartAt()
        {
            // Arrange
            var service = CreateService();
            var id = Guid.NewGuid();

            service.Create(new Event
            {
                Id = id,
                Title = "Valid event",
                StartAt = new DateTime(2026, 5, 1, 10, 0, 0),
                EndAt = new DateTime(2026, 5, 1, 11, 0, 0)
            });

            var updatedEvent = new Event
            {
                Id = id,
                Title = "Broken event",
                StartAt = new DateTime(2026, 5, 1, 12, 0, 0),
                EndAt = new DateTime(2026, 5, 1, 11, 0, 0)
            };

            // Act
            Action act = () => service.Update(id, updatedEvent);

            // Assert
            act.Should().Throw<ValidationException>()
                .WithMessage("EndAt must be later than StartAt.");
        }

        [Fact]
        public void GetAll_Should_Throw_ValidationException_When_Page_Is_Invalid()
        {
            // Arrange
            var service = CreateService();

            // Act
            Action act = () => service.GetAll(new EventQueryParameters
            {
                Page = 0,
                PageSize = 10
            });

            // Assert
            act.Should().Throw<ValidationException>()
                .WithMessage("Page must be greater than 0.");
        }

        [Fact]
        public void GetAll_Should_Throw_ValidationException_When_PageSize_Is_Invalid()
        {
            // Arrange
            var service = CreateService();

            // Act
            Action act = () => service.GetAll(new EventQueryParameters
            {
                Page = 1,
                PageSize = 0
            });

            // Assert
            act.Should().Throw<ValidationException>()
                .WithMessage("PageSize must be greater than 0.");
        }

        [Fact]
        public void GetAll_Should_Throw_ValidationException_When_From_Is_Later_Than_To()
        {
            // Arrange
            var service = CreateService();

            // Act
            Action act = () => service.GetAll(new EventQueryParameters
            {
                From = new DateTime(2026, 6, 1),
                To = new DateTime(2026, 5, 1)
            });

            // Assert
            act.Should().Throw<ValidationException>()
                .WithMessage("'From' must be earlier than or equal to 'To'.");
        }
    }
}
