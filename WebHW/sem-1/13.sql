-- ==============================================
-- TRAVEL BOOKING — DDL + SEED
-- ==============================================

DROP TABLE IF EXISTS booking CASCADE;
DROP TABLE IF EXISTS hotel_room CASCADE;
DROP TABLE IF EXISTS tourist_city CASCADE;
DROP TABLE IF EXISTS city CASCADE;
DROP TABLE IF EXISTS hotel CASCADE;
DROP TABLE IF EXISTS room_type CASCADE;
DROP TABLE IF EXISTS tourist CASCADE;
DROP TABLE IF EXISTS country CASCADE;

-- -------------------------
-- Страны
CREATE TABLE country (
    id SERIAL PRIMARY KEY,
    name TEXT NOT NULL,
    region TEXT
);

-- -------------------------
-- Города
CREATE TABLE city (
    id SERIAL PRIMARY KEY,
    name TEXT NOT NULL,
    country_id INT REFERENCES country(id) ON DELETE SET NULL,
    population INT
);

-- -------------------------
-- Туристы
CREATE TABLE tourist (
    id SERIAL PRIMARY KEY,
    name TEXT NOT NULL,
    birth_year INT
);

-- -------------------------
-- MtM: турист ⇄ город
CREATE TABLE tourist_city (
    tourist_id INT NOT NULL REFERENCES tourist(id) ON DELETE CASCADE,
    city_id INT NOT NULL REFERENCES city(id) ON DELETE CASCADE,
    visited_at DATE NOT NULL,
    PRIMARY KEY (tourist_id, city_id)
);

-- -------------------------
-- Отели
CREATE TABLE hotel (
    id SERIAL PRIMARY KEY,
    city_id INT REFERENCES city(id) ON DELETE SET NULL,
    name TEXT NOT NULL,
    stars INT,
    year_opened INT
);

-- -------------------------
-- Типы номеров
CREATE TABLE room_type (
    id SERIAL PRIMARY KEY,
    title TEXT NOT NULL,
    max_guests INT
);

-- -------------------------
-- MtM: отель ⇄ тип номера
CREATE TABLE hotel_room (
    hotel_id INT NOT NULL REFERENCES hotel(id) ON DELETE CASCADE,
    room_type_id INT NOT NULL REFERENCES room_type(id) ON DELETE CASCADE,
    rooms_available INT NOT NULL CHECK (rooms_available >= 0),
    PRIMARY KEY (hotel_id, room_type_id)
);

-- -------------------------
-- Бронирования
CREATE TABLE booking (
    id SERIAL PRIMARY KEY,
    tourist_id INT REFERENCES tourist(id),
    hotel_id INT REFERENCES hotel(id),
    room_type_id INT REFERENCES room_type(id),
    nights INT CHECK (nights > 0),
    check_in DATE,
    total_price NUMERIC
);

-- ======================================================
-- SEED DATA
-- ======================================================

INSERT INTO country (id, name, region) VALUES
(1, 'Италия', 'Европа'),
(2, 'Япония', 'Азия'),
(3, 'Чили', 'Южная Америка'),
(4, 'Исландия', 'Европа'),
(5, 'Неизвестная страна', NULL);

INSERT INTO city (id, name, country_id, population) VALUES
(1, 'Рим', 1, 2800000),
(2, 'Милан', 1, 1400000),
(3, 'Токио', 2, 14000000),
(4, 'Саппоро', 2, 1900000),
(5, 'Сантьяго', 3, 5600000),
(6, 'Пунта-Аренас', 3, 130000),
(7, 'Рейкьявик', 4, 150000),
(8, 'Город-призрак', NULL, NULL);

INSERT INTO tourist (id, name, birth_year) VALUES
(1, 'Александр', 1990),
(2, 'Марина', 1985),
(3, 'Роберт', 1975),
(4, 'Турист без городов', 2000);

INSERT INTO tourist_city (tourist_id, city_id, visited_at) VALUES
(1, 1, '2022-05-10'),
(1, 3, '2023-11-01'),
(2, 2, '2024-02-12'),
(2, 4, '2025-01-15'),
(3, 5, '2025-03-18');

INSERT INTO hotel (id, city_id, name, stars, year_opened) VALUES
(1, 1, 'Roma Center Hotel', 5, 1990),
(2, 1, 'Budget Inn Rome', 3, 2005),
(3, 3, 'Tokyo Sky Hotel', 4, 2012),
(4, 3, 'Tiny Capsule Hotel', 2, 2018),
(5, 7, 'IceView Hotel', 4, 2020),
(6, NULL, 'Hotel Nowhere', 1, 2000);

INSERT INTO room_type (id, title, max_guests) VALUES
(1, 'Standard', 2),
(2, 'Deluxe', 3),
(3, 'Suite', 4),
(4, 'Capsule', 1);

INSERT INTO hotel_room (hotel_id, room_type_id, rooms_available) VALUES
(1, 1, 10),
(1, 2, 5),
(2, 1, 30),
(3, 1, 50),
(3, 3, 10),
(4, 4, 100),
(5, 3, 3);

INSERT INTO booking (tourist_id, hotel_id, room_type_id, nights, check_in, total_price) VALUES
(1, 1, 2, 3, '2025-10-10', 420),
(1, 3, 1, 5, '2025-11-15', 700),
(2, 4, 4, 2, '2025-09-01', 180),
(3, 5, 3, 1, '2025-07-22', 300);

/*

NOTICE:  table "booking" does not exist, skipping
DROP TABLE
NOTICE:  table "hotel_room" does not exist, skipping
DROP TABLE
NOTICE:  table "tourist_city" does not exist, skipping
DROP TABLE
NOTICE:  table "city" does not exist, skipping
DROP TABLE
NOTICE:  table "hotel" does not exist, skipping
DROP TABLE
NOTICE:  table "room_type" does not exist, skipping
DROP TABLE
NOTICE:  table "tourist" does not exist, skipping
DROP TABLE
NOTICE:  table "country" does not exist, skipping
DROP TABLE
CREATE TABLE
CREATE TABLE
CREATE TABLE
CREATE TABLE
CREATE TABLE
CREATE TABLE
CREATE TABLE
CREATE TABLE
INSERT 0 5
INSERT 0 8
INSERT 0 4
INSERT 0 5
INSERT 0 6
INSERT 0 4
INSERT 0 7
INSERT 0 4

*/

-- 1.1 INNER JOIN: отели + город + регион страны
SELECT
    h.name AS hotel_name,
    c.name AS city_name,
    co.region
FROM hotel AS h
JOIN city AS c
  ON c.id = h.city_id
JOIN country AS co
  ON co.id = c.country_id
ORDER BY h.id;

/*

     hotel_name     | city_name | region 
--------------------+-----------+--------
 Roma Center Hotel  | Рим       | Европа
 Budget Inn Rome    | Рим       | Европа
 Tokyo Sky Hotel    | Токио     | Азия
 Tiny Capsule Hotel | Токио     | Азия
 IceView Hotel      | Рейкьявик | Европа
(5 rows)

*/

-- 1.2 INNER JOIN: туристы и города, которые они посетили
SELECT
    t.name AS tourist_name,
    c.name AS city_name,
    tc.visited_at
FROM tourist_city AS tc
JOIN tourist AS t
  ON t.id = tc.tourist_id
JOIN city AS c
  ON c.id = tc.city_id
ORDER BY t.id, tc.visited_at;

/*

 tourist_name | city_name | visited_at 
--------------+-----------+------------
 Александр    | Рим       | 2022-05-10
 Александр    | Токио     | 2023-11-01
 Марина       | Милан     | 2024-02-12
 Марина       | Саппоро   | 2025-01-15
 Роберт       | Сантьяго  | 2025-03-18
(5 rows)

*/

-- 2.1 LEFT JOIN: все города и отели в них (города без отелей тоже)
SELECT
    c.name AS city_name,
    h.name AS hotel_name,
    h.stars,
    h.year_opened
FROM city AS c
LEFT JOIN hotel AS h
  ON h.city_id = c.id
ORDER BY c.id, h.id;

/*

   city_name   |     hotel_name     | stars | year_opened 
---------------+--------------------+-------+-------------
 Рим           | Roma Center Hotel  |     5 |        1990
 Рим           | Budget Inn Rome    |     3 |        2005
 Милан         |                    |       |            
 Токио         | Tokyo Sky Hotel    |     4 |        2012
 Токио         | Tiny Capsule Hotel |     2 |        2018
 Саппоро       |                    |       |            
 Сантьяго      |                    |       |            
 Пунта-Аренас  |                    |       |            
 Рейкьявик     | IceView Hotel      |     4 |        2020
 Город-призрак |                    |       |            
(10 rows)

*/

-- 2.2 LEFT JOIN + GROUP BY: кол-во посещенных городов по туристам (включая без посещений)
SELECT
    t.id,
    t.name,
    COUNT(tc.city_id) AS cities_visited_cnt
FROM tourist AS t
LEFT JOIN tourist_city AS tc
  ON tc.tourist_id = t.id
GROUP BY t.id, t.name
ORDER BY t.id;

/*

 id |        name        | cities_visited_cnt 
----+--------------------+--------------------
  1 | Александр          |                  2
  2 | Марина             |                  2
  3 | Роберт             |                  1
  4 | Турист без городов |                  0
(4 rows)

*/

-- 3.1 RIGHT JOIN: все страны и их города (страны без городов тоже)
SELECT
    co.name AS country_name,
    c.name AS city_name
FROM city AS c
RIGHT JOIN country AS co
  ON co.id = c.country_id
ORDER BY co.id, c.id;

/*

    country_name    |  city_name   
--------------------+--------------
 Италия             | Рим
 Италия             | Милан
 Япония             | Токио
 Япония             | Саппоро
 Чили               | Сантьяго
 Чили               | Пунта-Аренас
 Исландия           | Рейкьявик
 Неизвестная страна | 
(8 rows)

*/

-- 3.2 RIGHT JOIN: для каждого типа номера кол-во отелей, где он есть (включая 0)
SELECT
    rt.title,
    COUNT(hr.hotel_id) AS hotels_cnt
FROM hotel_room AS hr
RIGHT JOIN room_type AS rt
  ON rt.id = hr.room_type_id
GROUP BY rt.id, rt.title
ORDER BY rt.id;

/*

  title   | hotels_cnt 
----------+------------
 Standard |          3
 Deluxe   |          1
 Suite    |          2
 Capsule  |          1
(4 rows)

*/

-- 4.1 FULL JOIN: все комбинации город—отель (города без отелей и отели без города)
SELECT
    c.name AS city_name,
    h.name AS hotel_name
FROM city AS c
FULL JOIN hotel AS h
  ON h.city_id = c.id
ORDER BY city_name, hotel_name;

/*

   city_name   |     hotel_name     
---------------+--------------------
 Город-призрак | 
 Милан         | 
 Пунта-Аренас  | 
 Рейкьявик     | IceView Hotel
 Рим           | Budget Inn Rome
 Рим           | Roma Center Hotel
 Сантьяго      | 
 Саппоро       | 
 Токио         | Tiny Capsule Hotel
 Токио         | Tokyo Sky Hotel
               | Hotel Nowhere
(11 rows)

*/

-- 4.2 FULL JOIN: все типы номеров и все отели, чтобы были видны все случаи
SELECT
    h.name AS hotel_name,
    rt.title AS room_type_title,
    hr.rooms_available
FROM hotel AS h
FULL JOIN hotel_room AS hr
  ON hr.hotel_id = h.id
FULL JOIN room_type AS rt
  ON rt.id = hr.room_type_id
ORDER BY hotel_name, room_type_title;

/*

     hotel_name     | room_type_title | rooms_available 
--------------------+-----------------+-----------------
 Budget Inn Rome    | Standard        |              30
 Hotel Nowhere      |                 |                
 IceView Hotel      | Suite           |               3
 Roma Center Hotel  | Deluxe          |               5
 Roma Center Hotel  | Standard        |              10
 Tiny Capsule Hotel | Capsule         |             100
 Tokyo Sky Hotel    | Standard        |              50
 Tokyo Sky Hotel    | Suite           |              10
(8 rows)

*/

-- 5.1 CROSS JOIN: все пары (страна, тип номера)
SELECT
    co.name AS country_name,
    rt.title AS room_type_title
FROM country AS co
CROSS JOIN room_type AS rt
ORDER BY co.id, rt.id;

/*

    country_name    | room_type_title 
--------------------+-----------------
 Италия             | Standard
 Италия             | Deluxe
 Италия             | Suite
 Италия             | Capsule
 Япония             | Standard
 Япония             | Deluxe
 Япония             | Suite
 Япония             | Capsule
 Чили               | Standard
 Чили               | Deluxe
 Чили               | Suite
 Чили               | Capsule
 Исландия           | Standard
 Исландия           | Deluxe
 Исландия           | Suite
 Исландия           | Capsule
 Неизвестная страна | Standard
 Неизвестная страна | Deluxe
 Неизвестная страна | Suite
 Неизвестная страна | Capsule
(20 rows)

*/

-- 5.2 CROSS JOIN: все сочетания (город, год открытия отеля) по уникальным year_opened
SELECT
    c.name AS city_name,
    y.year_opened
FROM city AS c
CROSS JOIN (
    SELECT DISTINCT year_opened
    FROM hotel
    WHERE year_opened IS NOT NULL
) AS y
ORDER BY c.id, y.year_opened;

/*

   city_name   | year_opened 
---------------+-------------
 Рим           |        1990
 Рим           |        2000
 Рим           |        2005
 Рим           |        2012
 Рим           |        2018
 Рим           |        2020
 Милан         |        1990
 Милан         |        2000
 Милан         |        2005
 Милан         |        2012
 Милан         |        2018
 Милан         |        2020
 Токио         |        1990
 Токио         |        2000
 Токио         |        2005
 Токио         |        2012
 Токио         |        2018
 Токио         |        2020
 Саппоро       |        1990
 Саппоро       |        2000
 Саппоро       |        2005
 Саппоро       |        2012
 Саппоро       |        2018
 Саппоро       |        2020
 Сантьяго      |        1990
 Сантьяго      |        2000
 Сантьяго      |        2005
 Сантьяго      |        2012
 Сантьяго      |        2018
 Сантьяго      |        2020
 Пунта-Аренас  |        1990
 Пунта-Аренас  |        2000
 Пунта-Аренас  |        2005
 Пунта-Аренас  |        2012
 Пунта-Аренас  |        2018
 Пунта-Аренас  |        2020
 Рейкьявик     |        1990
 Рейкьявик     |        2000
 Рейкьявик     |        2005
 Рейкьявик     |        2012
 Рейкьявик     |        2018
 Рейкьявик     |        2020
 Город-призрак |        1990
 Город-призрак |        2000
 Город-призрак |        2005
 Город-призрак |        2012
 Город-призрак |        2018
 Город-призрак |        2020
(48 rows)

*/

-- 6.1 LATERAL JOIN: для каждого города один отель с макс. суммой rooms_available
SELECT
    c.id AS city_id,
    c.name AS city_name,
    h_best.hotel_id,
    h_best.hotel_name,
    h_best.total_rooms
FROM city AS c
LEFT JOIN LATERAL (
    SELECT
        h.id AS hotel_id,
        h.name AS hotel_name,
        COALESCE(SUM(hr.rooms_available), 0) AS total_rooms
    FROM hotel AS h
    LEFT JOIN hotel_room AS hr
      ON hr.hotel_id = h.id
    WHERE h.city_id = c.id
    GROUP BY h.id, h.name
    ORDER BY total_rooms DESC, h.id
    LIMIT 1
) AS h_best
  ON TRUE
ORDER BY c.id;

/*

 city_id |   city_name   | hotel_id |     hotel_name     | total_rooms 
---------+---------------+----------+--------------------+-------------
       1 | Рим           |        2 | Budget Inn Rome    |          30
       2 | Милан         |          |                    |            
       3 | Токио         |        4 | Tiny Capsule Hotel |         100
       4 | Саппоро       |          |                    |            
       5 | Сантьяго      |          |                    |            
       6 | Пунта-Аренас  |          |                    |            
       7 | Рейкьявик     |        5 | IceView Hotel      |           3
       8 | Город-призрак |          |                    |            
(8 rows)

*/

-- 6.2 LATERAL JOIN: для каждого туриста один последний визит (по дате)
SELECT
    t.id AS tourist_id,
    t.name AS tourist_name,
    v.city_id,
    v.city_name,
    v.visited_at
FROM tourist AS t
LEFT JOIN LATERAL (
    SELECT
        c.id AS city_id,
        c.name AS city_name,
        tc.visited_at
    FROM tourist_city AS tc
    JOIN city AS c
      ON c.id = tc.city_id
    WHERE tc.tourist_id = t.id
    ORDER BY tc.visited_at DESC, c.id
    LIMIT 1
) AS v
  ON TRUE
ORDER BY t.id;

/*

 tourist_id |    tourist_name    | city_id | city_name | visited_at 
------------+--------------------+---------+-----------+------------
          1 | Александр          |       3 | Токио     | 2023-11-01
          2 | Марина             |       4 | Саппоро   | 2025-01-15
          3 | Роберт             |       5 | Сантьяго  | 2025-03-18
          4 | Турист без городов |         |           | 
(4 rows)

*/

-- 7.1 SELF JOIN: пары городов в одной стране без A-A и без дубликатов
SELECT
    c1.name AS city_1,
    c2.name AS city_2,
    co.name AS country_name
FROM city AS c1
JOIN city AS c2
  ON c2.country_id = c1.country_id
 AND c2.id > c1.id
JOIN country AS co
  ON co.id = c1.country_id
ORDER BY co.id, c1.id, c2.id;

/*

  city_1  |    city_2    | country_name 
----------+--------------+--------------
 Рим      | Милан        | Италия
 Токио    | Саппоро      | Япония
 Сантьяго | Пунта-Аренас | Чили
(3 rows)

*/

-- 7.2 SELF JOIN: пары туристов с одним годом рождения без дубликатов
SELECT
    t1.name AS tourist_1,
    t2.name AS tourist_2,
    t1.birth_year
FROM tourist AS t1
JOIN tourist AS t2
  ON t2.birth_year = t1.birth_year
 AND t2.id > t1.id
ORDER BY t1.birth_year, t1.id, t2.id;

/*

 tourist_1 | tourist_2 | birth_year 
-----------+-----------+------------
(0 rows)

*/