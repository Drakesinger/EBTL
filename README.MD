# (Emergency) Blood Type Locator

## Program Description
(Emergency) Blood Type Locator which from now on will be called (E)BTL is an application used for emergency localization of people with a specific blood type.
The program allows one to suscribe to a database with the following private information:
* Name
* Surname
* Address
* Emergency phone number
* Blood-Type
* Geo-Location

The Geo-Location is a service that can be activated all the time or at specific intervals and send this geo-location information to the application's server.

In case of an emergency, the emergency services use the desktop application to launch an alert for a specific blood-type and the person who's nearest the emmiter will be notified that he/she can save a life.

## Technology

## Features
### Main Features

A (universal) desktop app used by the emergency services:
* [ ]  Shows a map
* [ ]  Active geo-fenced phones are shown on the map
* [ ]  Clicking on a geo-fence shows information about that person
* [ ]  Search for a specific blood-type
     * [ ] In an specified area radius
* [ ]  Launch an alert for a blood-type

A (universal) desktop app used by volunteers:
* [ ] Allows to subscribe with the following information:
    * [x] Name
    * [x] Surname
    * [x] Address
    * [ ] Emergency phone number
    * [x] Blood-Type

### Optional Features

Communication between the emergency and user application:
* [ ] Send notifications (a modal pop-up)
* [ ] Asks the user if he aggrees with sending real-time geo-location data to the server
* [ ] Update the database with the new location of the user    
* [ ] Mobile App for users (Xamarin for cross platform)
