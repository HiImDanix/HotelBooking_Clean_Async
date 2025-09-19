Feature: Booking a room
    Scenario: User books a room
        Given two available rooms
        When user books a room
        Then the room should be booked