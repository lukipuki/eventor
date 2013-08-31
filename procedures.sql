DELIMITER //

DROP PROCEDURE IF EXISTS `event` //
CREATE PROCEDURE `event` (IN eventID INT)
BEGIN
    SELECT Name, Url, StartDate, FinishDate, EntryBreak
    FROM Event
    WHERE EventorID = eventID;
END//

DROP PROCEDURE IF EXISTS `races` //
CREATE PROCEDURE `races` (IN event INT)
BEGIN
    SELECT Race.Id, Race.Name, Date, X, Y, Daylight, Distance
    FROM (Race JOIN Event ON Race.EventId = Event.Id)
    WHERE Event.EventorID = event
    ORDER BY Date;
END//

DROP PROCEDURE IF EXISTS `classes` //
CREATE PROCEDURE `classes` (IN raceID INT)
BEGIN
    SELECT Class.Name, Length, NoRunners
    FROM (RaceClass JOIN Class ON ClassId = Class.Id)
    WHERE RaceClass.RaceId = raceID
    ORDER BY Class.Name;
END//

DROP FUNCTION IF EXISTS `conv_time` //
CREATE FUNCTION `conv_time` (t BIGINT(20))
RETURNS VARCHAR(10) DETERMINISTIC
BEGIN
    DECLARE q DECIMAL;
    SET q = t / 10000000;
    RETURN CONCAT(FLOOR(q / 60), ':', LPAD(q % 60, 2, '0'));
END//

DROP PROCEDURE IF EXISTS `results` //
CREATE PROCEDURE `results` (IN raceID INT)
BEGIN
    SELECT Class.Name AS Class, Person.Name AS Person, Position,
    CONCAT(FLOOR(Run.Time / 600000000), ':', LPAD(ROUND(Run.Time / 10000000) % 60, 2, '0')) AS Time,
    CONCAT(FLOOR(Run.TimeDiff / 600000000), ':', LPAD(ROUND(Run.TimeDiff / 10000000) % 60, 2, '0'))
    AS Difference, Status, Club.Name AS ClubName
    FROM (Person JOIN Run ON PersonId = Person.Id JOIN RaceClass ON RaceClassId = RaceClass.Id
    JOIN Class ON Class.Id = ClassId) LEFT OUTER JOIN Club ON Person.ClubId = Club.Id
    WHERE RaceClass.RaceId = raceID AND Status IS NOT NULL
    ORDER BY Class.Name, COALESCE(Position, 100000);
END//

DROP PROCEDURE IF EXISTS `startlist` //
CREATE PROCEDURE `startlist` (IN raceID INT)
BEGIN
    SELECT Class.Name AS Class, Person.Name, DATE_FORMAT(StartTime, "%H:%i:%S") AS StartTime, SI
    FROM Person JOIN Run ON PersonId = Person.Id JOIN RaceClass ON RaceClassId = RaceClass.Id
    JOIN Class ON Class.Id = ClassId JOIN Club ON Person.ClubId = Club.Id
    WHERE RaceClass.RaceId = raceID AND Club.EventorId = 636 AND StartTime IS NOT NULL
    ORDER BY StartTime;
END//

DROP PROCEDURE IF EXISTS `documents` //
CREATE PROCEDURE `documents` (IN event INT)
BEGIN
    SELECT Document.Name, Document.Url
    FROM (Document JOIN Event ON Document.EventId = Event.Id)
    WHERE Event.EventorId = event;
END//

DROP PROCEDURE IF EXISTS `members` //
CREATE PROCEDURE `members` (IN cl INT)
BEGIN
    SELECT Person.Name, Phone, Address, EventorID
    FROM Person JOIN Club ON Person.ClubId = Club.Id
    WHERE Club.EventorID = cl;
END//

DROP PROCEDURE IF EXISTS `races_of_person` //
CREATE PROCEDURE `races_of_person` (IN pers INT)
BEGIN
    SELECT RaceId
    FROM Run JOIN RaceClass ON RaceClassId = RaceClass.Id JOIN Person ON Run.PersonId = Person.Id
    WHERE Person.EventorId = pers
    ORDER BY RaceId;
END//
