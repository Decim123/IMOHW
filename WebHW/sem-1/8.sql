CREATE TABLE rentals (
    id            SERIAL PRIMARY KEY,
    customer_name TEXT    NOT NULL CHECK (length(trim(customer_name)) > 0),
    vehicle_class TEXT    NOT NULL CHECK (vehicle_class ~* '^(economy|compact|premium|suv.*)$'),
    pickup_city   TEXT    NOT NULL CHECK (length(trim(pickup_city)) > 0),
    daily_rate    NUMERIC(8,2) NOT NULL CHECK (daily_rate BETWEEN 10 AND 500),
    pickup_date   DATE    NOT NULL,
    return_date   DATE    NOT NULL,
    status        TEXT    NOT NULL CHECK (status IN ('Booked','Confirmed','Completed','Cancelled')),
    CONSTRAINT chk_dates CHECK (return_date >= pickup_date)
);

INSERT INTO rentals (customer_name, vehicle_class, pickup_city, daily_rate, pickup_date, return_date, status) VALUES
('John Carter', 'SUV', 'New York', 95.00, '2022-01-05', '2022-01-10', 'Confirmed'),
('Emily Stone', 'SUV Premium', 'Newark', 110.00, '2023-12-30', '2024-01-03', 'Completed'),
('Liam Walker', 'SUV XL', 'New Haven', 102.00, '2022-06-15', '2022-06-20', 'Completed'),
('Ava Johnson', 'Economy', 'Chicago', 45.00, '2023-03-02', '2023-03-07', 'Booked'),
('Noah Brown', 'Compact', 'Los Angeles', 55.00, '2023-04-10', '2023-04-14', 'Booked'),
('Mason Lee', 'Premium', 'Miami', 150.00, '2022-07-05', '2022-07-09', 'Completed'),
('Olivia Davis', 'SUV', 'New Orleans', 99.00, '2021-12-28', '2022-01-03', 'Cancelled'),
('William Moore', 'Economy', 'Seattle', 60.00, '2023-09-15', '2023-09-18', 'Booked'),
('Sophia Wilson', 'SUV', 'Newcastle', 92.00, '2023-08-08', '2023-08-12', 'Confirmed'),
('James Taylor', 'Premium', 'Boston', 130.00, '2023-10-01', '2023-10-04', 'Completed'),
('Isabella Anderson', 'Compact', 'San Diego', 50.00, '2022-02-10', '2022-02-15', 'Booked'),
('Benjamin Thomas', 'SUV Premium', 'Newton', 120.00, '2023-05-03', '2023-05-08', 'Confirmed'),
('Fiona Green', 'Economy', 'Austin', 35.00, '2021-09-15', '2021-09-20', 'Cancelled'),
('George King', 'Compact', 'Dallas', 55.00, '2022-11-05', '2022-11-10', 'Booked'),
('Hannah Scott', 'SUV', 'New Bedford', 97.00, '2022-04-22', '2022-04-26', 'Completed'),
('Ian Baker', 'Premium', 'Portland', 140.00, '2023-07-12', '2023-07-16', 'Completed'),
('Julia Perez', 'Economy', 'Philadelphia', 42.00, '2023-01-14', '2023-01-18', 'Booked'),
('Kevin Roberts', 'Compact', 'Atlanta', 58.00, '2022-06-10', '2022-06-14', 'Confirmed'),
('Laura Young', 'SUV', 'New Plymouth', 100.00, '2023-11-22', '2023-11-27', 'Completed'),
('Michael Hall', 'SUV XL', 'New Braunfels', 108.00, '2022-03-03', '2022-03-08', 'Confirmed'),
('Nora Allen', 'Premium', 'Las Vegas', 160.00, '2023-05-05', '2023-05-08', 'Completed'),
('Oscar Wright', 'Economy', 'San Antonio', 40.00, '2023-09-01', '2023-09-04', 'Booked'),
('Paula Martin', 'SUV Premium', 'NEW TOWN', 118.00, '2023-08-15', '2023-08-19', 'Completed'),
('Ryan Phillips', 'Economy', 'Houston', 38.00, '2022-10-10', '2022-10-14', 'Cancelled'),
('Sara Campbell', 'Compact', 'Orlando', 52.00, '2023-02-25', '2023-03-01', 'Booked');

-- должны удаляться
INSERT INTO rentals (customer_name, vehicle_class, pickup_city, daily_rate, pickup_date, return_date, status) VALUES
('Alice Test', 'Economy', 'Austin', 30.00, '2018-12-20', '2018-12-28', 'Cancelled'),
('Bob Test',   'Compact', 'Dallas', 28.00, '2018-11-15', '2018-11-17', 'Cancelled');

-- не должны удаляться
('Diana Tester', 'Compact', 'Boston', 27.00, '2018-12-27', '2019-01-01', 'Cancelled'), -- дата = 2019-01-01 (не <)
('Evan Test',    'Premium', 'San Diego', 60.00, '2020-02-10', '2020-02-12', 'Completed'); -- статус не Cancelled

-- a)

 SELECT
     id, customer_name, vehicle_class, pickup_city, daily_rate, pickup_date, return_date, status
 FROM rentals
 WHERE pickup_city ILIKE '%new%'
   AND vehicle_class ILIKE 'suv%'
   AND pickup_date BETWEEN DATE '2022-01-01' AND DATE '2023-12-31'
 ORDER BY pickup_date, customer_name;

-- b) 

 UPDATE rentals
 SET status = 'Confirmed'
 WHERE status = 'Booked'
   AND daily_rate BETWEEN 40 AND 70
   AND pickup_city NOT ILIKE '%test%';

-- c) 
 
 DELETE FROM rentals
 WHERE status = 'Cancelled'
   AND return_date < DATE '2019-01-01'
   AND customer_name ILIKE '%test%';