Feature: CreateBooking
  Scenario: Create a new booking when one of two rooms is available 
    Given the hotel has the following rooms:
      | Id         | Description|
      | 101        | Single     |
      | 102        | Double     |

    And the following bookings exist:
      | Id | StartDate | EndDate   | RoomId |
      | 101| 3025-9-01 | 3025-9-05 | 101    |

    When I book a room from 3025-9-03 to 3025-9-06

    Then the booking should be created successfully
