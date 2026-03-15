USE MiniErp;
GO

------------------------------------------------------------
-- UPDATE PASSWORD HASHES FOR LOGIN
------------------------------------------------------------
-- Run this AFTER getting hashes from the API:
--
-- 1. Start the API:  dotnet run --project MiniErp
-- 2. Call:           POST https://localhost:7089/api/auth/hash
--    Body (JSON):    "admin@123"
--    Copy the returned hash string.
--
-- 3. Replace PASTE_HASH_HERE below with that hash, then run this script.
------------------------------------------------------------

-- Same password for both users (admin@123)
UPDATE Users SET PasswordHash = 'PASTE_HASH_HERE' WHERE Username = 'admin';
UPDATE Users SET PasswordHash = 'PASTE_HASH_HERE' WHERE Username = 'user1';
