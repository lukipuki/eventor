Eventor
=======

Eventor synchronisation for Centrum OK

O-Ringen 2013 har Eventor-ID 5113. Det här kan man göra i MySQL.

    mysql> CALL event(5113);
    +----------------+------------  ----+---------------------+--------  ---+---------------------+
    | Name           | Url              | StartDate           | FinishDate  | EntryBreak          |
    +----------------+------------  ----+---------------------+--------  ---+---------------------+
    | O-Ringen Boden | http://www.  .se | 2013-07-21 08:30:00 | 2013-07  00 | 2013-06-02 00:00:00 |
    +----------------+------------  ----+---------------------+--------  ---+---------------------+

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
    |  8 | Etapp 1 | 2013-07-21 08:30:00 | 21.65713 | 65.77678 |        1 | Long     |
    |  9 | Etapp 2 | 2013-07-22 08:30:00 | 21.65662 | 65.77671 |        1 | Long     |
    | 10 | Etapp 3 | 2013-07-24 08:30:00 | 21.24738 | 66.02169 |        1 | Long     |
    | 11 | Etapp 4 | 2013-07-25 08:30:00 | 21.24764 | 66.02117 |        1 | Middle   |
    | 12 | Etapp 5 | 2013-07-26 08:30:00 | 21.69859 | 65.79976 |        1 | Middle   |
    +----+---------+---------------------+----------+----------+----------+----------+

Nu är det viktigt att spara Id till nån race. Vi tar Ettap 4, så Id=10.

    mysql> CALL classes(10);
    +------------------+--------+-----------+
    | Name             | Length | NoRunners |
    +------------------+--------+-----------+
    | D10              |   2420 |       144 |
    | D11              |   2620 |        99 |
    | D12              |   2880 |       107 |
    | D12K             |   2420 |         9 |
    | D13              |   3870 |       109 |
    | D14              |   3930 |       115 |
    | D14K             |   2900 |        22 |

    mysql> CALL startlist(10);
    +---------+-----------------------+-----------+------+
    | Class   | Name                  | StartTime | SI   |
    +---------+-----------------------+-----------+------+
    | H21M    | Henrik Hellborg       | 09:38:31  | NULL |
    | H21     | Mattias Henriksson    | 09:45:00  | NULL |
    | H35     | Joel Berring          | 09:51:00  | NULL |
    | Open 10 | Julia Dalgren         | 09:51:30  | NULL |
    | D21K    | Anna Weglin Elenius   | 09:53:00  | NULL |
    | H21K    | Kristoffer Edshage    | 09:54:00  | NULL |
    | D21     | Gro Dahlbom           | 09:55:00  | NULL |

    mysql> CALL results(10);
    +-------+----------------------+----------+--------+------------+----------+-------------------+
    | Class | Person               | Position | Time   | Difference | Status   | ClubName          |
    +-------+----------------------+----------+--------+------------+----------+-------------------+
    | D21   | Linnea Martinsson    |        1 | 50:32  | 0:00       | OK       | Järla Orientering |
    | D21   | Malin Annegård       |       20 | 64:26  | 13:54      | OK       | Centrum OK        |
    | D21   | Sofia Andersson      |       22 | 64:45  | 14:13      | OK       | Centrum OK        |
    | D21   | Moa Kjellstrand      |       27 | 65:45  | 15:13      | OK       | Centrum OK        |
    | D21   | Karin Fägerlind      |       80 | 77:08  | 26:36      | OK       | Centrum OK        |
    | D21   | Sara Jonasson        |      128 | 97:34  | 47:02      | OK       | Centrum OK        |
    | D21   | Gro Dahlbom          |     NULL | NULL   | NULL       | ej start | Centrum OK        |
    | D21K  | Amy Rankka           |        1 | 36:40  | 0:00       | OK       | Linköpings OK     |
    | D21K  | Elina Johnsson       |       17 | 42:44  | 6:04       | OK       | Centrum OK        |
    | D21K  | Emma Johansson       |       87 | 57:36  | 20:56      | OK       | Centrum OK        |
    | D21K  | Anna Weglin Elenius  |       92 | 59:11  | 22:31      | OK       | Centrum OK        |
    | D50   | Anne Magnusson       |        1 | 36:24  | 0:00       | OK       | OK Landehof       |
    | D50   | Marie Niska Thörnvik |     NULL | NULL   | NULL       | felst.   | Centrum OK        |

Man kan lista alla races för en person.

    mysql> CALL races_of_person(51999);
    +--------+
    | RaceId |
    +--------+
    |      2 |
    |     13 |
    +--------+

Och alla medlemmar.

    mysql> CALL members(636);
    +-----------------------+-------------+-------------------------------------------------+
    | Name                  | Phone       | Address                                         |
    +-----------------------+-------------+-------------------------------------------------+
    | Henrik Persson        | 07XXXXXXXXX | 12XXX, XXXXXXXXX, XXXXXXXXXXXXXXX 99 B lgh 1002,|
    | Sofia Andersson       | NULL        | 11XXX, XXXXXXXXX, XXXXXXXXXXXXXXX 50,           |
    | Anna Holmström        | 07XXXXXXXX  | 12XXX, XXXXXXXXX, XXXXXXXXXXXXXX 9 lgh 1201,    |
