CREATE TABLE courses (
    course_id INTEGER PRIMARY KEY,
    title TEXT NOT NULL,
    hours INTEGER NOT NULL CHECK (hours BETWEEN 10 AND 200),
    difficulty_level INTEGER NOT NULL CHECK (difficulty_level BETWEEN 1 AND 5)
);

INSERT INTO courses (course_id, title, hours, difficulty_level) VALUES
    (1, 'курс', 120, 2),
    (2, 'еще 1 курс', 160, 3),
    (3, 'какой-то курс', 80, 2),
    (4, 'Курс 4', 200, 4);