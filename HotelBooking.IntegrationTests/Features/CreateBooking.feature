Feature: CreateBooking
  In order to manage hotel reservations
  As a booking manager
  I want to create bookings only when rooms are available

  Background:
    Given the hotel has the following rooms:
      | Id  | Description |
      | 101 | Single      |
      | 102 | Double      |

  Scenario: Create a new booking when one of two rooms is available
    And the following bookings exist:
      | Id  | StartDate  | EndDate    | RoomId |
      | 201 | 3025-09-01 | 3025-09-05 | 101    |
    When I book a room from 3025-09-03 to 3025-09-06
    Then the booking should be created successfully

  Scenario: Fail to create a booking when no room is available
    And the following bookings exist:
      | Id  | StartDate  | EndDate    | RoomId |
      | 301 | 3025-09-01 | 3025-09-07 | 101    |
      | 302 | 3025-09-01 | 3025-09-07 | 102    |
    When I book a room from 3025-09-03 to 3025-09-06
    Then the booking should fail due to no availability

