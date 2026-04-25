/* 
1. DIRECTOR
id - первичный ключ
name - ФИО режиссёра (уникальное)
country - страна (опционально)
*/

CREATE TABLE director (
    id      SERIAL PRIMARY KEY,
    name    TEXT NOT NULL UNIQUE,
    country TEXT
);

/* 
2. FILM
id - первичный ключ
title - название фильма
release_year - год релиза (CHECK)
primary_director_id - внешний ключ на DIRECTOR(id)

Связь: 1:N - один режиссёр может быть основным для многих фильмов
*/

CREATE TABLE film (
    id                   SERIAL PRIMARY KEY,
    title                TEXT NOT NULL,
    release_year         INTEGER NOT NULL
        CHECK (release_year BETWEEN 1900 AND EXTRACT(YEAR FROM CURRENT_DATE)),
    primary_director_id  INTEGER NOT NULL
);

/* 
3. FILM_INFO
film_id - внешний ключ на FILM(id) и одновременно первичный ключ
duration_minutes - длительность (CHECK > 0)
rating - возрастной рейтинг (CHECK IN)
budget_usd - бюджет

Связь: 1:1 - на каждый фильм ровно одна запись в FILM_INFO
*/

CREATE TABLE film_info (
    film_id          INTEGER PRIMARY KEY,
    duration_minutes INTEGER NOT NULL
        CHECK (duration_minutes > 0),
    rating           TEXT NOT NULL
        CHECK (rating IN ('G','PG','PG-13','R','NC-17')),
    budget_usd       NUMERIC(15, 2) NOT NULL
);

/* 
4. FILM_CREDIT
film_id - внешний ключ на FILM(id)
director_id - внешний ключ на DIRECTOR(id)
role - роль участника (director, co-director, producer)

Связь: M:N - многие режиссёры участвуют во многих фильмах
PK (film_id, director_id, role)
*/

CREATE TABLE film_credit (
    film_id     INTEGER NOT NULL,
    director_id INTEGER NOT NULL,
    role        TEXT NOT NULL,
    PRIMARY KEY (film_id, director_id, role)
);

/* 
Ограничения целостности и связи между таблицами

1:N DIRECTOR -> FILM
удаление режиссёра запрещено, если он основной
*/

ALTER TABLE film
    ADD CONSTRAINT film_primary_director_fk
        FOREIGN KEY (primary_director_id)
        REFERENCES director (id)
        ON DELETE RESTRICT;

/*
1:1 FILM <-> FILM_INFO
удаление фильма удаляет единственную запись film_info
*/

ALTER TABLE film_info
    ADD CONSTRAINT film_info_film_fk
        FOREIGN KEY (film_id)
        REFERENCES film (id)
        ON DELETE CASCADE;

/*
M:N через FILM_CREDIT
удаление фильма или режиссёра очищает связи
*/

ALTER TABLE film_credit
    ADD CONSTRAINT film_credit_film_fk
        FOREIGN KEY (film_id)
        REFERENCES film (id)
        ON DELETE CASCADE;

ALTER TABLE film_credit
    ADD CONSTRAINT film_credit_director_fk
        FOREIGN KEY (director_id)
        REFERENCES director (id)
        ON DELETE CASCADE;

/*

hw=# \i 10.sql
CREATE TABLE
CREATE TABLE
CREATE TABLE
CREATE TABLE
ALTER TABLE
ALTER TABLE
ALTER TABLE
ALTER TABLE
hw=# \dt
            List of tables
 Schema |    Name     | Type  | Owner 
--------+-------------+-------+-------
 public | customers   | table | hw
 public | director    | table | hw
 public | film        | table | hw
 public | film_credit | table | hw
 public | film_info   | table | hw
(5 rows)

hw=# \d director
                             Table "public.director"
 Column  |  Type   | Collation | Nullable |               Default                
---------+---------+-----------+----------+--------------------------------------
 id      | integer |           | not null | nextval('director_id_seq'::regclass)
 name    | text    |           | not null | 
 country | text    |           |          | 
Indexes:
    "director_pkey" PRIMARY KEY, btree (id)
    "director_name_key" UNIQUE CONSTRAINT, btree (name)
Referenced by:
    TABLE "film_credit" CONSTRAINT "film_credit_director_fk" FOREIGN KEY (director_id) REFERENCES director(id) ON DELETE CASCADE
    TABLE "film" CONSTRAINT "film_primary_director_fk" FOREIGN KEY (primary_director_id) REFERENCES director(id) ON DELETE RESTRICT

hw=# \d film
                                   Table "public.film"
       Column        |  Type   | Collation | Nullable |             Default              
---------------------+---------+-----------+----------+----------------------------------
 id                  | integer |           | not null | nextval('film_id_seq'::regclass)
 title               | text    |           | not null | 
 release_year        | integer |           | not null | 
 primary_director_id | integer |           | not null | 
Indexes:
    "film_pkey" PRIMARY KEY, btree (id)
Check constraints:
    "film_release_year_check" CHECK (release_year >= 1900 AND release_year::numeric <= EXTRACT(year FROM CURRENT_DATE))
hw=# \d film_info
                     Table "public.film_info"
      Column      |     Type      | Collation | Nullable | Default 
------------------+---------------+-----------+----------+---------
 film_id          | integer       |           | not null | 
 duration_minutes | integer       |           | not null | 
 rating           | text          |           | not null | 
 budget_usd       | numeric(15,2) |           | not null | 
Indexes:
    "film_info_pkey" PRIMARY KEY, btree (film_id)
Check constraints:
    "film_info_duration_minutes_check" CHECK (duration_minutes > 0)
    "film_info_rating_check" CHECK (rating = ANY (ARRAY['G'::text, 'PG'::text, 'PG-13'::text, 'R'::text, 'NC-17'::text]))
Foreign-key constraints:
    "film_info_film_fk" FOREIGN KEY (film_id) REFERENCES film(id) ON DELETE CASCADE

hw=# \d film_credit
               Table "public.film_credit"
   Column    |  Type   | Collation | Nullable | Default 
-------------+---------+-----------+----------+---------
 film_id     | integer |           | not null | 
 director_id | integer |           | not null | 
 role        | text    |           | not null | 
Indexes:
    "film_credit_pkey" PRIMARY KEY, btree (film_id, director_id, role)
Foreign-key constraints:
    "film_credit_director_fk" FOREIGN KEY (director_id) REFERENCES director(id) ON DELETE CASCADE
    "film_credit_film_fk" FOREIGN KEY (film_id) REFERENCES film(id) ON DELETE CASCADE

ТЕСТ
--------------------------------------------------------------------------------------------------

hw=# -- добавим режиссёра
INSERT INTO director (name, country)
VALUES ('Christopher Nolan', 'UK');
INSERT 0 1
hw=# -- добавим фильм, где он основной режиссёр 1:N
INSERT INTO film (title, release_year, primary_director_id)
VALUES ('Inception', 2010, 1);
INSERT 0 1
hw=# -- добавим 1:1 запись в film_info
INSERT INTO film_info (film_id, duration_minutes, rating, budget_usd)
VALUES (1, 148, 'PG-13', 160000000);
INSERT 0 1
hw=# -- добавим участие в фильме M:N
INSERT INTO film_credit (film_id, director_id, role)
VALUES (1, 1, 'director');
INSERT 0 1

hw=# SELECT * FROM director;
 id |       name        | country 
----+-------------------+---------
  1 | Christopher Nolan | UK
(1 row)

hw=# SELECT * FROM film;
 id |   title   | release_year | primary_director_id 
----+-----------+--------------+---------------------
  1 | Inception |         2010 |                   1
(1 row)

hw=# SELECT * FROM film_info;
 film_id | duration_minutes | rating |  budget_usd  
---------+------------------+--------+--------------
       1 |              148 | PG-13  | 160000000.00
(1 row)

hw=# SELECT * FROM film_credit;
 film_id | director_id |   role   
---------+-------------+----------
       1 |           1 | director
(1 row)



hw=# -- эта команда должна вызвать ошибку из за ON DELETE RESTRICT
DELETE FROM director WHERE id = 1;
ERROR:  update or delete on table "director" violates foreign key constraint "film_primary_director_fk" on table "film"
DETAIL:  Key (id)=(1) is still referenced from table "film".
hw=# 



hw=# -- удаляем фильм
DELETE FROM film WHERE id = 1;
DELETE 1
hw=# 
hw=# -- связанные записи в film_info и film_credit должны исчезнуть
SELECT * FROM film_info;
 film_id | duration_minutes | rating | budget_usd 
---------+------------------+--------+------------
(0 rows)

hw=# SELECT * FROM film_credit;
 film_id | director_id | role 
---------+-------------+------
(0 rows)

hw=# 
*/