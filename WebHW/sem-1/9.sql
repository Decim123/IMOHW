-- Подготовка окружения
DROP TABLE IF EXISTS account CASCADE;

-- Стартовая "неидеальная" схема
CREATE TABLE account (
    id              serial PRIMARY KEY,
    full_name       text,
    phone           text,                 -- хранится без нормализации, возможны пробелы/дефисы
    balance_cents   integer,              -- баланс в центах
    status          text,                 -- ожидаются значения: active | blocked | pending
    registered_at   timestamp DEFAULT now(),
    note            text                  -- временное поле для черновиков
);

-- Посев данных (намеренно неоднородные телефоны/статусы)
INSERT INTO account (full_name, phone, balance_cents, status, registered_at, note) VALUES
('Alice Johnson',    ' +46 70-123-45-67 ', 125000, 'active',  now() - interval '400 days', NULL),
('Bob Smith',        '+46-73-555-00-11',   8999,  'pending',  now() - interval '320 days', 'promo'),
('Charlie Brown',    '070 222 33 44',     159900, 'active',   now() - interval '280 days', NULL),
('Diana Prince',     '073-111-22-33',     4599,   'blocked',  now() - interval '250 days', NULL),
('Evan Lee',         '070-999-88-77',     219900, 'active',   now() - interval '200 days', 'vip'),
('Fiona Adams',      '0735550012',        9900,   NULL,       now() - interval '190 days', NULL),
('George Miller',    '070-700-70-70',     45999,  'active',   now() - interval '175 days', NULL),
('Hannah Davis',     '073 333 44 55',     2999,   'pending',  now() - interval '160 days', NULL),
('Ian Wright',       '+46 70 101 20 30',  119999, 'active',   now() - interval '150 days', NULL),
('Julia Stone',      '070-000-00-01',     25999,  'blocked',  now() - interval '140 days', NULL),
('Kevin Park',       '073-222-22-22',     34999,  'active',   now() - interval '130 days', NULL),
('Laura Chen',       '070-010-0100',      49999,  'active',   now() - interval '120 days', 'gift'),
('Mark Green',       '073-777-77-77',     1299,   'pending',  now() - interval '110 days', NULL),
('Nina Patel',       '070-234-56-78',     1899,   'active',   now() - interval '100 days', NULL),
('Oscar Diaz',       '+46-73-700-80-90',  45999,  'active',   now() - interval '95 days',  NULL),
('Paula Gomez',      '070 888 99 00',     219999, 'blocked',  now() - interval '80 days',  NULL),
('Quinn Baker',      '073-000-12-34',     89999,  'active',   now() - interval '70 days',  NULL),
('Rita Ora',         '070-333-66-99',     16999,  'pending',  now() - interval '60 days',  NULL),
('Sam Young',        '070-444-55-66',     13999,  'active',   now() - interval '45 days',  NULL),
('Tina King',        '073-111-00-00',     299999, 'active',   now() - interval '30 days',  NULL),
('Uma Reed',         '070-222-00-00',     9900,   NULL,       now() - interval '20 days',  NULL),
('Victor Hugo',      '073-123-45-67',     4999,   'active',   now() - interval '15 days',  NULL),
('Wendy Frost',      '070-765-43-21',     12345,  'blocked',  now() - interval '10 days',  NULL),
('Yara Novak',       '+46 73 987 65 43',  77777,  'active',   now() - interval '5 days',   NULL),
('Zack Cole',        '0700000002',        2500,   'pending',  now() - interval '2 days',   NULL);

ALTER TABLE account RENAME TO customers;

ALTER TABLE customers
    ALTER COLUMN balance_cents
    TYPE numeric(12,2)
    USING balance_cents / 100.0;

ALTER TABLE customers
    RENAME COLUMN balance_cents TO balance;

ALTER TABLE customers
    ADD COLUMN account_no text;

UPDATE customers
SET account_no =
    'ACC-' ||
    to_char(registered_at, 'YYYY') || '-' ||
    lpad(id::text, 5, '0');

ALTER TABLE customers
    ALTER COLUMN account_no SET NOT NULL;

ALTER TABLE customers
    ALTER COLUMN phone TYPE varchar(20);

ALTER TABLE customers
    ALTER COLUMN phone SET NOT NULL;

ALTER TABLE customers
    ADD CONSTRAINT customers_phone_uniq UNIQUE (phone);

ALTER TABLE customers
    ADD CONSTRAINT customers_status_chk
        CHECK (status IN ('active','blocked','pending'));

ALTER TABLE customers
    ALTER COLUMN status SET DEFAULT 'active';

/*


♰ skyceo 03:59 projects/IMOHW/WebHW
❯  psql -h localhost -U hw -d hw
psql (18.1, server 16.10)
Type "help" for help.

hw=# \i /home/skyceo/projects/IMOHW/WebHW/9.sql
psql:/home/skyceo/projects/IMOHW/WebHW/9.sql:2: NOTICE:  table "account" does not exist, skipping
DROP TABLE
CREATE TABLE
INSERT 0 25
ALTER TABLE
ALTER TABLE
ALTER TABLE
ALTER TABLE
UPDATE 25
ALTER TABLE
ALTER TABLE
ALTER TABLE
ALTER TABLE
ALTER TABLE
ALTER TABLE
hw=# \d customers
                                         Table "public.customers"
    Column     |            Type             | Collation | Nullable |               Default               
---------------+-----------------------------+-----------+----------+-------------------------------------
 id            | integer                     |           | not null | nextval('account_id_seq'::regclass)
 full_name     | text                        |           |          | 
 phone         | character varying(20)       |           | not null | 
 balance       | numeric(12,2)               |           |          | 
 status        | text                        |           |          | 'active'::text
 registered_at | timestamp without time zone |           |          | now()
 note          | text                        |           |          | 
 account_no    | text                        |           | not null | 
hw=# SELECT * FROM customers LIMIT 5;
 id |   full_name   |       phone        | balance | status  |       registered_at        | note  |   account_no   
----+---------------+--------------------+---------+---------+----------------------------+-------+----------------
  1 | Alice Johnson |  +46 70-123-45-67  | 1250.00 | active  | 2024-11-06 01:00:07.557649 |       | ACC-2024-00001
  2 | Bob Smith     | +46-73-555-00-11   |   89.99 | pending | 2025-01-25 01:00:07.557649 | promo | ACC-2025-00002
  3 | Charlie Brown | 070 222 33 44      | 1599.00 | active  | 2025-03-06 01:00:07.557649 |       | ACC-2025-00003
  4 | Diana Prince  | 073-111-22-33      |   45.99 | blocked | 2025-04-05 01:00:07.557649 |       | ACC-2025-00004
  5 | Evan Lee      | 070-999-88-77      | 2199.00 | active  | 2025-05-25 01:00:07.557649 | vip   | ACC-2025-00005
(5 rows)

hw=# 

*/