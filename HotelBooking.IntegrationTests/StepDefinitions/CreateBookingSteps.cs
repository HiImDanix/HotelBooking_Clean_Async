using System;
using Reqnroll;
using HotelBooking.Core;
using Moq;

using HotelBooking.IntegrationTests.StepDefinitions


  [Binding]
public class CreateBookingSteps
{
    private IBookingManager _bookingManager;
    private Moq.Mock<IRepository<Booking>> _bookingRepositoryMock;
    private Moq.Mock<IRepository<Room>> _roomRepositoryMock;

    private readonly IReqnrollOutputHelper _outputHelper;

    private readonly ScenarioContext _scenarioContext;


    public CreateBookingSteps(ScenarioContext scenarioContext, IReqnrollOutputHelper outputHelper)
    {
        _scenarioContext = scenarioContext;
        _outputHelper = outputHelper;

        _bookingRepositoryMock = new Moq.Mock<IRepository<Booking>>();
        _roomRepositoryMock = new Moq.Mock<IRepository<Room>>();

        _bookingManager = new BookingManager(_bookingRepositoryMock.Object, _roomRepositoryMock.Object);
    }

    [Given(@"the following rooms exist:")]

    public void GivenTheFollowingRoomsExist(Table table)
    {
        var rooms = table.CreateSet<Room>().ToList();
    }



    [When(@"I create a booking with the following details:")]





}






