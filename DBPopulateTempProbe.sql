drop database as_e6179eb49bd1184;
CREATE SCHEMA as_e6179eb49bd1184;

USE as_e6179eb49bd1184;
CREATE TABLE devices
(
	deviceId int primary key,
	warning bool,
	active bool
);

CREATE TABLE readings
(
readingNum int primary key AUTO_INCREMENT,
deviceId int,
currentTemp double,
longitude double,
latitude double,
readingTime datetime,
CONSTRAINT FOREIGN KEY (deviceId)
	REFERENCES devices(deviceId)
);