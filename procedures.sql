DELIMITER //

DROP PROCEDURE IF EXISTS `event` //
CREATE PROCEDURE `event` (IN eventID INT)
BEGIN
    SELECT Name, Url, StartDate, FinishDate
    FROM Event
    WHERE EventorID = eventID;
END//

DROP PROCEDURE IF EXISTS `races` //
CREATE PROCEDURE `races` (IN eventID INT)
BEGIN
    SELECT Race.Id, Race.Name, Race.Date, X, Y, Daylight, Distance
    FROM (Race JOIN Event) WHERE Event.EventorID = eventID;
END//

DROP PROCEDURE IF EXISTS `classes` //
CREATE PROCEDURE `classes` (IN raceID INT)
BEGIN
    SELECT Class.Name, Length, NoRunners
    FROM (RaceClass JOIN Class ON ClassId = Class.Id)
    WHERE RaceClass.RaceId = raceID;
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
    SELECT Class.Name AS Class, Person.Name, Position,
    conv_time(Run.Time) AS Time, conv_time(Run.TimeDiff) AS Difference, Status
    FROM Person JOIN Run ON PersonId = Person.Id JOIN RaceClass ON RaceClassId = RaceClass.Id
    JOIN Class ON Class.Id = ClassId
    WHERE RaceClass.RaceId = raceID
    ORDER BY Class.Name, COALESCE(Position, 100000);
END//

DROP PROCEDURE IF EXISTS `documents` //
CREATE PROCEDURE `documents` (IN eventID INT)
BEGIN
    SELECT Document.Name, Document.Url FROM Document JOIN Event WHERE  Event.EventorId = eventID;
END//
