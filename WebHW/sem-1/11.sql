-- Схема
DROP TABLE IF EXISTS trips;
CREATE TABLE trips (
  trip_id        SERIAL PRIMARY KEY,
  trip_date      DATE        NOT NULL,
  city           TEXT        NOT NULL CHECK (city IN ('Stockholm', 'Gothenburg', 'Malmo', 'Uppsala')),
  rider_type     TEXT        NOT NULL CHECK (rider_type IN ('new', 'loyal', 'corporate')),
  payment_method TEXT        NOT NULL CHECK (payment_method IN ('card', 'cash', 'wallet')),
  status         TEXT        NOT NULL CHECK (status IN ('requested', 'accepted', 'completed', 'cancelled', 'refunded')),
  distance_km    NUMERIC(5,2) NOT NULL CHECK (distance_km > 0),
  base_fare      NUMERIC(6,2) NOT NULL CHECK (base_fare >= 0),
  surge          NUMERIC(3,2) NOT NULL CHECK (surge BETWEEN 1.0 AND 2.5),
  tip            NUMERIC(5,2) NOT NULL DEFAULT 0 CHECK (tip >= 0),
  discount       NUMERIC(5,2) NOT NULL DEFAULT 0 CHECK (discount >= 0)
);

-- Наполнение (28 строк)
INSERT INTO trips (trip_date,city,rider_type,payment_method,status,distance_km,base_fare,surge,tip,discount) VALUES
('2025-01-03','Stockholm','new','card','completed',   4.2, 120, 1.2,  10,  0),
('2025-01-04','Stockholm','loyal','wallet','completed',7.8, 150, 1.0,   0, 10),
('2025-01-05','Stockholm','corporate','card','cancelled',3.1,100, 1.1,   0,  0),
('2025-01-07','Stockholm','new','cash','completed',   2.6,  90, 1.3,   5,  0),
('2025-01-10','Stockholm','loyal','card','refunded',  5.0, 140, 1.1,   0,  0),
('2025-02-02','Gothenburg','new','card','completed',  6.5, 130, 1.5,   8,  0),
('2025-02-03','Gothenburg','loyal','wallet','completed',9.4,170, 1.2,   0, 15),
('2025-02-04','Gothenburg','corporate','card','completed',11.0,220,1.1,  0,  0),
('2025-02-06','Gothenburg','new','cash','cancelled',  1.8,  80, 1.0,   0,  0),
('2025-02-09','Gothenburg','loyal','card','completed', 3.9,  95, 1.6,   4,  0),
('2025-03-01','Malmo','new','wallet','completed',     5.7, 125, 1.4,   6,  5),
('2025-03-02','Malmo','loyal','card','completed',     8.2, 160, 1.1,   0,  0),
('2025-03-03','Malmo','corporate','cash','completed', 12.6,240, 1.3,   0,  0),
('2025-03-05','Malmo','new','card','cancelled',       2.0,  85, 1.0,   0,  0),
('2025-03-07','Malmo','loyal','wallet','refunded',    6.0, 135, 1.2,   0,  0),
('2025-03-10','Uppsala','new','card','completed',     3.3,  95, 1.0,   2,  0),
('2025-03-11','Uppsala','loyal','cash','completed',   7.1, 155, 1.3,   0, 10),
('2025-03-12','Uppsala','corporate','card','completed',10.4,210,1.5,   0,  0),
('2025-03-14','Uppsala','new','wallet','cancelled',   1.5,  70, 1.0,   0,  0),
('2025-03-16','Uppsala','loyal','card','completed',   4.8, 110, 1.2,   3,  0),
('2025-04-01','Stockholm','corporate','card','completed',13.2,260,1.4,  0,  0),
('2025-04-02','Stockholm','new','wallet','completed',  6.9,145, 1.1,   5,  0),
('2025-04-03','Stockholm','loyal','card','completed',  9.0,175, 1.7,   0, 20),
('2025-04-04','Gothenburg','corporate','wallet','refunded',9.2,200,1.2, 0,  0),
('2025-04-06','Gothenburg','new','card','completed',   5.4,120, 1.8,   7,  0),
('2025-04-07','Malmo','loyal','card','completed',      7.6,150, 1.0,   0, 10),
('2025-04-08','Malmo','new','cash','completed',        3.1, 90,  1.9,  3,  0),
('2025-04-09','Uppsala','corporate','card','cancelled', 2.2,100, 1.0,  0,  0);

/*

NOTICE:  table "trips" does not exist, skipping
DROP TABLE
CREATE TABLE
INSERT 0 28
hw=# \dt
            List of tables
 Schema |    Name     | Type  | Owner 
--------+-------------+-------+-------
 public | customers   | table | hw
 public | director    | table | hw
 public | film        | table | hw
 public | film_credit | table | hw
 public | film_info   | table | hw
 public | trips       | table | hw
(6 rows)

*/

-- 1) CASE в SELECT
SELECT
  trip_id,
  city,
  status,
  CASE
    WHEN status = 'completed' THEN base_fare * surge + tip - discount
    ELSE 0
  END AS net_charge,
  CASE
    WHEN distance_km <= 4 THEN 'short'
    WHEN distance_km <= 8 THEN 'mid'
    ELSE 'long'
  END AS distance_band
FROM trips
ORDER BY trip_date, trip_id;

/*

 trip_id |    city    |  status   | net_charge | distance_band 
---------+------------+-----------+------------+---------------
       1 | Stockholm  | completed |   154.0000 | mid
       2 | Stockholm  | completed |   140.0000 | mid
       3 | Stockholm  | cancelled |          0 | short
       4 | Stockholm  | completed |   122.0000 | short
       5 | Stockholm  | refunded  |          0 | mid
       6 | Gothenburg | completed |   203.0000 | mid
       7 | Gothenburg | completed |   189.0000 | long
       8 | Gothenburg | completed |   242.0000 | long
       9 | Gothenburg | cancelled |          0 | short
      10 | Gothenburg | completed |   156.0000 | short
      11 | Malmo      | completed |   176.0000 | mid
      12 | Malmo      | completed |   176.0000 | long
      13 | Malmo      | completed |   312.0000 | long
      14 | Malmo      | cancelled |          0 | short
      15 | Malmo      | refunded  |          0 | mid
      16 | Uppsala    | completed |    97.0000 | short
      17 | Uppsala    | completed |   191.5000 | mid
      18 | Uppsala    | completed |   315.0000 | long
      19 | Uppsala    | cancelled |          0 | short
      20 | Uppsala    | completed |   135.0000 | mid
      21 | Stockholm  | completed |   364.0000 | long
      22 | Stockholm  | completed |   164.5000 | mid
      23 | Stockholm  | completed |   277.5000 | long
      24 | Gothenburg | refunded  |          0 | long
      25 | Gothenburg | completed |   223.0000 | mid
      26 | Malmo      | completed |   140.0000 | mid
      27 | Malmo      | completed |   174.0000 | short
      28 | Uppsala    | cancelled |          0 | short
(28 rows)

*/

-- 2) CASE в WHERE
SELECT
  trip_id,
  rider_type,
  distance_km
FROM trips
WHERE distance_km > CASE
  WHEN rider_type = 'new' THEN 5
  WHEN rider_type = 'loyal' THEN 8
  ELSE 10
END
ORDER BY trip_id;

/*

 trip_id | rider_type | distance_km 
---------+------------+-------------
       6 | new        |        6.50
       7 | loyal      |        9.40
       8 | corporate  |       11.00
      11 | new        |        5.70
      12 | loyal      |        8.20
      13 | corporate  |       12.60
      18 | corporate  |       10.40
      21 | corporate  |       13.20
      22 | new        |        6.90
      23 | loyal      |        9.00
      25 | new        |        5.40
(11 rows)

*/

-- 3) Простой GROUP BY с несколькими агрегаторами
SELECT
  city,
  COUNT(*) AS trips_cnt,
  SUM(distance_km) AS sum_distance,
  AVG(surge) AS avg_surge,
  MAX(tip) AS max_tip
FROM trips
GROUP BY city
ORDER BY sum_distance DESC;

/*

    city    | trips_cnt | sum_distance |     avg_surge      | max_tip 
------------+-----------+--------------+--------------------+---------
 Stockholm  |         8 |        51.80 | 1.2375000000000000 |   10.00
 Gothenburg |         7 |        47.20 | 1.3428571428571429 |    8.00
 Malmo      |         7 |        45.20 | 1.2714285714285714 |    6.00
 Uppsala    |         6 |        29.30 | 1.1666666666666667 |    3.00
(4 rows)

*/

-- 4) GROUP BY с FILTER
SELECT
  city,
  SUM(base_fare * surge + tip - discount) FILTER (WHERE status = 'completed') AS delivered_revenue,
  COUNT(*) FILTER (WHERE status = 'cancelled') AS cancel_cnt,
  (COUNT(*) FILTER (WHERE status = 'cancelled'))::numeric / COUNT(*) AS cancel_rate
FROM trips
GROUP BY city
ORDER BY cancel_rate DESC;

/*

    city    | delivered_revenue | cancel_cnt |      cancel_rate       
------------+-------------------+------------+------------------------
 Uppsala    |          738.5000 |          2 | 0.33333333333333333333
 Malmo      |          978.0000 |          1 | 0.14285714285714285714
 Gothenburg |         1013.0000 |          1 | 0.14285714285714285714
 Stockholm  |         1222.0000 |          1 | 0.12500000000000000000
(4 rows)

*/

-- 5) GROUP BY с HAVING
SELECT
  payment_method,
  COUNT(*) AS trips_cnt,
  COUNT(*) FILTER (WHERE status = 'cancelled') AS cancel_cnt,
  SUM(base_fare * surge + tip - discount) FILTER (WHERE status = 'completed') AS delivered_revenue
FROM trips
GROUP BY payment_method
HAVING
  (COUNT(*) FILTER (WHERE status = 'cancelled'))::numeric / COUNT(*) > 0.15
  OR SUM(base_fare * surge + tip - discount) FILTER (WHERE status = 'completed') > 1200
ORDER BY payment_method;

/*

 payment_method | trips_cnt | cancel_cnt | delivered_revenue 
----------------+-----------+------------+-------------------
 card           |        16 |          3 |         2482.5000
 cash           |         5 |          1 |          799.5000
(2 rows)

*/