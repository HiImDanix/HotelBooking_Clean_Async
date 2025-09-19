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
        private readonly IRepository<Booking> bookingRepository;
        private readonly IRepository<Room> roomRepository;

        public BookingManagerTests(){
            DateTime start = DateTime.Today.AddDays(10);
            DateTime end = DateTime.Today.AddDays(20);
            bookingRepository = new FakeBookingRepository(start, end);
            IRepository<Room> roomRepository = new FakeRoomRepository();
            bookingManager = new BookingManager(bookingRepository, roomRepository);
        }

        [Fact]
        public async Task FindAvailableRoom_StartDateNotInTheFuture_ThrowsArgumentException()
        {
            // Arrange
            var bookingRepositoryMock = new Mock<IRepository<Booking>>();
            var roomRepositoryMock = new Mock<IRepository<Room>>();
            var bookingManager = new BookingManager(bookingRepositoryMock.Object, roomRepositoryMock.Object);
            
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
            var bookingRepositoryMock = new Mock<IRepository<Booking>>();
            bookingRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(
                new List<Booking>()
                );

            var roomRepositoryMock = new Mock<IRepository<Room>>();
            roomRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(
                new List<Room> { new Room { Id = 1, Description = "A" } }
                );
            
            var bookingManager = new BookingManager(bookingRepositoryMock.Object, roomRepositoryMock.Object);

            // Act
            int roomId = await bookingManager.FindAvailableRoom(DateTime.Today.AddDays(1), DateTime.Today.AddDays(2));

            // Assert
            Assert.NotEqual(-1, roomId);
        }

       [Fact]
        public async Task FindAvailableRoom_RoomAvailable_ReturnsAvailableRoom()
        {
            // Arrange
            var date = DateTime.Today.AddDays(1);
            var rooms = new List<Room> { new Room { Id = 1 }, new Room { Id = 2 } };
            // Setup a booking that does NOT conflict with our desired date.
            var bookings = new List<Booking>
            {
                new Booking { Id = 1, RoomId = 1, IsActive = true, StartDate = date.AddDays(5), EndDate = date.AddDays(10) }
            };

            var bookingRepositoryMock = new Mock<IRepository<Booking>>();
            bookingRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(bookings);

            var roomRepositoryMock = new Mock<IRepository<Room>>();
            roomRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(rooms);

            var bookingManager = new BookingManager(bookingRepositoryMock.Object, roomRepositoryMock.Object);
            
            // Act
            // Expect to get Room 2, since Room 1 is occupied later.
            int roomId = await bookingManager.FindAvailableRoom(date, date.AddDays(1));

            // Assert
            Assert.Equal(2, roomId);
        }

        [Fact]
        public async Task FindAvailableRoom_AllRoomsAreBookedWithPartialDateOverlap_ReturnsMinusOne()
        {
            // Arrange
            var requestedStartDate = DateTime.Today.AddDays(10);
            var requestedEndDate = DateTime.Today.AddDays(15);
            
            var rooms = new List<Room> { new Room { Id = 1}, new Room { Id = 2 } };

            var existingBookings = new List<Booking> { };
            
            // Room 1: stats before our request but ends during it.
            existingBookings.Add(new Booking { RoomId = 1, StartDate = requestedStartDate.AddDays(-5), EndDate = requestedStartDate.AddDays(2), IsActive = true });
            // Room 2: starts during our request but ends after it.
            existingBookings.Add(new Booking {RoomId = 2, StartDate = requestedStartDate.AddDays(2), EndDate = requestedStartDate.AddDays(10), IsActive = true });
            
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
    }
}
