ALTER TABLE my_aspnet_Membership CONVERT TO CHARACTER SET DEFAULT;
ALTER TABLE my_aspnet_Roles CONVERT TO CHARACTER SET DEFAULT;
ALTER TABLE my_aspnet_UsersInRoles CONVERT TO CHARACTER SET DEFAULT;

UPDATE my_aspnet_SchemaVersion SET version=4 WHERE version=3;
