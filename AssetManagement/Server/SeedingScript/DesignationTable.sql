CREATE TABLE Designation (
    Id INTEGER PRIMARY KEY AUTOINCREMENT, 
    Designation TEXT NOT NULL

);


ALTER TABLE EmployeeOnboarding ADD COLUMN Designation TEXT NOT NULL DEFAULT '';
ALTER TABLE EmployeeOnboarding ADD COLUMN ReportingTo TEXT NOT NULL DEFAULT '';