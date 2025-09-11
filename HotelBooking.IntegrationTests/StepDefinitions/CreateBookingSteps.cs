using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Reqnroll;
using Reqnroll.Assist;
using HotelBooking.Core;
using Xunit;

namespace HotelBooking.IntegrationTests.StepDefinitions
{
    [Binding]
    public class CreateBookingSteps
    {
        private readonly ScenarioContext _scenarioContext;
        private readonly IReqnrollOutputHelper _outputHelper;

        private readonly Mock<IRepository<Booking>> _bookingRepositoryMock;
        private readonly Mock<IRepository<Room>> _roomRepositoryMock;
        private readonly IBookingManager _bookingManager;

        private readonly List<Booking> _liveBookings = new(); // live bookings list
        private readonly List<Room> _liveRooms = new();       // live rooms list

        public CreateBookingSteps(ScenarioContext scenarioContext, IReqnrollOutputHelper outputHelper)
        {
            _scenarioContext = scenarioContext;
            _outputHelper = outputHelper;

            _bookingRepositoryMock = new Mock<IRepository<Booking>>();
            _roomRepositoryMock = new Mock<IRepository<Room>>();

            // Setup repositories to use live lists
            _bookingRepositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(() => _liveBookings.ToList());

            _bookingRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Booking>()))
                .Returns((Booking b) =>
                {
                    _liveBookings.Add(b);
                    return Task.CompletedTask;
                });

            _roomRepositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(() => _liveRooms.ToList());

            // Instantiate real BookingManager with mocked repositories
            _bookingManager = new BookingManager(
                _bookingRepositoryMock.Object,
                _roomRepositoryMock.Object
            );
        }

        [Given(@"the hotel has the following rooms:")]
        public Task GivenTheHotelHasTheFollowingRooms(Table table)
        {
            var rooms = table.CreateSet<Room>();
            _liveRooms.Clear();
            _liveRooms.AddRange(rooms);
            return Task.CompletedTask;
        }


        [Given(@"the following bookings exist:")]
        public Task GivenTheFollowingBookingsExist(Table table)
        {
            var bookings = table.CreateSet<Booking>();
            _liveBookings.Clear();
            foreach (var b in bookings)
            {
                b.IsActive = true;   // IMPORTANT!
                _liveBookings.Add(b);
            }

            // Setup repository to use live list
            _bookingRepositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(() => _liveBookings.ToList());

            _bookingRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Booking>()))
                .Returns((Booking b) =>
                {
                    _liveBookings.Add(b);
                    return Task.CompletedTask;
                });

            return Task.CompletedTask;
        }


        [When(@"I book a room from (.*) to (.*)")]
        public async Task WhenIBookARoomFromTo(DateTime startDate, DateTime endDate)
        {
            var booking = new Booking
            {
                StartDate = startDate,
                EndDate = endDate
            };

            bool result = await _bookingManager.CreateBooking(booking);
            _scenarioContext["BookingResult"] = result;

            if (result)
                _scenarioContext["BookingId"] = booking.Id;

            _outputHelper.WriteLine($"Booking attempt from {startDate} to {endDate}: {(result ? "Success" : "Failed")}");
        }

        [Then(@"the booking should be created successfully")]
        public Task ThenTheBookingShouldBeCreatedSuccessfully()
        {
            var bookingCreated = _scenarioContext["BookingResult"] as bool?;
            Assert.True(bookingCreated == true, "Expected booking to be created successfully.");
            return Task.CompletedTask;
        }

        [Then(@"the booking should fail due to no availability")]
        public Task ThenTheBookingShouldFailDueToNoAvailability()
        {
            var bookingCreated = _scenarioContext["BookingResult"] as bool?;
            Assert.True(bookingCreated == false, "Expected booking to fail due to no available rooms.");
            return Task.CompletedTask;
        }
    }
}

