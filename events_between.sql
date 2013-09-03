-- A procedure to get all events in the WordPress in a given time interval
DELIMITER //
DROP PROCEDURE IF EXISTS `events_between` //
CREATE PROCEDURE `events_between` (IN beginning CHAR(8), IN end CHAR(8))
BEGIN
    SELECT pp_postmeta.meta_value AS EventID, post_id AS PostID
    FROM pp_postmeta INNER JOIN
    (SELECT post_id FROM pp_postmeta WHERE meta_key = 'date_year' AND beginning <= meta_value
    AND meta_value <= end) AS cur_events USING (post_id)
    WHERE pp_postmeta.meta_key = 'wpcf-eventor-id' AND pp_postmeta.meta_value != '';
END//

-- CALL events_between('20130701', '20130801') //
