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
* [x]  Shows a map
* [x]  Active geo-fenced phones are shown on the map
* [ ]  Clicking on a geo-fence shows information about that person
    * For some reason this doesn't work anymore. So changed the icon to something more beautiful.
* [x]  Search for a specific blood-type
     * [ ] In an specified area radius
       * No real need
* [x]  Launch an alert for a blood-type

A (universal) desktop app used by volunteers:
* [x] Allows to subscribe with the following information:
    * [x] Name
    * [x] Surname
    * [x] Address
    * [x] Emergency phone number
    * [x] Blood-Type

### Optional Features

Communication between the emergency and user application:
* [x] Send notifications
* [x] Launch BINGMaps directly after notification has been received
* [x] Asks the user if he agrees with sending real-time geo-location data to the server
* [x] Update the database with the new location of the user    
* [ ] Mobile App for users (Xamarin for cross platform)

### Issues

Toast notification sending between apps requires a WNS service. Installation of the Azure Service nugget fails.
Code to use and implement this functionality has been written regardless.

Tried using app-service Communication to send data about users that subscribed directly to the EBTL-Control app. It fails for unknown reasons.
Since we cannot make it work, instead of populating the DB withthe mobile application suscribers, clicking on the map in EBTL-Control adds fictional users  (all of AB+ blood type...).

The Configuration screen that shows after the EBTL app has already been launched doesn't do anything.
