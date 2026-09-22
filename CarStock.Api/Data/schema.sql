PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Dealers (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL CHECK (length(trim(Name)) BETWEEN 1 AND 100),
    Email TEXT NOT NULL COLLATE NOCASE UNIQUE
        CHECK (length(trim(Email)) BETWEEN 3 AND 254),
    PasswordHash TEXT NOT NULL CHECK (length(PasswordHash) > 0)
);

-- Each row represents a dealer's stock of a make/model/year combination.
CREATE TABLE IF NOT EXISTS Cars (
    Id INTEGER PRIMARY KEY,
    DealerId INTEGER NOT NULL,
    Make TEXT NOT NULL COLLATE NOCASE
        CHECK (length(trim(Make)) BETWEEN 1 AND 100),
    Model TEXT NOT NULL COLLATE NOCASE
        CHECK (length(trim(Model)) BETWEEN 1 AND 100),
    Year INTEGER NOT NULL CHECK (typeof(Year) = 'integer' AND Year BETWEEN 1886 AND 9999),
    StockLevel INTEGER NOT NULL DEFAULT 0
        CHECK (typeof(StockLevel) = 'integer' AND StockLevel >= 0),
    FOREIGN KEY (DealerId) REFERENCES Dealers(Id) ON DELETE RESTRICT,
    UNIQUE (DealerId, Make, Model, Year)
);
