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
        private readonly List<Booking> _liveBookings = new();
        private readonly List<Room> _liveRooms = new();

        public CreateBookingSteps(ScenarioContext scenarioContext, IReqnrollOutputHelper outputHelper)
        {
            _scenarioContext = scenarioContext;
            _outputHelper = outputHelper;

            _bookingRepositoryMock = new Mock<IRepository<Booking>>();
            _roomRepositoryMock = new Mock<IRepository<Room>>();

            SetupBookingRepositoryMock();
            _roomRepositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(() => _liveRooms.ToList());

            _bookingManager = new BookingManager(
                _bookingRepositoryMock.Object,
                _roomRepositoryMock.Object
            );
        }

        private void SetupBookingRepositoryMock()
        {
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
        }

        [Given(@"the hotel has the following rooms:")]
        public Task GivenTheHotelHasTheFollowingRooms(Table table)
        {
            _liveRooms.Clear();
            _liveRooms.AddRange(table.CreateSet<Room>());
            return Task.CompletedTask;
        }

        [Given(@"the following bookings exist:")]
        public Task GivenTheFollowingBookingsExist(Table table)
        {
            _liveBookings.Clear();
            foreach (var b in table.CreateSet<Booking>())
            {
                b.IsActive = true;
                _liveBookings.Add(b);
            }
            // No need to re-setup the mock here
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
            Assert.True(_scenarioContext["BookingResult"] as bool? == true, "Expected booking to be created successfully.");
            return Task.CompletedTask;
        }

        [Then(@"the booking should fail due to no availability")]
        public Task ThenTheBookingShouldFailDueToNoAvailability()
        {
            Assert.True(_scenarioContext["BookingResult"] as bool? == false, "Expected booking to fail due to no available rooms.");
            return Task.CompletedTask;
        }
    }
}

