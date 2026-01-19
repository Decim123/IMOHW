DROP TABLE IF EXISTS orders_import_lines;
CREATE TABLE orders_import_lines (
  id serial PRIMARY KEY,
  source_file text NOT NULL,   -- имя файла/источника
  line_no int NOT NULL,        -- номер строки
  raw_line text NOT NULL,      -- необработанная строка
  imported_at timestamptz default now(),
  note text
);

INSERT INTO orders_import_lines (source_file, line_no, raw_line, note) VALUES
-- Контакты покупателей: email в угловых скобках и простые варианты, телефоны в разных форматах
('marketplace_A_2025_11.csv', 1, 'Order#1001; Customer: Olga Petrova <olga.petrova@example.com>; +7 (921) 555-12-34; Items: SKU:AB-123-XY x1', 'order row'),
('marketplace_A_2025_11.csv', 2, 'Order#1002; Customer: Ivan <ivan@@example..com>; 8-921-5551234; Items: SKU:zx9999 x2', 'order row'),
('newsletter_upload.csv', 10, 'john.doe@domain.com; +44 7700 900123; tags: promo, holiday', 'marketing upload'),

-- Цены с разделителями тысяч и валютой
('pricing_feed.csv', 3, 'product: ZX-11; price: "1,299.99" USD', 'price row'),
('pricing_feed.csv', 4, 'product: Y-200; price: "2 500,00" EUR', 'price row'),

-- Теги/категории в поле tags:
('catalog_tags.csv', 1, 'tags: electronics, mobile,  accessories', 'tags row'),
('catalog_tags.csv', 2, 'tags: home,kitchen', 'tags row'),

-- «Грязные» CSV-строки: запятые внутри полей, кавычки
('orders_dirty.csv', 5, '"Smith, John","12 Baker St, Apt 4","1,200.00","SKU: AB-123-XY"', 'dirty csv'),

-- Логи обработки: разного регистра, ошибки и предупреждения
('processor_log.txt', 100, 'INFO: Processing order 1001', 'log'),
('processor_log.txt', 101, 'warning: price parse failed for line 4', 'log'),
('processor_log.txt', 102, 'Error: invalid phone for order 1002', 'log'),
('processor_log.txt', 103, 'error: missing sku in items list', 'log'),

-- Ловушки / edge-cases для проверки наивных regex
('marketplace_A_2025_11.csv', 20, 'Customer: bad@-domain.com; +7 921 ABC-12-34; Items: SKU: 12-AB-!!', 'trap-invalid-email-phone-sku'),
('orders_dirty.csv', 6, E'"O\'Connor, Liam","New York, NY","500"', 'dirty csv with apostrophe');

-- ('orders_dirty.csv', 6, '"O\'Connor, Liam","New York, NY","500"', 'dirty csv with apostrophe'); было так без E перед Connor но я не смог это вставить c E '\' вставился

/*

NOTICE:  table "orders_import_lines" does not exist, skipping
DROP TABLE
CREATE TABLE

*/

-- 1) Строки с корректным email через ~
SELECT id, source_file, line_no, raw_line
FROM orders_import_lines
WHERE raw_line ~ '([A-Za-z0-9._%+\-]+)@([A-Za-z0-9\-]+\.)+[A-Za-z]{2,}'
ORDER BY id;

/*

 id |        source_file        | line_no |                                                  raw_line                                                  
----+---------------------------+---------+------------------------------------------------------------------------------------------------------------
  1 | marketplace_A_2025_11.csv |       1 | Order#1001; Customer: Olga Petrova <olga.petrova@example.com>; +7 (921) 555-12-34; Items: SKU:AB-123-XY x1
  3 | newsletter_upload.csv     |      10 | john.doe@domain.com; +44 7700 900123; tags: promo, holiday
 13 | marketplace_A_2025_11.csv |      20 | Customer: bad@-domain.com; +7 921 ABC-12-34; Items: SKU: 12-AB-!!
(3 rows)

*/

-- 2) Строки без корректного email через !~
SELECT id, source_file, line_no, raw_line
FROM orders_import_lines
WHERE raw_line !~ '([A-Za-z0-9._%+\-]+)@([A-Za-z0-9\-]+\.)+[A-Za-z]{2,}'
ORDER BY id;

/*

 id |        source_file        | line_no |                                       raw_line                                       
----+---------------------------+---------+--------------------------------------------------------------------------------------
  2 | marketplace_A_2025_11.csv |       2 | Order#1002; Customer: Ivan <ivan@@example..com>; 8-921-5551234; Items: SKU:zx9999 x2
  4 | pricing_feed.csv          |       3 | product: ZX-11; price: "1,299.99" USD
  5 | pricing_feed.csv          |       4 | product: Y-200; price: "2 500,00" EUR
  6 | catalog_tags.csv          |       1 | tags: electronics, mobile,  accessories
  7 | catalog_tags.csv          |       2 | tags: home,kitchen
  8 | orders_dirty.csv          |       5 | "Smith, John","12 Baker St, Apt 4","1,200.00","SKU: AB-123-XY"
  9 | processor_log.txt         |     100 | INFO: Processing order 1001
 10 | processor_log.txt         |     101 | warning: price parse failed for line 4
 11 | processor_log.txt         |     102 | Error: invalid phone for order 1002
 12 | processor_log.txt         |     103 | error: missing sku in items list
 14 | orders_dirty.csv          |       6 | "O'Connor, Liam","New York, NY","500"
(11 rows)

*/

-- 3) Извлечь первый email через regexp_match
SELECT
  id,
  source_file,
  line_no,
  (regexp_match(raw_line, '([A-Za-z0-9._%+\-]+@(?:[A-Za-z0-9\-]+\.)+[A-Za-z]{2,})'))[1] AS email
FROM orders_import_lines
WHERE raw_line ~ '([A-Za-z0-9._%+\-]+)@([A-Za-z0-9\-]+\.)+[A-Za-z]{2,}'
ORDER BY id;

/*

 id |        source_file        | line_no |          email           
----+---------------------------+---------+--------------------------
  1 | marketplace_A_2025_11.csv |       1 | olga.petrova@example.com
  3 | newsletter_upload.csv     |      10 | john.doe@domain.com
 13 | marketplace_A_2025_11.csv |      20 | bad@-domain.com
(3 rows)

*/

-- 4) Извлечь все SKU через regexp_matches (множественные совпадения)
SELECT
  o.id,
  o.source_file,
  o.line_no,
  m[1] AS sku
FROM orders_import_lines AS o
CROSS JOIN LATERAL regexp_matches(
  o.raw_line,
  '\m[A-Za-z]{1,3}-?\d{1,4}(?:-[A-Za-z]{1,3})?\M|\m[A-Za-z]{2}\d{3,6}\M',
  'g'
) AS m
ORDER BY o.id, sku;

/*

 id |        source_file        | line_no |    sku    
----+---------------------------+---------+-----------
  1 | marketplace_A_2025_11.csv |       1 | AB-123-XY
  1 | marketplace_A_2025_11.csv |       1 | x1
  2 | marketplace_A_2025_11.csv |       2 | x2
  2 | marketplace_A_2025_11.csv |       2 | zx9999
  4 | pricing_feed.csv          |       3 | ZX-11
  5 | pricing_feed.csv          |       4 | Y-200
  8 | orders_dirty.csv          |       5 | AB-123-XY
 13 | marketplace_A_2025_11.csv |      20 | ABC-12
(8 rows)

*/

-- 5) Нормализация телефона через regexp_replace
SELECT
  id,
  source_file,
  line_no,
  regexp_replace(raw_line, '\D', '', 'g') AS phone_digits
FROM orders_import_lines
WHERE raw_line ~ '(\+?\d[\d\s().-]{7,}\d)'
ORDER BY id;

/*

 id |        source_file        | line_no |     phone_digits     
----+---------------------------+---------+----------------------
  1 | marketplace_A_2025_11.csv |       1 | 1001792155512341231
  2 | marketplace_A_2025_11.csv |       2 | 10028921555123499992
  3 | newsletter_upload.csv     |      10 | 447700900123
(3 rows)

*/

-- 6) Преобразовать цену в числовой формат
WITH p AS (
  SELECT
    id,
    source_file,
    line_no,
    (regexp_match(raw_line, 'price:\s*"?([0-9][0-9\s,\.]*[0-9])"?'))[1] AS price_txt
  FROM orders_import_lines
  WHERE raw_line ~ 'price:\s*"?[0-9]'
)
SELECT
  id,
  source_file,
  line_no,
  CASE
    WHEN price_txt ~ '\.' AND price_txt ~ ',' THEN
      regexp_replace(price_txt, '[\s,]', '', 'g')::numeric
    WHEN price_txt ~ ',' THEN
      replace(regexp_replace(price_txt, '\s', '', 'g'), ',', '.')::numeric
    ELSE
      regexp_replace(price_txt, '\s', '', 'g')::numeric
  END AS price_value
FROM p
ORDER BY id;

/*

 id |   source_file    | line_no | price_value 
----+------------------+---------+-------------
  4 | pricing_feed.csv |       3 |     1299.99
  5 | pricing_feed.csv |       4 |     2500.00
(2 rows)

*/

-- 7) Для строк с tags: разбить в массив, убрать лишние пробелы/пустые элементы
SELECT
  id,
  source_file,
  line_no,
  array_remove(
    regexp_split_to_array(
      regexp_replace((regexp_match(raw_line, 'tags:\s*(.*)'))[1], '\s+', '', 'g'),
      ','
    ),
    ''
  ) AS tags_arr
FROM orders_import_lines
WHERE raw_line ~ 'tags:\s*'
ORDER BY id;

/*

 id |      source_file      | line_no |             tags_arr             
----+-----------------------+---------+----------------------------------
  3 | newsletter_upload.csv |      10 | {promo,holiday}
  6 | catalog_tags.csv      |       1 | {electronics,mobile,accessories}
  7 | catalog_tags.csv      |       2 | {home,kitchen}
(3 rows)

*/

-- 8) orders_dirty.csv: разбить на поля (каждое поле отдельной строкой) через regexp_split_to_table
SELECT
  id,
  source_file,
  line_no,
  btrim(regexp_replace(field, '^"|"$', '', 'g')) AS field_value
FROM orders_import_lines
CROSS JOIN LATERAL regexp_split_to_table(
  raw_line,
  ',(?=(?:[^"]*"[^"]*")*[^"]*$)'
) AS field
WHERE source_file = 'orders_dirty.csv'
ORDER BY id, field_value;

/*

 id |   source_file    | line_no |    field_value     
----+------------------+---------+--------------------
  8 | orders_dirty.csv |       5 | 1,200.00
  8 | orders_dirty.csv |       5 | 12 Baker St, Apt 4
  8 | orders_dirty.csv |       5 | SKU: AB-123-XY
  8 | orders_dirty.csv |       5 | Smith, John
 14 | orders_dirty.csv |       6 | 500
 14 | orders_dirty.csv |       6 | New York, NY
 14 | orders_dirty.csv |       6 | O'Connor, Liam
(7 rows)

*/

-- 9) В логах найти строки со словом error в любом регистре через ~*
SELECT id, source_file, line_no, raw_line
FROM orders_import_lines
WHERE source_file = 'processor_log.txt'
  AND raw_line ~* '\merror\M'
ORDER BY id;

/*

 id |    source_file    | line_no |              raw_line               
----+-------------------+---------+-------------------------------------
 11 | processor_log.txt |     102 | Error: invalid phone for order 1002
 12 | processor_log.txt |     103 | error: missing sku in items list
(2 rows)

*/

-- 10) В логах заменить error (любой регистр) на ERROR (глобально) через regexp_replace
SELECT
  id,
  source_file,
  line_no,
  regexp_replace(raw_line, 'error', 'ERROR', 'gi') AS raw_line_fixed
FROM orders_import_lines
WHERE source_file = 'processor_log.txt'
ORDER BY id;

/*

SELECT
  id,
  source_file,
  line_no,
  regexp_replace(raw_line, 'error', 'ERROR', 'gi') AS raw_line_fixed
FROM orders_import_lines
WHERE source_file = 'processor_log.txt'
ORDER BY id;


*/