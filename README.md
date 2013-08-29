Eventor
=======

Eventor synchronisation for Centrum OK


O-Ringen 2013 har Eventor-ID 5113. Det här kan du göra i MySQL.

mysql> CALL event(5113) ;
+----------------+-----------------------+---------------------+---------
| Name           | Url                   | StartDate           | FinishDa
 +----------------+-----------------------+---------------------+---------
| O-Ringen Boden | http://www.oringen.se | 2013-07-21 08:30:00 | 2013-07-
 +----------------+-----------------------+---------------------+---------

mysql> CALL documents(5113);
+------------------------+------------------------------
| Name                   | Url
+------------------------+------------------------------
| Inbjudan               | http://www.oringen.se/downloa
| Terrängbeskrivning mm  | http://www.oringen.se/terrang
| Startlistor            | http://www.oringen.se/webdav/
| PM                     | http://np.netpublicator.com/n
| Programme              | http://np.netpublicator.com/n
+------------------------+------------------------------

mysql> CALL races(5113);
+----+---------+---------------------+----------+----------+----------+----------+
| Id | Name    | Date                | X        | Y        | Daylight | Distance |
+----+---------+---------------------+----------+----------+----------+----------+
|  1 | Etapp 1 | 2013-07-21 08:30:00 | 21.65713 | 65.77678 |        1 | Long     |
|  2 | Etapp 2 | 2013-07-22 08:30:00 | 21.65662 | 65.77671 |        1 | Long     |
|  3 | Etapp 3 | 2013-07-24 08:30:00 | 21.24738 | 66.02169 |        1 | Long     |
|  4 | Etapp 4 | 2013-07-25 08:30:00 | 21.24764 | 66.02117 |        1 | Middle   |
|  5 | Etapp 5 | 2013-07-26 08:30:00 | 21.69859 | 65.79976 |        1 | Middle   |
+----+---------+---------------------+----------+----------+----------+----------+

Nu är det viktigt att spara Id till nån race. Vi tar Ettap 4, så Id=4.

mysql> CALL classes(4) ;
+------------------+--------+-----------+
| Name             | Length | NoRunners |
 +------------------+--------+-----------+
| D21E             |   5310 |      NULL |
| H21E             |   6150 |      NULL |
| H21L             |   4920 |       184 |
| D21              |   3640 |       148 |
| H21              |   4690 |       189 |
| D21K             |   3210 |       147 |
| H21K             |   4110 |       214 |

NoRunners = NULL är fel här, men det spelar ingen roll eftersom centrumare sprang inte där.

mysql> CALL results(4) ;
+--------+-----------------------+----------+-------+------------+--------+
| Class  | Name                  | Position | Time  | Difference | Status |
 +--------+-----------------------+----------+-------+------------+--------+
| D21    | Moa Kjellstrand       |        7 | 31:40 | 6:22       | OK     |
| D21    | Malin Annegård        |       16 | 32:35 | 7:17       | OK     |
| D21    | Karin Fägerlind       |       37 | 36:26 | 11:08      | OK     |
| D21    | Sofia Andersson       |       39 | 36:43 | 11:25      | OK     |
| D21    | Gro Dahlbom           |     NULL | NULL  | NULL       |        |
| D21    | Sara Jonasson         |     NULL | NULL  | NULL       | felst. |
| D21K   | Elina Johnsson        |        6 | 30:26 | 2:31       | OK     |

mysql> CALL startlist(4);
+---------+-----------------------+-----------+
| Class   | Name                  | StartTime |
+---------+-----------------------+-----------+
| D21     | Gro Dahlbom           | 09:55:00  |
| D21     | Sofia Andersson       | 10:06:00  |
| D21     | Karin Fägerlind       | 10:17:00  |
| D21     | Sara Jonasson         | 10:21:00  |
| D21     | Malin Annegård        | 10:38:00  |
| D21     | Moa Kjellstrand       | 10:53:00  |
| D21K    | Anna Weglin Elenius   | 09:53:00  |
