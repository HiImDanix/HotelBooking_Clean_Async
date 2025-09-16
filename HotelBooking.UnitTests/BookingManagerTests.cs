using System;
using System.Collections.Generic;
using HotelBooking.Core;
using HotelBooking.UnitTests.Fakes;
using Xunit;
using System.Linq;
using System.Threading.Tasks;
using Moq;


namespace HotelBooking.UnitTests
{
    public class BookingManagerTests
    {
        private readonly IBookingManager bookingManager;
        private readonly Mock<IRepository<Booking>> bookingRepository;
        private readonly Mock<IRepository<Room>> roomRepository;

        public BookingManagerTests()
        {
            bookingRepository = new Mock<IRepository<Booking>>();
            roomRepository = new Mock<IRepository<Room>>();
            bookingManager = new BookingManager(bookingRepository.Object, roomRepository.Object);

        }

        [Fact]
        public async Task FindAvailableRoom_StartDateNotInTheFuture_ThrowsArgumentException()
        {
            // Arrange

            DateTime date = DateTime.Today;

            // Act
            Task result() => bookingManager.FindAvailableRoom(date, date);

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(result);
        }

        [Fact]
        public async Task FindAvailableRoom_RoomAvailable_RoomIdNotMinusOne()
        {
            // Arrange
            bookingRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(
                new List<Booking>()
                );

            var roomRepositoryMock = new Mock<IRepository<Room>>();
            roomRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(
                new List<Room> { new Room { Id = 1, Description = "A" } }
                );

            var bookingManager = new BookingManager(bookingRepository.Object, roomRepositoryMock.Object);

            // Act
            int roomId = await bookingManager.FindAvailableRoom(DateTime.Today.AddDays(1), DateTime.Today.AddDays(2));

            // Assert
            Assert.NotEqual(-1, roomId);
        }

        //[expectedRoomId, roomIdBooking, daysFromNowStart1, daysFromNowEnd1, daysFromNowCheck]
        [Theory]
        [InlineData(1, 1, 5, 10, 2)]                //Room 1 should be available, since Room 1 is not booked in the given check time.
        [InlineData(1, 2, 3, 7, 5)]                 //Room 1 is available since Room 1 is booked in time period
        [InlineData(2, 1, 3, 6, 4)]                 //Room 2 is available since Room 1 is booked in time period
        [InlineData(1, 2, 10, 15, 13)]              //Room 2 is available since Room 1 is booked in time period
        [InlineData(1, 1, 10, 15, 2)]               //Room 2 is available since Room 1 is booked in time period

        public async Task FindAvailableRoom_RoomAvailable_ReturnsAvailableRoom(int expectedRoomId, int roomIdBooking, int daysFromNowStart1, int daysFromNowEnd1, int daysFromNowCheck)
        {
            // Arrange
            var date = DateTime.Today.AddDays(1);
            var dateCheck = date.AddDays(daysFromNowCheck);
            var rooms = new List<Room> { new Room { Id = 1 }, new Room { Id = 2 } };
            // Setup a booking that does NOT conflict with our desired date.
            var bookings = new List<Booking>
            {
                new Booking { Id = 1, RoomId = roomIdBooking, IsActive = true, StartDate = date.AddDays(daysFromNowStart1), EndDate = date.AddDays(daysFromNowEnd1) }
            };

            // Mock the repositories and set the bookings
            var bookingRepositoryMock = new Mock<IRepository<Booking>>();
            bookingRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(bookings);

            //Mock the repository to return the rooms
            var roomRepositoryMock = new Mock<IRepository<Room>>();
            roomRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(rooms);

            //Create the BookingManager with the mocked repositories
            var bookingManager = new BookingManager(bookingRepositoryMock.Object, roomRepositoryMock.Object);

            // Act
            int roomId = await bookingManager.FindAvailableRoom(dateCheck, dateCheck.AddDays(1));

            // Assert
            Assert.Equal(expectedRoomId, roomId);
        }

        [Fact]
        public async Task FindAvailableRoom_AllRoomsAreBookedWithPartialDateOverlap_ReturnsMinusOne()
        {
            // Arrange
            var requestedStartDate = DateTime.Today.AddDays(10);
            var requestedEndDate = DateTime.Today.AddDays(15);

            var rooms = new List<Room> { new Room { Id = 1 }, new Room { Id = 2 } };

            var existingBookings = new List<Booking> { };

            // Room 1: stats before our request but ends during it.
            existingBookings.Add(new Booking { RoomId = 1, StartDate = requestedStartDate.AddDays(-5), EndDate = requestedStartDate.AddDays(2), IsActive = true });
            // Room 2: starts during our request but ends after it.
            existingBookings.Add(new Booking { RoomId = 2, StartDate = requestedStartDate.AddDays(2), EndDate = requestedStartDate.AddDays(10), IsActive = true });

            var bookingRepositoryMock = new Mock<IRepository<Booking>>();
            bookingRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(existingBookings);

            var roomRepositoryMock = new Mock<IRepository<Room>>();
            roomRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(rooms);

            var bookingManager = new BookingManager(bookingRepositoryMock.Object, roomRepositoryMock.Object);

            // Act
            var roomId = await bookingManager.FindAvailableRoom(requestedStartDate, requestedEndDate);

            // Assert
            Assert.Equal(-1, roomId);


        }

        [Fact]
        public async Task CreateBooking_NoRoomIsAvailable_ReturnsFalseAndDoesNotAddBooking()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(2);
            var endDate = DateTime.Today.AddDays(5);
            var rooms = new List<Room> { new Room { Id = 1 }, new Room { Id = 2 } };

            var bookings = new List<Booking>
            {
                new Booking { RoomId = 1, StartDate = startDate, EndDate = endDate, IsActive = true },
                new Booking { RoomId = 2, StartDate = startDate, EndDate = endDate, IsActive = true }
            };

            var bookingRepositoryMock = new Mock<IRepository<Booking>>();
            bookingRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(bookings);

            var roomRepositoryMock = new Mock<IRepository<Room>>();
            roomRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(rooms);

            var bookingManager = new BookingManager(bookingRepositoryMock.Object, roomRepositoryMock.Object);

            var newBooking = new Booking { StartDate = startDate, EndDate = endDate };

            // Act
            bool result = await bookingManager.CreateBooking(newBooking);

            // Assert
            Assert.False(result, "CreateBooking should return false when no rooms are available.");

            bookingRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Booking>()), Times.Never());
        }

        [Fact]
        public async Task CreateBooking_RoomIsAvailable_ReturnsTrueAndAddsBooking()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(2);
            var endDate = DateTime.Today.AddDays(5);

            var bookingRepositoryMock = new Mock<IRepository<Booking>>();
            bookingRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(
                new List<Booking>()
                );

            var roomRepositoryMock = new Mock<IRepository<Room>>();
            roomRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(
                new List<Room> { new Room { Id = 1 } }
                );

            var bookingManager = new BookingManager(bookingRepositoryMock.Object, roomRepositoryMock.Object);

            var newBooking = new Booking { StartDate = startDate, EndDate = endDate, CustomerId = 123 };

            // Act
            bool result = await bookingManager.CreateBooking(newBooking);

            // Assert
            Assert.True(result, "CreateBooking should return true when a room is available.");

            bookingRepositoryMock.Verify(x => x.AddAsync(It.Is<Booking>(b =>
                b.IsActive == true &&
                b.RoomId == 1 &&
                b.CustomerId == 123
            )), Times.Once());
        }

        [Fact]
        public async Task GetFullyOccupiedDates_StartDateIsAfterEndDate_ThrowsArgumentException()
        {
            //Arrange
            DateTime startDate = DateTime.Today.AddDays(1);
            DateTime endDate = DateTime.Today;


            // Act
            Task result() => bookingManager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(result);


        }

    }
}
