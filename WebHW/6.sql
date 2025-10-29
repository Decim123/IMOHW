CREATE SCHEMA movie_db;

CREATE TABLE movie_db.directors (
    director_id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    country TEXT NOT NULL
);

CREATE TABLE movie_db.movies (
    movie_id INTEGER PRIMARY KEY,
    title TEXT NOT NULL,
    release_year INTEGER NOT NULL,
    duration INTEGER NOT NULL,
    rating NUMERIC DEFAULT 0,
    CONSTRAINT unique_title_year UNIQUE (title, release_year)
);
