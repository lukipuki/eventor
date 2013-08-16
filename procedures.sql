DELIMITER //
CREATE PROCEDURE `races` (IN eventID INT)
BEGIN
    SELECT Race.Id, Race.Name, Race.Date FROM (Race JOIN Event) WHERE Event.EventorID = eventID;
END//

CREATE PROCEDURE `class_info` (IN raceID INT)
BEGIN
    SELECT Class.Name, RaceClass.Length, RaceClass.NoRunners
    FROM (RaceClass JOIN Class ON RaceClass.ClassId = Class.Id)
    WHERE RaceClass.RaceId = raceID;
END//

CREATE FUNCTION `conv_time` (t BIGINT(20))
RETURNS VARCHAR(10) DETERMINISTIC
BEGIN
    DECLARE q DECIMAL;
    SET q = t / 10000000;
    RETURN CONCAT(FLOOR(q / 60), ':', LPAD(q % 60, 2, '0'));
END//

CREATE PROCEDURE `results` (IN raceID INT)
BEGIN
    SELECT Person.Name, Run.Position, conv_time(Run.Time) AS Time,
    conv_time(Run.TimeDiff) AS TimeDiff, Class.Name AS ClassName
    FROM Person JOIN Run ON Run.PersonId = Person.Id JOIN RaceClass ON Run.RaceClassId = RaceClass.Id
    JOIN Class ON Class.Id = RaceClass.ClassId
    WHERE RaceClass.RaceId = raceID
    ORDER BY Class.Name, COALESCE(Run.Position, 100000);
END//
