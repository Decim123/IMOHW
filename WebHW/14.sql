-- Создать таблицу с полями id, username, phone, city, created_at.

DROP TABLE IF EXISTS users_perf;
CREATE TABLE users_perf (
    id SERIAL PRIMARY KEY,
    username TEXT NOT NULL,
    phone TEXT NOT NULL,
    city TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

/*

NOTICE:  table "users_perf" does not exist, skipping
DROP TABLE
CREATE TABLE

*/

-- Заполнить таблицу большим объёмом случайных данных.

INSERT INTO users_perf (username, phone, city, created_at)
SELECT
    'user_' || g::text AS username,
    '79' || lpad((floor(random() * 1000000000))::bigint::text, 9, '0') AS phone,
    (ARRAY[
        'Москва',
        'Санкт-Петербург',
        'Новосибирск',
        'Екатеринбург',
        'Казань',
        'Нижний Новгород',
        'Самара',
        'Омск',
        'Ростов-на-Дону',
        'Уфа',
        'Красноярск',
        'Ульяновск'
    ])[1 + floor(random() * 12)::int] AS city,
    NOW() - ((random() * 365)::int || ' days')::interval
FROM generate_series(1, 500000) AS g;

/*

INSERT 0 500000

*/

-- Выполнить выборку по точному совпадению значения столбца phone и замерить время выполнения.

EXPLAIN ANALYZE
WITH mid AS (
    SELECT phone
    FROM users_perf
    ORDER BY id
    OFFSET (
        SELECT COUNT(*) / 2
        FROM users_perf
    )
    LIMIT 1
)
SELECT *
FROM users_perf
WHERE phone = (SELECT phone FROM mid);


/*

                                                                                   QUERY PLAN                                                                                   
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
 Gather  (cost=11823.86..19809.13 rows=1 width=54) (actual time=90.825..95.031 rows=1 loops=1)
   Workers Planned: 2
   Params Evaluated: $2
   Workers Launched: 2
   InitPlan 2 (returns $2)
     ->  Subquery Scan on mid  (cost=10823.82..10823.86 rows=1 width=12) (actual time=69.915..69.988 rows=1 loops=1)
           ->  Limit  (cost=10823.82..10823.85 rows=1 width=16) (actual time=69.914..69.986 rows=1 loops=1)
                 InitPlan 1 (returns $1)
                   ->  Finalize Aggregate  (cost=8985.38..8985.39 rows=1 width=8) (actual time=26.889..26.960 rows=1 loops=1)
                         ->  Gather  (cost=8985.17..8985.38 rows=2 width=8) (actual time=26.794..26.953 rows=3 loops=1)
                               Workers Planned: 2
                               Workers Launched: 2
                               ->  Partial Aggregate  (cost=7985.17..7985.18 rows=1 width=8) (actual time=18.356..18.358 rows=1 loops=3)
                                     ->  Parallel Seq Scan on users_perf users_perf_1  (cost=0.00..7464.33 rows=208333 width=0) (actual time=0.005..11.158 rows=166667 loops=3)
                 ->  Index Scan using users_perf_pkey on users_perf users_perf_2  (cost=0.42..18380.42 rows=500000 width=16) (actual time=0.036..35.703 rows=250001 loops=1)
   ->  Parallel Seq Scan on users_perf  (cost=0.00..7985.17 rows=1 width=54) (actual time=14.125..17.107 rows=0 loops=3)
         Filter: (phone = $2)
         Rows Removed by Filter: 166666
 Planning Time: 0.631 ms
 Execution Time: 95.130 ms
(20 rows)

*/

-- Создать обычный индекс на столбец phone.

CREATE INDEX idx_users_perf_phone ON users_perf (phone);

/*

CREATE INDEX

*/

-- Повторить выборку из п.3 и сравнить время с предыдущим измерением.

EXPLAIN ANALYZE
WITH mid AS (
    SELECT phone
    FROM users_perf
    ORDER BY id
    OFFSET (
        SELECT COUNT(*) / 2
        FROM users_perf
    )
    LIMIT 1
)
SELECT *
FROM users_perf
WHERE phone = (SELECT phone FROM mid);

/*

                                                                                   QUERY PLAN                                                                                   
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
 Index Scan using idx_users_perf_phone on users_perf  (cost=10824.29..10832.30 rows=1 width=54) (actual time=63.553..63.625 rows=1 loops=1)
   Index Cond: (phone = $2)
   InitPlan 2 (returns $2)
     ->  Subquery Scan on mid  (cost=10823.82..10823.86 rows=1 width=12) (actual time=63.491..63.562 rows=1 loops=1)
           ->  Limit  (cost=10823.82..10823.85 rows=1 width=16) (actual time=63.490..63.560 rows=1 loops=1)
                 InitPlan 1 (returns $1)
                   ->  Finalize Aggregate  (cost=8985.38..8985.39 rows=1 width=8) (actual time=23.172..23.242 rows=1 loops=1)
                         ->  Gather  (cost=8985.17..8985.38 rows=2 width=8) (actual time=23.079..23.234 rows=3 loops=1)
                               Workers Planned: 2
                               Workers Launched: 2
                               ->  Partial Aggregate  (cost=7985.17..7985.18 rows=1 width=8) (actual time=18.401..18.401 rows=1 loops=3)
                                     ->  Parallel Seq Scan on users_perf users_perf_1  (cost=0.00..7464.33 rows=208333 width=0) (actual time=0.007..11.704 rows=166667 loops=3)
                 ->  Index Scan using users_perf_pkey on users_perf users_perf_2  (cost=0.42..18380.42 rows=500000 width=16) (actual time=0.037..33.363 rows=250001 loops=1)
 Planning Time: 0.430 ms
 Execution Time: 63.699 ms
(15 rows)

*/

-- Выполнить выборку по подстроке в столбце city с использованием условия ILIKE '%…%' и замерить время выполнения.

EXPLAIN ANALYZE
SELECT *
FROM users_perf
WHERE city ILIKE '%ск%';

/*

                                                     QUERY PLAN                                                      
---------------------------------------------------------------------------------------------------------------------
 Seq Scan on users_perf  (cost=0.00..11631.00 rows=209567 width=54) (actual time=0.018..336.195 rows=208203 loops=1)
   Filter: (city ~~* '%ск%'::text)
   Rows Removed by Filter: 291797
 Planning Time: 0.129 ms
 Execution Time: 342.267 ms
(5 rows)

*/

-- Создать индекс на вычисляемое выражение, ускоряющее выполнение запроса из п.6.

CREATE INDEX idx_users_perf_city_lower ON users_perf (lower(city));

/*

CREATE INDEX

*/

-- Повторить выборку из п.6 и сравнить время с предыдущим измерением.

EXPLAIN ANALYZE
SELECT *
FROM users_perf
WHERE lower(city) LIKE '%ск%';

/*

                                                          QUERY PLAN                                                           
-------------------------------------------------------------------------------------------------------------------------------
 Gather  (cost=1000.00..9906.00 rows=4000 width=54) (actual time=0.337..108.604 rows=208203 loops=1)
   Workers Planned: 2
   Workers Launched: 2
   ->  Parallel Seq Scan on users_perf  (cost=0.00..8506.00 rows=1667 width=54) (actual time=0.023..93.866 rows=69401 loops=3)
         Filter: (lower(city) ~~ '%ск%'::text)
         Rows Removed by Filter: 97266
 Planning Time: 0.386 ms
 Execution Time: 116.578 ms
(8 rows)

*/